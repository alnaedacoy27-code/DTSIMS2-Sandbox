Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>Same look as InputField, but with a drop-down list.</summary>
Public Class ChoiceField
    Inherits Panel

    Private ReadOnly _caption As New Label()
    Private ReadOnly _box As New Panel()
    Private ReadOnly _combo As New ComboBox()
    Private _focused As Boolean

    Public Sub New(caption As String, Optional fieldWidth As Integer = 228)
        Size = New Size(fieldWidth, Theme.S(70))
        BackColor = Color.White

        _caption.Text = caption
        _caption.AutoSize = True
        _caption.Font = Theme.FontCaption
        _caption.ForeColor = Theme.TextMuted
        _caption.Location = New Point(0, 0)

        _box.Location = New Point(0, Theme.S(22))
        _box.Size = New Size(fieldWidth, Theme.S(42))
        _box.BackColor = Color.White
        AddHandler _box.Paint, AddressOf OnBoxPaint

        _combo.DropDownStyle = ComboBoxStyle.DropDownList
        _combo.FlatStyle = FlatStyle.Flat
        _combo.Font = Theme.FontInput
        _combo.BackColor = Color.White
        _combo.ForeColor = Theme.TextDark
        _combo.Left = Theme.S(8)
        _combo.Width = fieldWidth - Theme.S(16)
        _combo.Top = (_box.Height - _combo.Height) \ 2
        AddHandler _combo.Enter, AddressOf OnComboEnter
        AddHandler _combo.SelectedIndexChanged, AddressOf OnComboChanged
        AddHandler _combo.Leave, AddressOf OnComboLeave

        _box.Controls.Add(_combo)
        Controls.Add(_caption)
        Controls.Add(_box)
    End Sub

    ' Raised when the chosen item changes
    Public Event ValueChanged()

    Private Sub OnComboChanged(sender As Object, e As EventArgs)
        RaiseEvent ValueChanged()
    End Sub

    Public Sub SetChoices(items As String())
        _combo.Items.Clear()
        For Each s As String In items
            _combo.Items.Add(s)
        Next
        If _combo.Items.Count > 0 Then _combo.SelectedIndex = 0
    End Sub

    Public Property Value As String
        Get
            Return If(_combo.SelectedItem Is Nothing, "", _combo.SelectedItem.ToString())
        End Get
        Set(v As String)
            Dim i As Integer = _combo.Items.IndexOf(v)
            If i >= 0 Then _combo.SelectedIndex = i
        End Set
    End Property

    Private Sub OnComboEnter(sender As Object, e As EventArgs)
        _focused = True
        _box.Invalidate()
    End Sub

    Private Sub OnComboLeave(sender As Object, e As EventArgs)
        _focused = False
        _box.Invalidate()
    End Sub

    Private Sub OnBoxPaint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, _box.Width - 1, _box.Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(rect, 8)
            Using pen As New Pen(If(_focused, Theme.Maroon, Theme.BorderColor), If(_focused, 1.8F, 1.0F))
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub
End Class

