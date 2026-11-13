Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Everything the monitoring screen reads from the database.</summary>
Public Module MonitoringData

    Private Function EscapeLike(s As String) As String
        Return s.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
    End Function

    ''' <summary>People logged in right now (fresh heartbeat in the last 30 seconds).</summary>
    Public Function GetActiveSessions(search As String) As DataTable
        Dim sql As String =
            "SELECT s.session_id, s.user_id, u.username, u.full_name, u.role, s.device_name, s.ip_address, s.login_at, " &
            "s.current_module, " &
            "TIMESTAMPDIFF(SECOND, s.login_at, NOW()) AS active_seconds, " &
            "TIMESTAMPDIFF(SECOND, s.module_since, NOW()) AS module_seconds " &
            "FROM user_sessions s INNER JOIN users u ON u.id = s.user_id " &
            "WHERE s.is_active = 1 AND s.last_seen >= DATE_SUB(NOW(), INTERVAL 30 SECOND) "

        Dim p As New Dictionary(Of String, Object)()
        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= "AND (u.full_name LIKE @s OR u.username LIKE @s OR s.device_name LIKE @s OR s.current_module LIKE @s) "
            p("@s") = "%" & EscapeLike(search.Trim()) & "%"
        End If
        sql &= "ORDER BY FIELD(u.role, 'superadmin', 'admin', 'cashier'), s.login_at"
        Return Db.Table(sql, p)
    End Function

    Public Function GetLastAuditId() As Long
        Return Convert.ToInt64(Db.Scalar("SELECT COALESCE(MAX(id), 0) FROM audit_trail"))
    End Function

    ''' <summary>The latest movements (logins, screens opened, adds/edits/deletes ...).</summary>
    Public Function GetRecentActivity(limit As Integer) As DataTable
        Return Db.Table(
            "SELECT a.id, a.created_at, a.username, a.action, a.module, a.description, a.device_name, " &
            "(SELECT u.full_name FROM users u WHERE u.id = a.user_id) AS full_name " &
            "FROM audit_trail a ORDER BY a.id DESC LIMIT " & limit.ToString())
    End Function

End Module

