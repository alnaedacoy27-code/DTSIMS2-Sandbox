Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' File Maintenance > Users. Minimal list on the left, details of the selected person on the right.
''' Refreshes by itself when anyone changes a user (ILiveForm).
''' </summary>
Public Class UsersForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly DetailsWidth As Integer = Theme.S(340)
    Private Shared ReadOnly ContentW As Integer = Theme.S(268)

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly BigNameFont As Font = Theme.MakeFont("Segoe UI Semibold", 15.0F)

    ' header
    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _search As New SearchBox("Search users...")
    Private ReadOnly _add As New MaroonButton()

    ' counters
    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cardSuper As New StatCard()
    Private ReadOnly _cardAdmin As New StatCard()
    Private ReadOnly _cardCashier As New StatCard()
    Private ReadOnly _cardOnline As New StatCard()
    Private ReadOnly _cardLocked As New StatCard()

    ' list
    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()

    ' details
    Private ReadOnly _details As New Panel()
    Private ReadOnly _scroll As New FlowLayoutPanel()
    Private ReadOnly _bar As New FlowLayoutPanel()
    Private ReadOnly _avatar As New Avatar()
    Private ReadOnly _dName As New Label()
    Private ReadOnly _dUser As New Label()
    Private ReadOnly _pillRow As New Panel()
    Private ReadOnly _pillRole As New PillLabel()
    Private ReadOnly _pillStatus As New PillLabel()
    Private ReadOnly _vContact As Label
    Private ReadOnly _vAddress As Label
    Private ReadOnly _vLogin As Label
    Private ReadOnly _vDevice As Label
    Private ReadOnly _vCreated As Label
    Private ReadOnly _btnEdit As New MaroonButton()
    Private ReadOnly _btnReset As New MaroonButton()
    Private ReadOnly _btnDisable As New MaroonButton()
    Private ReadOnly _btnUnlock As New MaroonButton()
    Private ReadOnly _btnDelete As New MaroonButton()

    Private ReadOnly _presenceTimer As New Timer()
    Private _presenceSig As String = ""
    Private _loading As Boolean
    Private _selectedId As Integer

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildStats()
        BuildGrid()
        _vContact = BuildDetails("CONTACT NUMBER", 24)
        _vAddress = BuildDetailsRow("ADDRESS", 44)
        _vLogin = BuildDetailsRow("LAST LOGIN", 24)
        _vDevice = BuildDetailsRow("LOGGED IN ON", 24)
        _vCreated = BuildDetailsRow("ACCOUNT CREATED", 24)
        BuildActions()

        ' Fill first; header ends up back-most so it docks first (top-most).
        Controls.Add(_listHost)
        Controls.Add(_details)
        Controls.Add(_stats)
        Controls.Add(_header)
    End Sub

    ' ------------------------------------------------------------------ live
    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"users"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadUsers()
    End Sub

    ' ------------------------------------------------------------------ building the screen
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Users"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "Manage who can log in and what they can do"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _search.Size = New Size(Theme.S(280), Theme.S(40))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _add.Text = "+  Add user"
        _add.Size = New Size(Theme.S(130), Theme.S(40))
        AddHandler _add.Click, AddressOf OnAdd

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)
        _header.Controls.Add(_search)
        _header.Controls.Add(_add)
        AddHandler _header.Resize, AddressOf OnHeaderResize
        OnHeaderResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnHeaderResize(sender As Object, e As EventArgs)
        _add.Location = New Point(Math.Max(Theme.S(300), _header.ClientSize.Width - Theme.S(32) - _add.Width), Theme.S(30))
        _search.Location = New Point(Math.Max(0, _add.Left - Theme.S(16) - _search.Width), Theme.S(30))
    End Sub

    Private Sub BuildStats()
        _stats.Dock = DockStyle.Top
        _stats.Height = Theme.S(84)
        _stats.BackColor = Color.White
        _stats.FlowDirection = FlowDirection.LeftToRight
        _stats.WrapContents = False
        _stats.Padding = New Padding(Theme.S(32), Theme.S(4), 0, 0)

        For Each c As StatCard In New StatCard() {_cardSuper, _cardAdmin, _cardCashier, _cardOnline, _cardLocked}
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            _stats.Controls.Add(c)
        Next
    End Sub

    Private Sub BuildGrid()
        _listHost.Dock = DockStyle.Fill
        _listHost.BackColor = Color.White
        _listHost.Padding = New Padding(Theme.S(32), Theme.S(4), Theme.S(16), Theme.S(16))

        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 60)

        AddColumn("user", "USER", "full_name", 36)
        AddColumn("role", "ROLE", "role", 14)
        AddColumn("contact", "CONTACT", "contact_number", 18)
        AddColumn("status", "STATUS", "status", 16)
        AddColumn("last_login", "LAST LOGIN", "last_login", 18)

        AddHandler _grid.CellPainting, AddressOf OnCellPainting
        AddHandler _grid.CellFormatting, AddressOf OnCellFormatting
        AddHandler _grid.SelectionChanged, AddressOf OnGridSelectionChanged
        AddHandler _grid.CellDoubleClick, AddressOf OnGridDoubleClick

        _empty.Dock = DockStyle.Fill
        _empty.TextAlign = ContentAlignment.MiddleCenter
        _empty.Font = Theme.FontHeading
        _empty.ForeColor = Theme.TextMuted
        _empty.BackColor = Color.White
        _empty.Visible = False

        _listHost.Controls.Add(_grid)
        _listHost.Controls.Add(_empty)
    End Sub

    Private Sub AddColumn(name As String, headerText As String, dataProp As String, weight As Single)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = name
        c.HeaderText = headerText
        c.DataPropertyName = dataProp
        c.FillWeight = weight
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        _grid.Columns.Add(c)
    End Sub

    Private Function BuildDetails(caption As String, valueHeight As Integer) As Label
        ' the top of the details panel: avatar, name, username, pills, then the first info row
        _details.Dock = DockStyle.Right
        _details.Width = DetailsWidth
        _details.BackColor = Color.FromArgb(251, 249, 249)
        DetailsLayout.Setup(_details, _scroll, _bar, New Padding(Theme.S(28), Theme.S(22), Theme.S(12), Theme.S(16)))
        AddHandler _scroll.Paint, AddressOf OnDetailsPaint

        _avatar.Size = New Size(Theme.S(96), Theme.S(96))
        _avatar.Margin = New Padding((ContentW - Theme.S(96)) \ 2, 0, 0, Theme.S(10))
        _scroll.Controls.Add(_avatar)

        _dName.AutoSize = False
        _dName.Size = New Size(ContentW, Theme.S(30))
        _dName.TextAlign = ContentAlignment.MiddleCenter
        _dName.Font = BigNameFont
        _dName.ForeColor = Theme.TextDark
        _dName.Margin = New Padding(0)
        _scroll.Controls.Add(_dName)

        _dUser.AutoSize = False
        _dUser.Size = New Size(ContentW, Theme.S(22))
        _dUser.TextAlign = ContentAlignment.MiddleCenter
        _dUser.Font = Theme.FontBase
        _dUser.ForeColor = Theme.TextMuted
        _dUser.Margin = New Padding(0, 0, 0, Theme.S(6))
        _scroll.Controls.Add(_dUser)

        _pillRow.Size = New Size(ContentW, Theme.S(32))
        _pillRow.BackColor = _details.BackColor
        _pillRow.Margin = New Padding(0, 0, 0, Theme.S(8))
        _pillRow.Controls.Add(_pillRole)
        _pillRow.Controls.Add(_pillStatus)
        _scroll.Controls.Add(_pillRow)

        Return BuildDetailsRow(caption, valueHeight)
    End Function

    Private Function BuildDetailsRow(caption As String, valueHeight As Integer) As Label
        Dim cap As New Label()
        cap.AutoSize = False
        cap.Size = New Size(ContentW, Theme.S(18))
        cap.Font = Theme.FontCaption
        cap.ForeColor = Theme.TextMuted
        cap.Text = caption
        cap.Margin = New Padding(0, Theme.S(12), 0, 0)
        _scroll.Controls.Add(cap)

        Dim v As New Label()
        v.AutoSize = False
        v.Size = New Size(ContentW, valueHeight)
        v.Font = Theme.FontBase
        v.ForeColor = Theme.TextDark
        v.Margin = New Padding(0)
        _scroll.Controls.Add(v)
        Return v
    End Function

    Private Sub BuildActions()
        _btnEdit.Text = "Edit user"
        _btnReset.Text = "Reset password"
        _btnDisable.Text = "Disable account"
        _btnUnlock.Text = "Unlock account"
        _btnDelete.Text = "Delete user"

        _btnReset.MakeSecondary()
        _btnDisable.MakeSecondary()
        _btnUnlock.MakeSecondary()
        _btnDelete.MakeDanger()

        AddHandler _btnEdit.Click, AddressOf OnEdit
        AddHandler _btnReset.Click, AddressOf OnResetPassword
        AddHandler _btnDisable.Click, AddressOf OnToggleActive
        AddHandler _btnUnlock.Click, AddressOf OnUnlock
        AddHandler _btnDelete.Click, AddressOf OnDelete

        Dim first As Boolean = True
        For Each b As MaroonButton In New MaroonButton() {_btnEdit, _btnReset, _btnDisable, _btnUnlock, _btnDelete}
            b.Size = New Size(ContentW, Theme.S(40))
            b.Margin = New Padding(0, If(first, 0, Theme.S(8)), 0, 0)
            first = False
            _bar.Controls.Add(b)
        Next
    End Sub

    ' ------------------------------------------------------------------ data
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadUsers()

        ' "Active" = logged in right now. Logins and logouts do not touch the users table,
        ' so look at who is online every few seconds and refresh only when it changes.
        _presenceTimer.Interval = 4000
        AddHandler _presenceTimer.Tick, AddressOf OnPresenceTick
        _presenceTimer.Start()
    End Sub

    Private Sub OnPresenceTick(sender As Object, e As EventArgs)
        Try
            Dim sig As String = UserRepository.GetPresenceSignature()
            If sig <> _presenceSig Then LoadUsers()
        Catch
            ' database offline: the main window already shows it
        End Try
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _presenceTimer.Stop()
            _presenceTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub OnSearchChanged()
        LoadUsers()
    End Sub

    Private Sub LoadUsers()
        If _loading Then Return
        _loading = True
        Try
            Dim keep As Integer = _selectedId
            Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex

            Dim dt As DataTable = UserRepository.GetUsers(AppSession.Role, _search.Query)
            _grid.DataSource = dt
            UpdateStats(UserRepository.GetCounts(AppSession.Role))
            _presenceSig = UserRepository.GetPresenceSignature()
            _add.Enabled = UserRules.CanAddUsers(AppSession.Role)

            If dt.Rows.Count = 0 Then
                _empty.Text = If(_search.Query.Length > 0, "No users match your search.", "No users yet.")
                _empty.Visible = True
                _empty.BringToFront()
            Else
                _empty.Visible = False
            End If

            _grid.ClearSelection()
            Dim target As DataGridViewRow = Nothing
            For Each r As DataGridViewRow In _grid.Rows
                Dim drv As DataRowView = TryCast(r.DataBoundItem, DataRowView)
                If drv IsNot Nothing AndAlso Convert.ToInt32(drv("id")) = keep Then
                    target = r
                    Exit For
                End If
            Next
            If target Is Nothing AndAlso _grid.Rows.Count > 0 Then target = _grid.Rows(0)
            If target IsNot Nothing Then
                target.Selected = True
                _grid.CurrentCell = target.Cells(0)
            End If
            If scroll >= 0 AndAlso scroll < _grid.Rows.Count Then
                _grid.FirstDisplayedScrollingRowIndex = scroll
            End If

        Catch ex As Exception
            _empty.Text = "Could not load users: " & ex.Message
            _empty.Visible = True
            _empty.BringToFront()
        Finally
            _loading = False
        End Try
        ShowDetails()
    End Sub

    Private Sub UpdateStats(c As UserCounts)
        _cardSuper.Visible = (Permissions.NormalizeRole(AppSession.Role) = "superadmin")
        _cardSuper.Value = c.Superadmins & " / " & UserRules.MaxSuperadmins
        _cardSuper.Caption = "Superadmin"

        _cardAdmin.Value = c.Admins & " / " & UserRules.MaxAdmins
        _cardAdmin.Caption = "Admins"
        _cardAdmin.Warn = (c.Admins >= UserRules.MaxAdmins)

        _cardCashier.Value = c.Cashiers & " / " & UserRules.MaxCashiers
        _cardCashier.Caption = "Cashiers"
        _cardCashier.Warn = (c.Cashiers >= UserRules.MaxCashiers)

        _cardOnline.Value = c.Online.ToString()
        _cardOnline.Caption = "Active now"

        _cardLocked.Value = c.Locked.ToString()
        _cardLocked.Caption = "Locked accounts"
        _cardLocked.Warn = (c.Locked > 0)
    End Sub

    ' ------------------------------------------------------------------ grid painting
    Private Sub OnCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        If colName <> "user" AndAlso colName <> "role" AndAlso colName <> "status" Then Return

        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Select Case colName
            Case "role"
                Dim role As String = row("role").ToString()
                Pills.Draw(e.Graphics, e.CellBounds, Permissions.RoleDisplay(role), Pills.RoleBack(role), Pills.RoleFore(role))

            Case "status"
                Dim status As String = row("status").ToString()
                Pills.Draw(e.Graphics, e.CellBounds, status, Pills.StatusBack(status), Pills.StatusFore(status))

            Case "user"
                Dim c As Rectangle = e.CellBounds
                Dim fullName As String = row("full_name").ToString()
                Dim circle As New Rectangle(c.X + Theme.S(12), c.Y + (c.Height - Theme.S(38)) \ 2, Theme.S(38), Theme.S(38))
                Avatar.PaintInitials(e.Graphics, circle, fullName, 10.0F)

                Dim textX As Integer = circle.Right + Theme.S(12)
                Dim textW As Integer = Math.Max(Theme.S(10), c.Right - textX - Theme.S(8))
                TextRenderer.DrawText(e.Graphics, fullName, NameFont, New Rectangle(textX, c.Y + 11, textW, 20),
                                      Theme.TextDark, TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)
                TextRenderer.DrawText(e.Graphics, "@" & row("username").ToString(), Theme.FontSmall, New Rectangle(textX, c.Y + 32, textW, 18),
                                      Theme.TextMuted, TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)
        End Select

        e.Handled = True
    End Sub

    Private Sub OnCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim colName As String = _grid.Columns(e.ColumnIndex).Name

        If colName = "contact" Then
            e.Value = TextOrDash(e.Value)
            e.FormattingApplied = True
        ElseIf colName = "last_login" Then
            If e.Value Is Nothing OrElse e.Value Is DBNull.Value Then
                e.Value = "Never"
            Else
                e.Value = FriendlyDate(Convert.ToDateTime(e.Value))
            End If
            e.FormattingApplied = True
        End If
    End Sub

    Private Shared Function TextOrDash(o As Object) As String
        If o Is Nothing OrElse o Is DBNull.Value Then Return "-"
        Dim s As String = o.ToString().Trim()
        Return If(s.Length = 0, "-", s)
    End Function

    Private Shared Function FriendlyDate(d As DateTime) As String
        If d.Date = DateTime.Today Then Return "Today, " & d.ToString("h:mm tt")
        If d.Date = DateTime.Today.AddDays(-1) Then Return "Yesterday, " & d.ToString("h:mm tt")
        Return d.ToString("MMM d, yyyy")
    End Function

    ' ------------------------------------------------------------------ details panel
    Private Sub OnGridSelectionChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        ShowDetails()
    End Sub

    Private Sub OnGridDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 AndAlso _btnEdit.Enabled Then OnEdit(sender, EventArgs.Empty)
    End Sub

    Private Function SelectedRow() As DataRowView
        If _grid.SelectedRows.Count = 0 Then Return Nothing
        Return TryCast(_grid.SelectedRows(0).DataBoundItem, DataRowView)
    End Function

    Private Sub ShowDetails()
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then
            _selectedId = 0
            _details.Visible = False
            Return
        End If
        _details.Visible = True

        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim role As String = row("role").ToString()
        Dim status As String = row("status").ToString()
        Dim fullName As String = row("full_name").ToString()
        _selectedId = id

        _dName.Text = fullName
        _dUser.Text = "@" & row("username").ToString()
        _avatar.DisplayName = fullName

        Dim photo As Image = Nothing
        If Convert.ToInt32(row("has_photo")) = 1 Then
            Try
                photo = UserRepository.GetPhoto(id)
            Catch
                photo = Nothing
            End Try
        End If
        _avatar.Photo = photo

        _pillRole.SetPill(Permissions.RoleDisplay(role), Pills.RoleBack(role), Pills.RoleFore(role))
        _pillStatus.SetPill(status, Pills.StatusBack(status), Pills.StatusFore(status))
        Dim total As Integer = _pillRole.Width + Theme.S(8) + _pillStatus.Width
        Dim startX As Integer = Math.Max(0, (_pillRow.Width - total) \ 2)
        _pillRole.Location = New Point(startX, Theme.S(4))
        _pillStatus.Location = New Point(startX + _pillRole.Width + Theme.S(8), Theme.S(4))

        _vContact.Text = TextOrDash(row("contact_number"))
        _vAddress.Text = TextOrDash(row("address"))
        _vLogin.Text = If(row("last_login") Is DBNull.Value, "Never", FriendlyDate(Convert.ToDateTime(row("last_login"))))
        _vCreated.Text = Convert.ToDateTime(row("created_at")).ToString("MMM d, yyyy")
        _vDevice.Text = If(status = "Active", TextOrDash(row("device")), "-")

        Dim manage As Boolean = UserRules.CanManage(AppSession.Role, role, AppSession.UserId, id)
        _btnEdit.Enabled = manage OrElse id = AppSession.UserId
        _btnReset.Enabled = manage
        _btnDisable.Enabled = manage
        _btnDisable.Text = If(status = "Disabled", "Enable account", "Disable account")
        _btnUnlock.Enabled = manage AndAlso status = "Locked"
        _btnDelete.Enabled = manage
    End Sub

    Private Sub OnDetailsPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _details.Height)
        End Using
    End Sub

    ' ------------------------------------------------------------------ actions
    ''' <summary>The main window (dialogs and messages belong to it, not to this embedded screen).</summary>
    Private Function OwnerWindow() As IWin32Window
        Dim f As Form = Nothing
        If Parent IsNot Nothing Then f = Parent.FindForm()
        If f Is Nothing Then Return Me
        Return f
    End Function

    Private Sub Notify(text As String, icon As MessageBoxIcon)
        MessageBox.Show(OwnerWindow(), text, "Users", MessageBoxButtons.OK, icon)
    End Sub

    Private Function Confirm(text As String, icon As MessageBoxIcon) As Boolean
        Return MessageBox.Show(OwnerWindow(), text, "Users", MessageBoxButtons.YesNo, icon,
                               MessageBoxDefaultButton.Button2) = DialogResult.Yes
    End Function

    ''' <summary>The selected person, with their role read fresh from the database (the list may be a second old).</summary>
    Private Function GetTarget(ByRef id As Integer, ByRef role As String, ByRef fullName As String, ByRef username As String) As Boolean
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return False

        id = Convert.ToInt32(row("id"))
        fullName = row("full_name").ToString()
        username = row("username").ToString()
        Try
            role = UserRepository.GetRole(id)
        Catch ex As Exception
            Notify("Could not reach the database: " & ex.Message, MessageBoxIcon.Error)
            Return False
        End Try

        If role.Length = 0 Then
            Notify("This user no longer exists.", MessageBoxIcon.Information)
            LoadUsers()
            Return False
        End If
        Return True
    End Function

    Private Sub OnAdd(sender As Object, e As EventArgs)
        If Not UserRules.CanAddUsers(AppSession.Role) Then Return
        Using dlg As New UserEditForm()
            If dlg.ShowDialog(OwnerWindow()) = DialogResult.OK Then LoadUsers()
        End Using
    End Sub

    Private Sub OnEdit(sender As Object, e As EventArgs)
        Dim id As Integer, role As String = "", fullName As String = "", username As String = ""
        If Not GetTarget(id, role, fullName, username) Then Return

        Dim isMe As Boolean = (id = AppSession.UserId)
        Dim manage As Boolean = UserRules.CanManage(AppSession.Role, role, AppSession.UserId, id)
        If Not (manage OrElse isMe) Then
            Notify("You do not have permission to edit this user.", MessageBoxIcon.Warning)
            Return
        End If

        Dim canChangeRole As Boolean = manage AndAlso Permissions.NormalizeRole(AppSession.Role) = "superadmin"
        Using dlg As New UserEditForm(id, canChangeRole)
            If dlg.ShowDialog(OwnerWindow()) = DialogResult.OK Then
                If isMe Then
                    AppSession.FullName = dlg.SavedFullName
                    AppSession.Username = dlg.SavedUsername
                End If
                LoadUsers()
            End If
        End Using
    End Sub

    Private Sub OnResetPassword(sender As Object, e As EventArgs)
        Dim id As Integer, role As String = "", fullName As String = "", username As String = ""
        If Not GetTarget(id, role, fullName, username) Then Return
        If Not UserRules.CanManage(AppSession.Role, role, AppSession.UserId, id) Then
            Notify("You do not have permission to reset this password.", MessageBoxIcon.Warning)
            Return
        End If

        Using dlg As New ResetPasswordForm(id, fullName, username)
            If dlg.ShowDialog(OwnerWindow()) = DialogResult.OK Then
                Notify("The password for " & fullName & " was changed." & Environment.NewLine &
                       "They can log in with the new password right away.", MessageBoxIcon.Information)
                LoadUsers()
            End If
        End Using
    End Sub

    Private Sub OnToggleActive(sender As Object, e As EventArgs)
        Dim id As Integer, role As String = "", fullName As String = "", username As String = ""
        If Not GetTarget(id, role, fullName, username) Then Return
        If Not UserRules.CanManage(AppSession.Role, role, AppSession.UserId, id) Then
            Notify("You do not have permission to change this account.", MessageBoxIcon.Warning)
            Return
        End If

        Dim row As DataRowView = SelectedRow()
        Dim isActive As Boolean = (row IsNot Nothing AndAlso Convert.ToInt32(row("is_active")) = 1)

        If isActive Then
            If Not Confirm("Disable the account of " & fullName & "?" & Environment.NewLine & Environment.NewLine &
                           "They will not be able to log in, and if they are logged in now they will be logged out.",
                           MessageBoxIcon.Warning) Then Return
        End If

        Try
            If UserRepository.SetActive(id, Not isActive) Then
                AuditLog.Write(If(isActive, "DISABLE", "ENABLE"), "Users",
                               If(isActive, "Disabled", "Enabled") & " account '" & username & "'")
            End If
        Catch ex As Exception
            Notify("Could not change the account: " & ex.Message, MessageBoxIcon.Error)
        End Try
        LoadUsers()
    End Sub

    Private Sub OnUnlock(sender As Object, e As EventArgs)
        Dim id As Integer, role As String = "", fullName As String = "", username As String = ""
        If Not GetTarget(id, role, fullName, username) Then Return
        If Not UserRules.CanManage(AppSession.Role, role, AppSession.UserId, id) Then
            Notify("You do not have permission to unlock this account.", MessageBoxIcon.Warning)
            Return
        End If

        Try
            If UserRepository.Unlock(id) Then
                AuditLog.Write("UNLOCK", "Users", "Unlocked account '" & username & "'")
            End If
        Catch ex As Exception
            Notify("Could not unlock the account: " & ex.Message, MessageBoxIcon.Error)
        End Try
        LoadUsers()
    End Sub

    Private Sub OnDelete(sender As Object, e As EventArgs)
        Dim id As Integer, role As String = "", fullName As String = "", username As String = ""
        If Not GetTarget(id, role, fullName, username) Then Return
        If Not UserRules.CanManage(AppSession.Role, role, AppSession.UserId, id) Then
            Notify("You do not have permission to delete this user.", MessageBoxIcon.Warning)
            Return
        End If

        If Not Confirm("Delete " & fullName & " (@" & username & ") permanently?" & Environment.NewLine & Environment.NewLine &
                       "This cannot be undone. Their past sales stay in the reports." & Environment.NewLine &
                       "If you only want to stop them logging in, use Disable instead.",
                       MessageBoxIcon.Warning) Then Return

        Try
            If UserRepository.DeleteUser(id) Then
                AuditLog.Write("DELETE", "Users", "Deleted " & Permissions.RoleDisplay(role) & " '" & username & "'")
            End If
        Catch ex As Exception
            Notify("Could not delete the user: " & ex.Message, MessageBoxIcon.Error)
        End Try
        _selectedId = 0
        LoadUsers()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'UsersForm
        '
        Me.ClientSize = New System.Drawing.Size(284, 261)
        Me.Name = "UsersForm"
        Me.ResumeLayout(False)

    End Sub

    Private Sub UsersForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class
