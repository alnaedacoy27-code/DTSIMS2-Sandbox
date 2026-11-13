Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

Public Class LoginForm
    Inherits AuthShellForm

    Private ReadOnly _username As New InputField("Username")
    Private ReadOnly _password As New InputField("Password", True)
    Private ReadOnly _show As New CheckBox()
    Private ReadOnly _login As New MaroonButton()
    Private ReadOnly _settingsLink As New LinkLabel()
    Private ReadOnly _exitLink As New LinkLabel()

    Public Sub New()
        MyBase.New()
        Text = "Log in - Darwin Trading"

        AddHeading("Welcome back", "Log in to continue.")

        _username.Location = New Point(LeftMargin, Theme.S(150))
        _password.Location = New Point(LeftMargin, Theme.S(228))
        _username.MaxLength = 30
        _password.MaxLength = 64

        _show.Text = "Show password"
        _show.AutoSize = True
        _show.Font = Theme.FontSmall
        _show.ForeColor = Theme.TextMuted
        _show.Location = New Point(LeftMargin, Theme.S(306))
        AddHandler _show.CheckedChanged, AddressOf OnShowChanged

        Message.Location = New Point(LeftMargin, Theme.S(338))

        _login.Text = "Log in"
        _login.Size = New Size(FieldWidth, Theme.S(46))
        _login.Location = New Point(LeftMargin, Theme.S(386))
        AddHandler _login.Click, AddressOf OnLogin

        AddLink(_settingsLink, "Server settings", LeftMargin, 560)
        AddHandler _settingsLink.LinkClicked, AddressOf OnServerSettings
        AddLink(_exitLink, "Exit", LeftMargin + FieldWidth - 24, 560)
        AddHandler _exitLink.LinkClicked, AddressOf OnExit

        Body.Controls.Add(_username)
        Body.Controls.Add(_password)
        Body.Controls.Add(_show)
        Body.Controls.Add(Message)
        Body.Controls.Add(_login)

        AcceptButton = _login
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _username.FocusInput()
    End Sub

    Private Sub OnShowChanged(sender As Object, e As EventArgs)
        _password.IsPassword = Not _show.Checked
    End Sub

    Private Sub OnExit(sender As Object, e As LinkLabelLinkClickedEventArgs)
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

    Private Sub OnServerSettings(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Using f As New ConnectionSettingsForm("")
            If f.ShowDialog(Me) = DialogResult.OK Then
                Dim msg As String = ""
                If Db.CheckStatus(msg) = DbStatus.Ok Then
                    ShowMessage("Connected to " & Db.Settings.Server & ".", False)
                Else
                    ShowMessage(msg, True)
                End If
            End If
        End Using
    End Sub

    Private Sub OnLogin(sender As Object, e As EventArgs)
        ShowMessage("", False)
        _login.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim msg As String = ""
            If AuthService.TryLogin(_username.Value, _password.Value, msg) Then
                DialogResult = DialogResult.OK
            Else
                ShowMessage(msg, True)
                _password.Value = ""
                _password.FocusInput()
            End If
        Finally
            Cursor = Cursors.Default
            _login.Enabled = True
        End Try
    End Sub

End Class