''' <summary>Base for the small pop-up windows: white, titled, maroon title bar, centered on the main window.</summary>
Public Class ModalDialog
    Inherits Form

    Protected ReadOnly Message As New Label()

    Public Sub New(title As String, subtitle As String, clientWidth As Integer, clientHeight As Integer)
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.White
        Font = Theme.FontBase
        Text = title
        ClientSize = New Size(clientWidth, clientHeight)

        Dim t As New Label()
        t.AutoSize = True
        t.Font = Theme.FontTitle
        t.ForeColor = Theme.Maroon
        t.Text = title
        t.Location = New Point(Theme.S(37), Theme.S(22))
        Controls.Add(t)

        Dim s As New Label()
        s.AutoSize = False
        s.Size = New Size(clientWidth - Theme.S(80), Theme.S(24))
        s.Font = Theme.FontBase
        s.ForeColor = Theme.TextMuted
        s.Text = subtitle
        s.Location = New Point(Theme.S(40), Theme.S(68))
        Controls.Add(s)

        Message.AutoSize = False
        Message.Font = Theme.FontSmall
        Message.Text = ""
        Controls.Add(Message)
    End Sub

    Protected Sub ShowMessage(text As String, isError As Boolean)
        Message.ForeColor = If(isError, Theme.AlertRed, Color.FromArgb(22, 130, 60))
        Message.Text = text
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        WindowStyle.ApplyMaroonTitleBar(Me)
    End Sub
End Class

''' <summary>Add a user, or edit one. Passwords are only typed when adding (use Reset password later).</summary>
Public Class UserEditForm
    Inherits ModalDialog

    Private ReadOnly _isEdit As Boolean
    Private ReadOnly _userId As Integer
    Private ReadOnly _fullName As New InputField("Full name", InputKind.PersonName, 470, True)
    Private ReadOnly _username As New InputField("Username", InputKind.Username, 228, True)
    Private ReadOnly _role As New ChoiceField("Role", 228)
    Private ReadOnly _contact As New InputField("Contact number", InputKind.MobilePH, 228, False)
    Private ReadOnly _address As New InputField("Address", InputKind.Address, 470, False)
    Private ReadOnly _password As New InputField("Password (8+ characters)", InputKind.Password, 228, True)
    Private ReadOnly _confirm As New InputField("Confirm password", InputKind.Text, 228, True)
    Private ReadOnly _show As New CheckBox()
    Private ReadOnly _avatar As New Avatar()
    Private ReadOnly _choose As New LinkLabel()
    Private ReadOnly _remove As New LinkLabel()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()
    Private ReadOnly _nameTimer As New Timer()

    Private _photoBytes As Byte()
    Private _photoChanged As Boolean

    ''' <summary>After a successful save: the saved values (to keep the logged-in session in sync).</summary>
    Public Property SavedFullName As String = ""
    Public Property SavedUsername As String = ""

    ''' <summary>Add a new user.</summary>
    Public Sub New()
        MyBase.New("Add user", "Create a login for a new team member.", 760, 624)
        _isEdit = False
        _userId = 0
        BuildUi()

        If Permissions.NormalizeRole(AppSession.Role) = "superadmin" Then
            _role.SetChoices(New String() {"Admin", "Cashier"})
            _role.Value = "Cashier"
        Else
            _role.SetChoices(New String() {"Cashier"})
            _role.Enabled = False
        End If
    End Sub

    ''' <summary>Edit an existing user.</summary>
    Public Sub New(userId As Integer, canChangeRole As Boolean)
        MyBase.New("Edit user", "Update this person's details.", 760, 524)
        _isEdit = True
        _userId = userId
        BuildUi()
        LoadUser(canChangeRole)
    End Sub

    Private Sub BuildUi()
        Dim x0 As Integer = Theme.S(250)

        _avatar.Size = New Size(Theme.S(120), Theme.S(120))
        _avatar.Location = New Point(Theme.S(70), Theme.S(126))

        AddLink(_choose, "Choose photo", 62, 260)
        AddLink(_remove, "Remove", 158, 260)
        AddHandler _choose.LinkClicked, AddressOf OnChoosePhoto
        AddHandler _remove.LinkClicked, AddressOf OnRemovePhoto

        _fullName.Location = New Point(x0, Theme.S(112))
        _username.Location = New Point(x0, Theme.S(186))
        _role.Location = New Point(x0 + Theme.S(242), Theme.S(186))
        _contact.Location = New Point(x0, Theme.S(260))
        _address.Location = New Point(x0, Theme.S(334))

        Controls.Add(_avatar)
        Controls.Add(_choose)
        Controls.Add(_remove)
        Controls.Add(_fullName)
        Controls.Add(_username)
        Controls.Add(_role)
        Controls.Add(_contact)
        Controls.Add(_address)

        Dim nextY As Integer = Theme.S(408)
        If Not _isEdit Then
            _password.Location = New Point(x0, Theme.S(408))
            _confirm.Location = New Point(x0 + Theme.S(242), Theme.S(408))
            _confirm.IsPassword = True
            _confirm.MaxLength = 64
            _show.Text = "Show passwords"
            _show.AutoSize = True
            _show.Font = Theme.FontSmall
            _show.ForeColor = Theme.TextMuted
            _show.Location = New Point(x0, Theme.S(482))
            AddHandler _show.CheckedChanged, AddressOf OnShowChanged
            Controls.Add(_password)
            Controls.Add(_confirm)
            Controls.Add(_show)
            nextY = 510
        End If

        Message.Location = New Point(x0, nextY)
        Message.Size = New Size(Theme.S(470), Theme.S(34))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(140), Theme.S(44))
        _cancel.Location = New Point(x0 + Theme.S(470) - Theme.S(140) - Theme.S(156), nextY + Theme.S(44))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = If(_isEdit, "Save changes", "Create user")
        _save.Size = New Size(Theme.S(140), Theme.S(44))
        _save.Location = New Point(x0 + Theme.S(470) - Theme.S(140), nextY + Theme.S(44))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_cancel)
        Controls.Add(_save)
        ClientSize = New Size(Theme.S(760), nextY + Theme.S(44) + Theme.S(44) + Theme.S(26))

        AcceptButton = _save
        CancelButton = _cancel

        ' keep the initials in the picture in step with the typed name
        _nameTimer.Interval = 300
        AddHandler _nameTimer.Tick, AddressOf OnNameTimer
        _nameTimer.Start()
    End Sub

    Private Sub AddLink(link As LinkLabel, text As String, x As Integer, y As Integer)
        link.AutoSize = True
        link.Text = text
        link.Font = Theme.FontSmall
        link.LinkColor = Theme.Maroon
        link.ActiveLinkColor = Theme.MaroonDark
        link.VisitedLinkColor = Theme.Maroon
        link.LinkBehavior = LinkBehavior.HoverUnderline
        link.Location = New Point(x, y)
        link.Cursor = Cursors.Hand
    End Sub

    Private Sub LoadUser(canChangeRole As Boolean)
        Dim row As DataRow = Nothing
        Try
            row = UserRepository.GetUserForEdit(_userId)
        Catch ex As Exception
            ShowMessage("Could not load this user: " & ex.Message, True)
        End Try
        If row Is Nothing Then
            _save.Enabled = False
            ShowMessage("This user no longer exists.", True)
            Return
        End If

        _fullName.Value = row("full_name").ToString()
        _username.Value = row("username").ToString()
        _contact.Value = If(row("contact_number") Is DBNull.Value, "", row("contact_number").ToString())
        _address.Value = If(row("address") Is DBNull.Value, "", row("address").ToString())
        _avatar.DisplayName = _fullName.Value

        Dim cur As String = Permissions.NormalizeRole(row("role").ToString())
        If cur = "superadmin" Then
            _role.SetChoices(New String() {"Superadmin"})
            _role.Enabled = False
        ElseIf canChangeRole Then
            _role.SetChoices(New String() {"Admin", "Cashier"})
            _role.Value = Permissions.RoleDisplay(cur)
        Else
            _role.SetChoices(New String() {Permissions.RoleDisplay(cur)})
            _role.Enabled = False
        End If

        Try
            _avatar.Photo = UserRepository.GetPhoto(_userId)
        Catch
            _avatar.Photo = Nothing
        End Try
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _fullName.FocusInput()
    End Sub

    Private Sub OnNameTimer(sender As Object, e As EventArgs)
        _avatar.DisplayName = _fullName.Value
    End Sub

    Private Sub OnShowChanged(sender As Object, e As EventArgs)
        _password.IsPassword = Not _show.Checked
        _confirm.IsPassword = Not _show.Checked
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    ' ------------------------------------------------------------------ photo
    Private Sub OnChoosePhoto(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Using dlg As New OpenFileDialog()
            dlg.Title = "Choose a photo"
            dlg.Filter = "Pictures (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Dim err As String = ""
            Dim bytes As Byte() = PhotoHelper.ToAvatarBytes(dlg.FileName, err)
            If bytes Is Nothing Then
                ShowMessage(err, True)
                Return
            End If
            _photoBytes = bytes
            _photoChanged = True
            _avatar.Photo = PhotoHelper.FromBytes(bytes)
            ShowMessage("", False)
        End Using
    End Sub

    Private Sub OnRemovePhoto(sender As Object, e As LinkLabelLinkClickedEventArgs)
        _photoBytes = Nothing
        _photoChanged = True
        _avatar.Photo = Nothing
    End Sub

    ' ------------------------------------------------------------------ save
    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)

        Dim problem As String
        If _isEdit Then
            problem = InputField.CheckAll(_fullName, _username, _contact, _address)
        Else
            problem = InputField.CheckAll(_fullName, _username, _contact, _address, _password, _confirm)
        End If
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        Dim fullName As String = _fullName.Value.Trim()
        Dim username As String = _username.Value.Trim()
        Dim role As String = _role.Value.Trim()

        If role.Length = 0 Then
            ShowMessage("Choose a role.", True)
            Return
        End If
        If Not _isEdit Then
            problem = UserRules.ValidatePassword(_password.Value, username)
            If problem.Length > 0 Then
                ShowMessage(problem, True)
                _password.FocusInput()
                Return
            End If
            If _password.Value <> _confirm.Value Then
                ShowMessage("The two passwords do not match.", True)
                _confirm.FocusInput()
                Return
            End If
        End If

        _save.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim result As UserSaveResult
            If _isEdit Then
                result = UserRepository.UpdateUser(_userId, fullName, username, _contact.Value, _address.Value,
                                                   role, _photoBytes, _photoChanged)
            Else
                result = UserRepository.CreateUser(fullName, username, _contact.Value, _address.Value,
                                                   role, _password.Value, _photoBytes)
            End If

            Select Case result
                Case UserSaveResult.Ok
                    SavedFullName = fullName
                    SavedUsername = username
                    If _isEdit Then
                        AuditLog.Write("EDIT", "Users", "Edited " & Permissions.RoleDisplay(role) & " '" & username & "'")
                    Else
                        AuditLog.Write("ADD", "Users", "Added " & Permissions.RoleDisplay(role) & " '" & username & "'")
                    End If
                    DialogResult = DialogResult.OK
                Case UserSaveResult.UsernameTaken
                    ShowMessage("That username is already taken.", True)
                    _username.FocusInput()
                Case UserSaveResult.LimitReached
                    ShowMessage("The limit of " & UserRules.LimitFor(role) & " " & Permissions.RoleDisplay(role) &
                                " accounts has been reached.", True)
                Case Else
                    ShowMessage("This change is not allowed (the account may have been changed by someone else).", True)
            End Select
        Catch ex As Exception
            ShowMessage("Could not save: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            If DialogResult <> DialogResult.OK Then _save.Enabled = True
        End Try
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _nameTimer.Stop()
            _nameTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

''' <summary>An admin types a new password for someone (passwords are never shown anywhere).</summary>
Public Class ResetPasswordForm
    Inherits ModalDialog

    Private ReadOnly _userId As Integer
    Private ReadOnly _username As String
    Private ReadOnly _password As New InputField("New password (8+ characters)", InputKind.Password, 400, True)
    Private ReadOnly _confirm As New InputField("Confirm new password", InputKind.Text, 400, True)
    Private ReadOnly _show As New CheckBox()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    Public Sub New(userId As Integer, displayName As String, username As String)
        MyBase.New("Reset password", "Set a new password for " & displayName & ".", 480, 430)
        _userId = userId
        _username = username

        _password.Location = New Point(Theme.S(40), Theme.S(112))
        _confirm.Location = New Point(Theme.S(40), Theme.S(186))
        _confirm.IsPassword = True
        _confirm.MaxLength = 64

        _show.Text = "Show passwords"
        _show.AutoSize = True
        _show.Font = Theme.FontSmall
        _show.ForeColor = Theme.TextMuted
        _show.Location = New Point(Theme.S(40), Theme.S(262))
        AddHandler _show.CheckedChanged, AddressOf OnShowChanged

        Message.Location = New Point(Theme.S(40), Theme.S(292))
        Message.Size = New Size(Theme.S(400), Theme.S(34))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(140), Theme.S(44))
        _cancel.Location = New Point(Theme.S(144), Theme.S(346))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = "Set password"
        _save.Size = New Size(Theme.S(140), Theme.S(44))
        _save.Location = New Point(Theme.S(300), Theme.S(346))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_password)
        Controls.Add(_confirm)
        Controls.Add(_show)
        Controls.Add(_cancel)
        Controls.Add(_save)

        AcceptButton = _save
        CancelButton = _cancel
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _password.FocusInput()
    End Sub

    Private Sub OnShowChanged(sender As Object, e As EventArgs)
        _password.IsPassword = Not _show.Checked
        _confirm.IsPassword = Not _show.Checked
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Dim problem As String = InputField.CheckAll(_password, _confirm)
        If problem.Length = 0 Then problem = UserRules.ValidatePassword(_password.Value, _username)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            _password.FocusInput()
            Return
        End If
        If _password.Value <> _confirm.Value Then
            ShowMessage("The two passwords do not match.", True)
            _confirm.FocusInput()
            Return
        End If

        _save.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            If UserRepository.SetPassword(_userId, _password.Value) Then
                AuditLog.Write("RESET_PASSWORD", "Users", "Password reset for '" & _username & "'")
                DialogResult = DialogResult.OK
            Else
                ShowMessage("This user no longer exists.", True)
            End If
        Catch ex As Exception
            ShowMessage("Could not save: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            If DialogResult <> DialogResult.OK Then _save.Enabled = True
        End Try
    End Sub
End Class
