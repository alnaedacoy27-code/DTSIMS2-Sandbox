Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Where is the database? Shown at startup when the connection fails, and from the
''' login screen ("Server settings"). Main laptop: localhost. Other laptops: the main laptop's address.
''' </summary>
Public Class ConnectionSettingsForm
    Inherits AuthShellForm

    Private ReadOnly _server As New InputField("Server (main laptop name or IP address)", False, 340)
    Private ReadOnly _port As New InputField("Port", False, 92)
    Private ReadOnly _database As New InputField("Database")
    Private ReadOnly _user As New InputField("Database username")
    Private ReadOnly _password As New InputField("Database password", True)
    Private ReadOnly _test As New MaroonButton()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _exitLink As New LinkLabel()

    Public Sub New(problem As String)
        MyBase.New()
        Text = "Database connection - Darwin Trading"

        AddHeading("Connect to the database",
                   "On the main laptop use localhost. On the other laptops, enter the main laptop's name or IP address.")

        Dim cur As ConnectionSettings = Db.Settings
        _server.Value = cur.Server
        _port.Value = cur.Port.ToString()
        _database.Value = cur.Database
        _user.Value = cur.User
        _password.Value = cur.Password

        _server.Location = New Point(LeftMargin, Theme.S(136))
        _port.Location = New Point(LeftMargin + Theme.S(356), Theme.S(136))
        _database.Location = New Point(LeftMargin, Theme.S(210))
        _user.Location = New Point(LeftMargin, Theme.S(284))
        _password.Location = New Point(LeftMargin, Theme.S(358))

        Message.Location = New Point(LeftMargin, Theme.S(436))
        Message.Size = New Size(FieldWidth, Theme.S(56))
        If Not String.IsNullOrEmpty(problem) Then ShowMessage(problem, True)

        _test.Text = "Test connection"
        _test.MakeSecondary()
        _test.Size = New Size(Theme.S(200), Theme.S(46))
        _test.Location = New Point(LeftMargin, Theme.S(504))
        AddHandler _test.Click, AddressOf OnTest

        _save.Text = "Save and connect"
        _save.Size = New Size(Theme.S(236), Theme.S(46))
        _save.Location = New Point(LeftMargin + Theme.S(212), Theme.S(504))
        AddHandler _save.Click, AddressOf OnSave

        AddLink(_exitLink, "Exit", LeftMargin, 564)
        AddHandler _exitLink.LinkClicked, AddressOf OnExit

        Body.Controls.Add(_server)
        Body.Controls.Add(_port)
        Body.Controls.Add(_database)
        Body.Controls.Add(_user)
        Body.Controls.Add(_password)
        Body.Controls.Add(Message)
        Body.Controls.Add(_test)
        Body.Controls.Add(_save)

        AcceptButton = _save
    End Sub

    Private Sub OnExit(sender As Object, e As LinkLabelLinkClickedEventArgs)
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

    Private Function ReadSettings(ByRef error1 As String) As ConnectionSettings
        error1 = ""
        Dim p As Integer
        If _server.Value.Trim().Length = 0 Then
            error1 = "Enter the server name or IP address."
            Return Nothing
        End If
        If Not Integer.TryParse(_port.Value.Trim(), p) OrElse p < 1 OrElse p > 65535 Then
            error1 = "The port must be a number from 1 to 65535 (normally 3306)."
            Return Nothing
        End If
        If _database.Value.Trim().Length = 0 OrElse _user.Value.Trim().Length = 0 Then
            error1 = "Enter the database name and the database username."
            Return Nothing
        End If

        Dim s As New ConnectionSettings()
        s.Server = _server.Value.Trim()
        s.Port = p
        s.Database = _database.Value.Trim()
        s.User = _user.Value.Trim()
        s.Password = _password.Value
        Return s
    End Function

    Private Sub OnTest(sender As Object, e As EventArgs)
        Dim err As String = ""
        Dim s As ConnectionSettings = ReadSettings(err)
        If s Is Nothing Then
            ShowMessage(err, True)
            Return
        End If

        ShowMessage("Testing...", False)
        Cursor = Cursors.WaitCursor
        Try
            Dim msg As String = ""
            If Db.CheckStatusFor(s, msg) = DbStatus.Ok Then
                ShowMessage("Connected. The database is ready.", False)
            Else
                ShowMessage(msg, True)
            End If
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        Dim err As String = ""
        Dim s As ConnectionSettings = ReadSettings(err)
        If s Is Nothing Then
            ShowMessage(err, True)
            Return
        End If

        Try
            s.Save()
        Catch ex As Exception
            ShowMessage("Could not save the settings: " & ex.Message, True)
            Return
        End Try

        Db.Configure(s)
        DialogResult = DialogResult.OK
    End Sub

End Class
