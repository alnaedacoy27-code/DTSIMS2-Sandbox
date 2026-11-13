Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Threading

''' <summary>
''' Implement this on every module screen so it refreshes by itself when another laptop
''' (or Workbench) changes the data it shows.
''' </summary>
Public Interface ILiveForm
    ''' <summary>Table names this screen shows, e.g. {"suppliers"}.</summary>
    ReadOnly Property WatchedTables As String()
    ''' <summary>Reload the data (called on the screen's own thread).</summary>
    Sub RefreshData()
End Interface

Public Enum LiveSecurityEvent
    SuperadminMissing
    AccountRemoved
    AccountDisabled
    AccountLocked
    RoleChanged
End Enum

''' <summary>
''' Real-time engine. Once a second it asks the database "did anything change?" (the change_log
''' table, filled by triggers), then raises events on the screen thread:
'''   DataChanged       - which tables changed (module screens refresh themselves)
'''   SecurityChanged   - superadmin deleted / my account removed, disabled, locked / my role changed
'''   ConnectionChanged - database went offline / came back
''' </summary>
Public Class LiveSync
    Implements IDisposable

    Private Const PollMs As Integer = 1000
    Private Const ValidateEverySeconds As Integer = 15

    Public Event DataChanged(tables As List(Of String))
    Public Event SecurityChanged(kind As LiveSecurityEvent, detail As String)
    Public Event ConnectionChanged(connected As Boolean)

    Private _timer As Timer
    Private _ui As SynchronizationContext
    Private _busy As Integer
    Private _stopped As Boolean = True
    Private _connected As Boolean = True
    Private _lastId As Long = -1
    Private _lastRole As String = ""
    Private _lastValidate As DateTime = DateTime.MinValue
    Private _lastPrune As DateTime = DateTime.MinValue
    Private _lastBeat As DateTime = DateTime.MinValue

    ''' <summary>Call on the screen thread, after login.</summary>
    Public Sub Start()
        StopPolling()
        _ui = SynchronizationContext.Current
        _lastRole = Permissions.NormalizeRole(AppSession.Role)
        _lastId = -1
        _connected = True
        _stopped = False
        _timer = New Timer(AddressOf OnTick, Nothing, 500, PollMs)
    End Sub

    Public Sub StopPolling()
        _stopped = True
        If _timer IsNot Nothing Then
            _timer.Dispose()
            _timer = Nothing
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        StopPolling()
    End Sub

    ''' <summary>True when a screen watching these tables should refresh.</summary>
    Public Shared Function Affects(watched As String(), changed As List(Of String)) As Boolean
        If changed.Contains("*") Then Return True
        For Each t As String In watched
            If changed.Contains(t) Then Return True
        Next
        Return False
    End Function

    ' ------------------------------------------------------------------ polling (background thread)
    Private Sub OnTick(state As Object)
        If _stopped Then Return
        If Interlocked.Exchange(_busy, 1) = 1 Then Return
        Try
            Poll()
        Catch
            SetConnected(False)
        Finally
            Interlocked.Exchange(_busy, 0)
        End Try
    End Sub

    Private Sub Poll()
        Dim maxId As Long = Convert.ToInt64(Db.Scalar("SELECT COALESCE(MAX(id), 0) FROM change_log"))
        Dim wasOffline As Boolean = Not _connected
        SetConnected(True)

        ' "I'm still here": the Users list and User Monitoring use this to show who is logged in
        If (DateTime.UtcNow - _lastBeat).TotalSeconds >= 5 Then
            _lastBeat = DateTime.UtcNow
            SessionTracker.Heartbeat()
        End If

        Dim changed As New List(Of String)()
        If _lastId < 0 Then
            _lastId = maxId                         ' first poll: start from "now"
        ElseIf maxId < _lastId Then
            _lastId = maxId                         ' log was reset / database restored
            changed.Add("*")
        ElseIf maxId > _lastId Then
            Dim dt As DataTable = Db.Table(
                "SELECT DISTINCT module FROM change_log WHERE id > @last AND id <= @max",
                Db.Params("@last", _lastId, "@max", maxId))
            For Each r As DataRow In dt.Rows
                changed.Add(r("module").ToString())
            Next
            _lastId = maxId
        End If
        If wasOffline Then changed.Add("*")         ' we may have missed changes while offline

        Dim needCheck As Boolean = changed.Contains("*") OrElse changed.Contains("users") OrElse
                                   (DateTime.UtcNow - _lastValidate).TotalSeconds >= ValidateEverySeconds
        If needCheck Then
            _lastValidate = DateTime.UtcNow
            If Not ValidateSession() Then Return
        End If

        If changed.Count > 0 Then
            OnUi(Sub() RaiseEvent DataChanged(changed))
        End If

        Prune()
    End Sub

    ''' <returns>False when a problem was reported (polling stops).</returns>
    Private Function ValidateSession() As Boolean
        Dim supers As Integer = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM users WHERE role = 'superadmin'"))
        If supers = 0 Then
            Fail(LiveSecurityEvent.SuperadminMissing, "")
            Return False
        End If

        Dim uid As Integer = AppSession.UserId
        If uid <= 0 Then Return True

        Dim dt As DataTable = Db.Table("SELECT role, is_active, is_locked FROM users WHERE id = @id", Db.Params("@id", uid))
        If dt.Rows.Count = 0 Then
            Fail(LiveSecurityEvent.AccountRemoved, "")
            Return False
        End If

        Dim row As DataRow = dt.Rows(0)
        If Convert.ToInt32(row("is_active")) = 0 Then
            Fail(LiveSecurityEvent.AccountDisabled, "")
            Return False
        End If
        If Convert.ToInt32(row("is_locked")) = 1 AndAlso Permissions.NormalizeRole(row("role").ToString()) <> "superadmin" Then
            Fail(LiveSecurityEvent.AccountLocked, "")
            Return False
        End If

        Dim role As String = Permissions.NormalizeRole(row("role").ToString())
        If role <> _lastRole Then
            _lastRole = role
            OnUi(Sub() RaiseEvent SecurityChanged(LiveSecurityEvent.RoleChanged, role))
        End If
        Return True
    End Function

    Private Sub Fail(kind As LiveSecurityEvent, detail As String)
        _stopped = True                             ' the screen decides what happens next
        OnUi(Sub() RaiseEvent SecurityChanged(kind, detail))
    End Sub

    Private Sub SetConnected(state As Boolean)
        If state = _connected Then Return
        _connected = state
        OnUi(Sub() RaiseEvent ConnectionChanged(state))
    End Sub

    Private Sub Prune()
        If (DateTime.UtcNow - _lastPrune).TotalMinutes < 30 Then Return
        _lastPrune = DateTime.UtcNow
        Try
            Db.NonQuery("DELETE FROM change_log WHERE changed_at < DATE_SUB(NOW(), INTERVAL 1 DAY)")
            ' sessions that never logged out (crash, power cut): close them at their last heartbeat
            Db.NonQuery("UPDATE user_sessions SET is_active = 0, logout_at = last_seen " &
                        "WHERE is_active = 1 AND last_seen < DATE_SUB(NOW(), INTERVAL 2 MINUTE)")
            Db.NonQuery("DELETE FROM user_sessions WHERE login_at < DATE_SUB(NOW(), INTERVAL 60 DAY)")
        Catch
            ' ignore
        End Try
    End Sub

    Private Sub OnUi(action As Action)
        If _ui IsNot Nothing Then
            _ui.Post(Sub(s) action(), Nothing)
        Else
            action()
        End If
    End Sub

End Class
