Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Shown when the database has no superadmin yet. Must be completed (or the app exits).</summary>
Public Class SetupSuperadminForm
    Inherits AuthShellForm

    Private ReadOnly _fullName As New InputField("Full name", InputKind.PersonName, 448, True)
    Private ReadOnly _username As New InputField("Username", InputKind.Username, 448, True)
    Private ReadOnly _password As New InputField("Password (8+ characters, letters and numbers)", InputKind.Password, 448, True)
    Private ReadOnly _confirm As New InputField("Confirm password", InputKind.Text, 448, True)
    Private ReadOnly _show As New CheckBox()
    Private ReadOnly _create As New MaroonButton()
    Private ReadOnly _exitLink As New LinkLabel()

    Public Sub New()
        MyBase.New()
        Text = "First-time setup - Darwin Trading"

        AddHeading("Create the Superadmin",
                   "No Superadmin account was found. Create one first. It has full control of the system.")

        _fullName.Location = New Point(LeftMargin, Theme.S(138))
        _username.Location = New Point(LeftMargin, Theme.S(212))
        _password.Location = New Point(LeftMargin, Theme.S(286))
        _confirm.Location = New Point(LeftMargin, Theme.S(360))
        _confirm.IsPassword = True
        _confirm.MaxLength = 64

        _show.Text = "Show passwords"
        _show.AutoSize = True
        _show.Font = Theme.FontSmall
        _show.ForeColor = Theme.TextMuted
        _show.Location = New Point(LeftMargin, Theme.S(434))
        AddHandler _show.CheckedChanged, AddressOf OnShowChanged

        Message.Location = New Point(LeftMargin, Theme.S(460))

        _create.Text = "Create Superadmin"
        _create.Size = New Size(FieldWidth, Theme.S(46))
        _create.Location = New Point(LeftMargin, Theme.S(504))
        AddHandler _create.Click, AddressOf OnCreate

        AddLink(_exitLink, "Exit", LeftMargin, 560)
        AddHandler _exitLink.LinkClicked, AddressOf OnExit

        Body.Controls.Add(_fullName)
        Body.Controls.Add(_username)
        Body.Controls.Add(_password)
        Body.Controls.Add(_confirm)
        Body.Controls.Add(_show)
        Body.Controls.Add(Message)
        Body.Controls.Add(_create)

        AcceptButton = _create
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _fullName.FocusInput()
    End Sub

    Private Sub OnShowChanged(sender As Object, e As EventArgs)
        _password.IsPassword = Not _show.Checked
        _confirm.IsPassword = Not _show.Checked
    End Sub

    Private Sub OnExit(sender As Object, e As LinkLabelLinkClickedEventArgs)
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

    Private Sub OnCreate(sender As Object, e As EventArgs)
        ShowMessage("", False)

        Dim problem As String = InputField.CheckAll(_fullName, _username, _password, _confirm)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        Dim fullName As String = _fullName.Value.Trim()
        Dim username As String = _username.Value.Trim()
        Dim pw As String = _password.Value

        problem = UserRules.ValidatePassword(pw, username)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            _password.FocusInput()
            Return
        End If
        If pw <> _confirm.Value Then
            ShowMessage("The two passwords do not match.", True)
            _confirm.FocusInput()
            Return
        End If

        _create.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim msg As String = ""
            If AuthService.CreateSuperadmin(fullName, username, pw, msg) Then
                MessageBox.Show(Me, "The Superadmin account was created. You can log in now.", "Account created",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                DialogResult = DialogResult.OK
            Else
                ShowMessage(msg, True)
            End If
        Finally
            Cursor = Cursors.Default
            _create.Enabled = True
        End Try
    End Sub

End Class