''' <summary>
''' Utilities > User Monitoring (Superadmin only). Who is logged in, on which PC, how long (a live timer),
''' and which screen they are in right now, plus a feed of every movement.
''' </summary>
Public Class UserMonitoringForm
    Inherits Form

    Private Shared ReadOnly FeedWidth As Integer = Theme.S(360)

    Private Shared ReadOnly BoldFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly ClockFont As New Font("Consolas", 11.5F, FontStyle.Bold)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _search As New SearchBox("Search user, PC or screen...")

    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cardActive As New StatCard()
    Private ReadOnly _cardPcs As New StatCard()
    Private ReadOnly _cardAdmins As New StatCard()
    Private ReadOnly _cardCashiers As New StatCard()

    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()

    Private ReadOnly _feedHost As New Panel()
    Private ReadOnly _feedTitle As New Label()
    Private ReadOnly _feed As New DataGridView()

    Private ReadOnly _poll As New Timer()
    Private ReadOnly _tick As New Timer()
    Private ReadOnly _sinceFetch As New Stopwatch()

    Private _sessionSig As String = ""
    Private _feedLastId As Long = -1

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildStats()
        BuildSessionGrid()
        BuildFeed()

        Controls.Add(_listHost)
        Controls.Add(_feedHost)
        Controls.Add(_stats)
        Controls.Add(_header)
    End Sub

    ' ------------------------------------------------------------------ building the screen
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "User Monitoring"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "Who is logged in, on which PC, and what they are doing right now"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _search.Size = New Size(Theme.S(300), Theme.S(40))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)
        _header.Controls.Add(_search)
        AddHandler _header.Resize, AddressOf OnHeaderResize
        OnHeaderResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnHeaderResize(sender As Object, e As EventArgs)
        _search.Location = New Point(Math.Max(0, _header.ClientSize.Width - Theme.S(32) - _search.Width), Theme.S(30))
    End Sub

    Private Sub BuildStats()
        _stats.Dock = DockStyle.Top
        _stats.Height = Theme.S(84)
        _stats.BackColor = Color.White
        _stats.FlowDirection = FlowDirection.LeftToRight
        _stats.WrapContents = False
        _stats.Padding = New Padding(Theme.S(32), Theme.S(4), 0, 0)

        For Each c As StatCard In New StatCard() {_cardActive, _cardPcs, _cardAdmins, _cardCashiers}
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            _stats.Controls.Add(c)
        Next
        _cardActive.Caption = "People logged in"
        _cardPcs.Caption = "PCs in use"
        _cardAdmins.Caption = "Admins online"
        _cardCashiers.Caption = "Cashiers online"
    End Sub

    Private Shared Sub AddColumn(grid As DataGridView, name As String, headerText As String, dataProp As String, weight As Single)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = name
        c.HeaderText = headerText
        c.DataPropertyName = dataProp
        c.FillWeight = weight
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        grid.Columns.Add(c)
    End Sub

    Private Sub BuildSessionGrid()
        _listHost.Dock = DockStyle.Fill
        _listHost.BackColor = Color.White
        _listHost.Padding = New Padding(Theme.S(32), Theme.S(4), Theme.S(12), Theme.S(16))

        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 64)
        AddColumn(_grid, "user", "USER", "full_name", 26)
        AddColumn(_grid, "role", "ROLE", "role", 10)
        AddColumn(_grid, "pc", "PC", "device_name", 15)
        AddColumn(_grid, "login", "LOGGED IN", "login_at", 13)
        AddColumn(_grid, "active", "ACTIVE FOR", "active_seconds", 12)
        AddColumn(_grid, "screen", "CURRENT SCREEN", "current_module", 24)
        AddHandler _grid.CellPainting, AddressOf OnSessionCellPainting

        _empty.Dock = DockStyle.Fill
        _empty.TextAlign = ContentAlignment.MiddleCenter
        _empty.Font = Theme.FontHeading
        _empty.ForeColor = Theme.TextMuted
        _empty.BackColor = Color.White
        _empty.Text = "Nobody matches your search."
        _empty.Visible = False

        _listHost.Controls.Add(_grid)
        _listHost.Controls.Add(_empty)
    End Sub

    Private Sub BuildFeed()
        _feedHost.Dock = DockStyle.Right
        _feedHost.Width = FeedWidth
        _feedHost.BackColor = Color.FromArgb(251, 249, 249)
        _feedHost.Padding = New Padding(0, Theme.S(14), 0, 0)
        AddHandler _feedHost.Paint, AddressOf OnFeedHostPaint

        _feedTitle.Dock = DockStyle.Top
        _feedTitle.Height = Theme.S(38)
        _feedTitle.Text = "Recent activity"
        _feedTitle.Font = Theme.FontHeading
        _feedTitle.ForeColor = Theme.Maroon
        _feedTitle.Padding = New Padding(Theme.S(22), Theme.S(6), 0, 0)

        _feed.Dock = DockStyle.Fill
        GridStyle.Apply(_feed, 58)
        _feed.ColumnHeadersVisible = False
        _feed.BackgroundColor = _feedHost.BackColor
        _feed.DefaultCellStyle.BackColor = _feedHost.BackColor
        _feed.DefaultCellStyle.SelectionBackColor = _feedHost.BackColor
        _feed.DefaultCellStyle.Padding = New Padding(0)
        _feed.GridColor = Color.FromArgb(240, 236, 236)
        AddColumn(_feed, "line1", "", "line1", 100)
        AddHandler _feed.CellPainting, AddressOf OnFeedCellPainting

        _feedHost.Controls.Add(_feed)
        _feedHost.Controls.Add(_feedTitle)
    End Sub

    Private Sub OnFeedHostPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _feedHost.Height)
        End Using
    End Sub

    ' ------------------------------------------------------------------ polling
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        RefreshSessions(True)
        RefreshFeed(True)

        _poll.Interval = 2000
        AddHandler _poll.Tick, AddressOf OnPoll
        _poll.Start()

        _tick.Interval = 1000
        AddHandler _tick.Tick, AddressOf OnTick
        _tick.Start()
    End Sub

    Private Sub OnPoll(sender As Object, e As EventArgs)
        Try
            RefreshSessions(False)
            RefreshFeed(False)
        Catch
            ' database offline: the main window already shows it
        End Try
    End Sub

    ''' <summary>Only the two timer columns repaint every second (no flicker, no database call).</summary>
    Private Sub OnTick(sender As Object, e As EventArgs)
        If _grid.Rows.Count = 0 Then Return
        _grid.InvalidateColumn(_grid.Columns("active").Index)
        _grid.InvalidateColumn(_grid.Columns("screen").Index)
    End Sub

    Private Sub OnSearchChanged()
        Try
            RefreshSessions(True)
        Catch
            ' ignore
        End Try
    End Sub

    Private Sub RefreshSessions(force As Boolean)
        Dim dt As DataTable = MonitoringData.GetActiveSessions(_search.Query)

        ' only rebuild the list when someone arrives, leaves or changes screen
        Dim sig As New System.Text.StringBuilder()
        For Each r As DataRow In dt.Rows
            sig.Append(r("session_id").ToString()).Append("|").Append(r("current_module").ToString()).Append(";")
        Next
        If Not force AndAlso sig.ToString() = _sessionSig Then Return
        _sessionSig = sig.ToString()

        Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex
        _grid.DataSource = dt
        _sinceFetch.Restart()
        If scroll >= 0 AndAlso scroll < _grid.Rows.Count Then _grid.FirstDisplayedScrollingRowIndex = scroll

        _empty.Visible = (dt.Rows.Count = 0)
        If dt.Rows.Count = 0 Then _empty.BringToFront()

        Dim users As New HashSet(Of Integer)()
        Dim pcs As New HashSet(Of String)()
        Dim admins As New HashSet(Of Integer)()
        Dim cashiers As New HashSet(Of Integer)()
        For Each r As DataRow In dt.Rows
            Dim uid As Integer = Convert.ToInt32(r("user_id"))
            users.Add(uid)
            pcs.Add(r("device_name").ToString())
            Select Case Permissions.NormalizeRole(r("role").ToString())
                Case "admin"
                    admins.Add(uid)
                Case "cashier"
                    cashiers.Add(uid)
            End Select
        Next
        _cardActive.Value = users.Count.ToString()
        _cardPcs.Value = pcs.Count.ToString()
        _cardAdmins.Value = admins.Count.ToString()
        _cardCashiers.Value = cashiers.Count.ToString()
    End Sub

    Private Sub RefreshFeed(force As Boolean)
        Dim lastId As Long = MonitoringData.GetLastAuditId()
        If Not force AndAlso lastId = _feedLastId Then Return
        _feedLastId = lastId

        Dim src As DataTable = MonitoringData.GetRecentActivity(60)
        Dim view As New DataTable()
        view.Columns.Add("line1", GetType(String))
        view.Columns.Add("line2", GetType(String))
        view.Columns.Add("kind", GetType(String))

        For Each r As DataRow In src.Rows
            Dim action As String = r("action").ToString()
            Dim uname As String = If(r("username") Is DBNull.Value, "", r("username").ToString())
            Dim fullName As String = If(r("full_name") Is DBNull.Value, "", r("full_name").ToString())
            Dim who As String = If(fullName.Length > 0, fullName, If(uname.Length > 0, uname, "Someone"))
            Dim moduleName As String = If(r("module") Is DBNull.Value, "", r("module").ToString())
            Dim desc As String = If(r("description") Is DBNull.Value, "", r("description").ToString())

            Dim text As String
            Select Case action
                Case "OPEN"
                    text = who & " opened " & moduleName
                Case "LOGIN"
                    text = who & " logged in"
                Case "LOGOUT"
                    text = who & " logged out"
                Case "LOGIN_FAILED"
                    text = "Failed login: " & desc
                Case Else
                    text = who & ": " & If(desc.Length > 0, desc, action & " " & moduleName)
            End Select

            Dim device As String = If(r("device_name") Is DBNull.Value, "", r("device_name").ToString())
            Dim whenText As String = FriendlyTime(Convert.ToDateTime(r("created_at")))
            view.Rows.Add(text, whenText & If(device.Length > 0, "   |   " & device, ""), action)
        Next

        Dim scroll As Integer = _feed.FirstDisplayedScrollingRowIndex
        _feed.DataSource = view
        _feed.ClearSelection()
        If scroll > 0 AndAlso scroll < _feed.Rows.Count Then _feed.FirstDisplayedScrollingRowIndex = scroll
    End Sub

    ' ------------------------------------------------------------------ formatting
    Private Shared Function FriendlyTime(d As DateTime) As String
        If d.Date = DateTime.Today Then Return d.ToString("h:mm:ss tt")
        If d.Date = DateTime.Today.AddDays(-1) Then Return "Yesterday " & d.ToString("h:mm tt")
        Return d.ToString("MMM d, h:mm tt")
    End Function

    Private Shared Function DayLabel(d As DateTime) As String
        If d.Date = DateTime.Today Then Return "Today"
        If d.Date = DateTime.Today.AddDays(-1) Then Return "Yesterday"
        Return d.ToString("MMM d")
    End Function

    ''' <summary>12:05:09 (or 2d 4h for very long sessions).</summary>
    Private Shared Function FormatClock(seconds As Long) As String
        If seconds < 0 Then seconds = 0
        Dim ts As TimeSpan = TimeSpan.FromSeconds(seconds)
        If ts.TotalHours >= 24 Then Return CInt(Math.Floor(ts.TotalDays)).ToString() & "d " & ts.Hours.ToString() & "h"
        Return CInt(Math.Floor(ts.TotalHours)).ToString("00") & ":" & ts.Minutes.ToString("00") & ":" & ts.Seconds.ToString("00")
    End Function

    ''' <summary>42s, 3m 12s, 1h 05m.</summary>
    Private Shared Function FormatShort(seconds As Long) As String
        If seconds < 0 Then seconds = 0
        Dim ts As TimeSpan = TimeSpan.FromSeconds(seconds)
        If ts.TotalHours >= 1 Then Return CInt(Math.Floor(ts.TotalHours)).ToString() & "h " & ts.Minutes.ToString("00") & "m"
        If ts.TotalMinutes >= 1 Then Return ts.Minutes.ToString() & "m " & ts.Seconds.ToString("00") & "s"
        Return ts.Seconds.ToString() & "s"
    End Function

    Private Function LiveSeconds(row As DataRowView, column As String) As Long
        If row(column) Is DBNull.Value Then Return 0
        Return Convert.ToInt64(row(column)) + (_sinceFetch.ElapsedMilliseconds \ 1000)
    End Function

    ' ------------------------------------------------------------------ painting
    Private Shared Sub TwoLines(g As Graphics, c As Rectangle, line1 As String, line2 As String)
        Dim x As Integer = c.X + Theme.S(10)
        Dim w As Integer = Math.Max(Theme.S(10), c.Width - Theme.S(18))
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
        TextRenderer.DrawText(g, line1, BoldFont, New Rectangle(x, c.Y + 12, w, 20), Theme.TextDark, flags)
        TextRenderer.DrawText(g, line2, Theme.FontSmall, New Rectangle(x, c.Y + 35, w, 18), Theme.TextMuted, flags)
    End Sub

    Private Sub OnSessionCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim c As Rectangle = e.CellBounds
        Select Case _grid.Columns(e.ColumnIndex).Name
            Case "user"
                Dim fullName As String = row("full_name").ToString()
                Dim circle As New Rectangle(c.X + Theme.S(12), c.Y + (c.Height - Theme.S(40)) \ 2, Theme.S(40), Theme.S(40))
                Avatar.PaintInitials(e.Graphics, circle, fullName, 10.5F)
                Dim textX As Integer = circle.Right + Theme.S(12)
                Dim textW As Integer = Math.Max(Theme.S(10), c.Right - textX - Theme.S(8))
                TextRenderer.DrawText(e.Graphics, fullName, BoldFont, New Rectangle(textX, c.Y + 12, textW, 20),
                                      Theme.TextDark, TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)
                TextRenderer.DrawText(e.Graphics, "@" & row("username").ToString(), Theme.FontSmall, New Rectangle(textX, c.Y + 35, textW, 18),
                                      Theme.TextMuted, TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

            Case "role"
                Dim role As String = row("role").ToString()
                Pills.Draw(e.Graphics, c, Permissions.RoleDisplay(role), Pills.RoleBack(role), Pills.RoleFore(role))

            Case "pc"
                Dim ip As String = If(row("ip_address") Is DBNull.Value, "", row("ip_address").ToString())
                TwoLines(e.Graphics, c, row("device_name").ToString(), If(ip.Length > 0, ip, "-"))

            Case "login"
                Dim d As DateTime = Convert.ToDateTime(row("login_at"))
                TwoLines(e.Graphics, c, d.ToString("h:mm tt"), DayLabel(d))

            Case "active"
                Dim secs As Long = LiveSeconds(row, "active_seconds")
                TextRenderer.DrawText(e.Graphics, FormatClock(secs), ClockFont,
                                      New Rectangle(c.X + Theme.S(10), c.Y, c.Width - Theme.S(12), c.Height), Theme.Maroon,
                                      TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)

            Case "screen"
                Dim screenName As String = If(row("current_module") Is DBNull.Value, "-", row("current_module").ToString())
                Dim since As String = If(row("module_seconds") Is DBNull.Value, "", "for " & FormatShort(LiveSeconds(row, "module_seconds")))
                TwoLines(e.Graphics, c, "In " & screenName, since)
        End Select

        e.Handled = True
    End Sub

    Private Sub OnFeedCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim row As DataRowView = TryCast(_feed.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background)
        Using pen As New Pen(Color.FromArgb(240, 236, 236))
            e.Graphics.DrawLine(pen, e.CellBounds.Left + Theme.S(16), e.CellBounds.Bottom - 1, e.CellBounds.Right - Theme.S(12), e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim c As Rectangle = e.CellBounds
        Dim dotColor As Color
        Select Case row("kind").ToString()
            Case "LOGIN"
                dotColor = Color.FromArgb(34, 160, 80)
            Case "LOGOUT"
                dotColor = Color.FromArgb(160, 160, 160)
            Case "LOGIN_FAILED"
                dotColor = Theme.AlertRed
            Case "OPEN"
                dotColor = Color.FromArgb(70, 110, 190)
            Case Else
                dotColor = Theme.MaroonLight
        End Select
        Using b As New SolidBrush(dotColor)
            e.Graphics.FillEllipse(b, c.X + 18, c.Y + (c.Height - 10) \ 2 - 8, 10, 10)
        End Using

        Dim x As Integer = c.X + Theme.S(40)
        Dim w As Integer = Math.Max(Theme.S(10), c.Width - Theme.S(52))
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
        TextRenderer.DrawText(e.Graphics, row("line1").ToString(), Theme.FontBase, New Rectangle(x, c.Y + 10, w, 20), Theme.TextDark, flags)
        TextRenderer.DrawText(e.Graphics, row("line2").ToString(), Theme.FontSmall, New Rectangle(x, c.Y + 32, w, 18), Theme.TextMuted, flags)

        e.Handled = True
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _poll.Stop()
            _poll.Dispose()
            _tick.Stop()
            _tick.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
