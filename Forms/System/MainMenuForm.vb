Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' The main window: maroon menu bar, optional low-stock marquee, a content area where
''' each module opens, and a status bar. Resizable / maximizable / minimizable.
''' </summary>
Public Class MainMenuForm
    Inherits Form

    Private ReadOnly _menu As New MenuStrip()
    Private ReadOnly _marquee As New MarqueePanel()
    Private ReadOnly _content As New Panel()
    Private ReadOnly _status As New StatusStrip()
    Private ReadOnly _lblUser As New ToolStripStatusLabel()
    Private ReadOnly _lblSpacer As New ToolStripStatusLabel()
    Private ReadOnly _lblServer As New ToolStripStatusLabel()
    Private ReadOnly _lblClock As New ToolStripStatusLabel()
    Private ReadOnly _clock As New Timer()
    Private ReadOnly _live As New LiveSync()
    Private _lowStockSig As String = ""
    Private _posMode As Boolean
    Private _prevBorder As FormBorderStyle
    Private _prevState As FormWindowState
    Private _prevBounds As Rectangle

    ''' <summary>True when the user chose Log out (so the app can show the login form again).</summary>
    Public Property LoggedOut As Boolean

    ''' <summary>True when the Superadmin was deleted while running: the app must show the setup form again.</summary>
    Public Property RequireSetup As Boolean

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        Text = "Darwin Trading - Sales and Inventory System"
        Font = Theme.FontBase
        BackColor = Color.White
        MinimumSize = New Size(1000, 640)
        Size = New Size(1280, 780)
        StartPosition = FormStartPosition.CenterScreen
        KeyPreview = True

        _content.Dock = DockStyle.Fill
        _content.BackColor = Color.White
        ' scroll bars when the window is smaller than the screen needs (bigger text needs more room)
        _content.AutoScroll = True
        _content.AutoScrollMinSize = New Size(CInt(1000.0F * Theme.FontScale / 1.12F), CInt(540.0F * Theme.FontScale / 1.12F))

        _marquee.Dock = DockStyle.Top

        _menu.Dock = DockStyle.Top
        _menu.GripStyle = ToolStripGripStyle.Hidden
        _menu.Renderer = New MaroonMenuRenderer()

        _status.Dock = DockStyle.Bottom
        _status.Renderer = New MaroonMenuRenderer()
        _status.SizingGrip = True
        _lblSpacer.Spring = True
        SetServerLabel(True)
        _status.Items.AddRange(New ToolStripItem() {_lblUser, _lblSpacer, _lblServer, _lblClock})

        ' Fill first, then the docked bars. Back-most control docks first (top-most).
        Controls.Add(_content)
        Controls.Add(_marquee)
        Controls.Add(_menu)
        Controls.Add(_status)
        _content.BringToFront()
        _status.SendToBack()
        _marquee.SendToBack()
        _menu.SendToBack()
        MainMenuStrip = _menu

        _clock.Interval = 1000
        AddHandler _clock.Tick, AddressOf OnClockTick

        RebuildForRole()

    End Sub

    ' ------------------------------------------------------------------ build / rebuild
    Private Sub RebuildForRole()
        BuildMenu()
        UpdateStatus()
        ShowHome()
        _lowStockSig = ""
        RefreshLowStock()
    End Sub

    ' The red moving line: items at or under their minimum stock (Superadmin and Admins only).
    Private Sub RefreshLowStock()
        If _posMode Then Return
        Dim role As String = Permissions.NormalizeRole(AppSession.Role)
        If role <> "superadmin" AndAlso role <> "admin" Then
            _marquee.SetMessages(Nothing)
            Return
        End If
        Try
            Dim messages As List(Of String) = LowStockAlerts.GetMessages()
            Dim sig As String = String.Join("|", messages.ToArray())
            If sig = _lowStockSig Then Return
            _lowStockSig = sig
            _marquee.SetMessages(messages)
        Catch
            ' database offline: keep what is showing
        End Try
    End Sub

    Private Sub BuildMenu()
        _menu.Items.Clear()

        Dim home As New ToolStripMenuItem("Home")
        AddHandler home.Click, AddressOf OnHomeClick
        _menu.Items.Add(home)

        For Each groupName As String In ModuleCatalog.GroupOrder
            Dim group As New ToolStripMenuItem(groupName)
            For Each m As ModuleInfo In ModuleCatalog.All
                If m.Group <> groupName Then Continue For
                If Not Permissions.CanAccess(AppSession.Role, m.Key) Then Continue For

                Dim item As New ToolStripMenuItem(m.Title)
                item.Tag = m
                If m.Shortcut <> Keys.None Then item.ShortcutKeys = m.Shortcut
                AddHandler item.Click, AddressOf OnMenuItemClick
                group.DropDownItems.Add(item)
            Next
            If group.DropDownItems.Count > 0 Then _menu.Items.Add(group)
        Next

        Dim acct As New ToolStripMenuItem("Account")
        acct.Alignment = ToolStripItemAlignment.Right
        Dim who As New ToolStripMenuItem(AppSession.FullName & "  (" & Permissions.RoleDisplay(AppSession.Role) & ")")
        who.Enabled = False
        Dim logout As New ToolStripMenuItem("Log out")
        AddHandler logout.Click, AddressOf OnLogoutClick
        Dim quit As New ToolStripMenuItem("Exit")
        AddHandler quit.Click, AddressOf OnExitClick
        acct.DropDownItems.AddRange(New ToolStripItem() {who, New ToolStripSeparator(), logout, quit})
        _menu.Items.Add(acct)

        StyleMenu()
    End Sub

    Private Sub StyleMenu()
        _menu.Font = Theme.FontMenu
        _menu.Padding = New Padding(10, 4, 10, 4)

        For Each top As ToolStripItem In _menu.Items
            top.Padding = New Padding(14, 6, 14, 6)

            Dim topItem As ToolStripMenuItem = TryCast(top, ToolStripMenuItem)
            If topItem Is Nothing Then Continue For

            Dim dd As ToolStripDropDownMenu = TryCast(topItem.DropDown, ToolStripDropDownMenu)
            If dd IsNot Nothing Then
                dd.ShowImageMargin = False
                dd.ShowCheckMargin = False
                dd.BackColor = Color.White
                dd.Font = Theme.FontMenu
                dd.Renderer = _menu.Renderer
            End If

            For Each child As ToolStripItem In topItem.DropDownItems
                child.Padding = New Padding(10, 7, 10, 7)
            Next
        Next
    End Sub

    Private Sub UpdateStatus()
        _lblUser.Text = "  " & AppSession.FullName & "  |  " & Permissions.RoleDisplay(AppSession.Role)
        OnClockTick(Me, EventArgs.Empty)
    End Sub

    ' ------------------------------------------------------------------ content area
    Private Sub ClearContent()
        For i As Integer = _content.Controls.Count - 1 To 0 Step -1
            Dim c As Control = _content.Controls(i)
            _content.Controls.RemoveAt(i)
            c.Dispose()
        Next
    End Sub

    Private Sub ShowHome()
        If _posMode Then ExitPosMode()
        Dim homeView As New HomeView(AppSession.FullName, AppSession.Role, ModuleCatalog.ForRole(AppSession.Role))
        AddHandler homeView.ModuleRequested, AddressOf OpenModule
        ClearContent()
        _content.Controls.Add(homeView)
        SessionTracker.SetModuleAsync("Home")
    End Sub

    Private Sub OpenModule(info As ModuleInfo)
        If Not Permissions.CanAccess(AppSession.Role, info.Key) Then
            MessageBox.Show(Me, "You do not have access to " & info.Title & ".", "Access denied",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If _posMode Then ExitPosMode()

        Dim frm As Form = CreateModuleForm(info)
        frm.TopLevel = False
        frm.FormBorderStyle = FormBorderStyle.None
        frm.Dock = DockStyle.Fill

        ClearContent()
        _content.Controls.Add(frm)
        frm.Show()
        SessionTracker.SetModuleAsync(info.Title)

        ' the POS is POS only: full screen, no menu, no low-stock line, and its own Exit button
        Dim pos As PosForm = TryCast(frm, PosForm)
        If pos IsNot Nothing Then
            AddHandler pos.ExitRequested, AddressOf OnPosExit
            EnterPosMode()
        End If
    End Sub

    Private Sub EnterPosMode()
        If _posMode Then Return
        _posMode = True
        _prevBorder = FormBorderStyle
        _prevState = WindowState
        _prevBounds = Bounds

        _menu.Visible = False
        _marquee.Visible = False
        _status.Visible = False

        WindowState = FormWindowState.Normal
        FormBorderStyle = FormBorderStyle.None
        Bounds = Screen.FromControl(Me).Bounds
    End Sub

    Private Sub ExitPosMode()
        If Not _posMode Then Return
        _posMode = False

        FormBorderStyle = _prevBorder
        If _prevState = FormWindowState.Maximized Then
            WindowState = FormWindowState.Maximized
        Else
            WindowState = FormWindowState.Normal
            Bounds = _prevBounds
        End If

        _menu.Visible = True
        _status.Visible = True
        _lowStockSig = ""
        RefreshLowStock()
    End Sub

    Private Sub OnPosExit()
        ShowHome()
    End Sub

    ''' <summary>Real module forms get plugged in here, one case at a time, as we build them.</summary>
    Private Function CreateModuleForm(info As ModuleInfo) As Form
        Select Case info.Key
            Case "users"
                Return New UsersForm()
            Case "suppliers"
                Return New SuppliersForm()
            Case "products"
                Return New ProductsForm()
            Case "delivery"
                Return New DeliveryForm()
            Case "inventory"
                Return New InventoryForm()
            Case "sales_report"
                Return New SalesReportForm()
            Case "inventory_report"
                Return New InventoryReportForm()
            Case "delivery_report"
                Return New DeliveryReportForm()
            Case "loss_report"
                Return New LossReportForm()
            Case "stock_threshold"
                Return New StockThresholdForm()
            Case "pos"
                Return New PosForm()
            Case "customers"
                Return New CustomersForm()
            Case "devices"
                Return New DevicesForm()
            Case "vat"
                Return New VatForm()
            Case "text_size"
                Return New TextSizeForm()
            Case "discounts"
                Return New DiscountsForm()
            Case "user_monitoring"
                Return New UserMonitoringForm()
            Case Else
                Return New PlaceholderForm(info.Title, info.Description)
        End Select
    End Function

    ' ------------------------------------------------------------------ events
    Private Sub OnHomeClick(sender As Object, e As EventArgs)
        ShowHome()
    End Sub

    Private Sub OnMenuItemClick(sender As Object, e As EventArgs)
        Dim item As ToolStripMenuItem = TryCast(sender, ToolStripMenuItem)
        If item Is Nothing Then Return
        Dim info As ModuleInfo = TryCast(item.Tag, ModuleInfo)
        If info IsNot Nothing Then OpenModule(info)
    End Sub

    Private Sub OnLogoutClick(sender As Object, e As EventArgs)
        If MessageBox.Show(Me, "Log out of the system?", "Log out",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            LoggedOut = True
            Close()
        End If
    End Sub

    Private Sub OnExitClick(sender As Object, e As EventArgs)
        Close()
    End Sub

    Private Sub OnClockTick(sender As Object, e As EventArgs)
        _lblClock.Text = DateTime.Now.ToString("ddd, MMM d, yyyy  h:mm:ss tt") & "  "
    End Sub

    ' ------------------------------------------------------------------ live updates
    Private Sub SetServerLabel(online As Boolean)
        If online Then
            _lblServer.Text = "Database: " & Db.Settings.Server & "  |  " & Db.Settings.Database & "  "
        Else
            _lblServer.Text = "DATABASE OFFLINE - reconnecting...  "
        End If
    End Sub

    ''' <summary>Tables changed somewhere: refresh the open screen if it shows them.</summary>
    Private Sub OnLiveDataChanged(tables As List(Of String))
        If tables.Contains("*") OrElse tables.Contains("products") Then RefreshLowStock()
        If _content.Controls.Count = 0 Then Return
        Dim live As ILiveForm = TryCast(_content.Controls(0), ILiveForm)
        If live Is Nothing Then Return
        If LiveSync.Affects(live.WatchedTables, tables) Then live.RefreshData()
    End Sub

    Private Sub OnLiveConnectionChanged(connected As Boolean)
        SetServerLabel(connected)
    End Sub

    Private Sub OnLiveSecurityChanged(kind As LiveSecurityEvent, detail As String)
        Select Case kind
            Case LiveSecurityEvent.SuperadminMissing
                CloseOwnedForms()
                MessageBox.Show(Me,
                    "The Superadmin account no longer exists." & Environment.NewLine &
                    "The system will close and ask you to create a new Superadmin.",
                    "Superadmin not found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                RequireSetup = True
                Close()

            Case LiveSecurityEvent.AccountRemoved, LiveSecurityEvent.AccountDisabled, LiveSecurityEvent.AccountLocked
                CloseOwnedForms()
                Dim why As String = "was removed"
                If kind = LiveSecurityEvent.AccountDisabled Then why = "was disabled"
                If kind = LiveSecurityEvent.AccountLocked Then why = "was locked"
                MessageBox.Show(Me,
                    "Your account " & why & "." & Environment.NewLine & "You will be logged out.",
                    "Account changed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                LoggedOut = True
                Close()

            Case LiveSecurityEvent.RoleChanged
                AppSession.Role = detail
                MessageBox.Show(Me,
                    "Your role was changed to " & Permissions.RoleDisplay(detail) & "." & Environment.NewLine &
                    "Your menu has been updated.",
                    "Role changed", MessageBoxButtons.OK, MessageBoxIcon.Information)
                RebuildForRole()
        End Select
    End Sub

    Private Sub CloseOwnedForms()
        For Each f As Form In OwnedForms
            f.Close()
        Next
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        WindowStyle.ApplyMaroonTitleBar(Me)
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        _clock.Start()

        AddHandler _live.DataChanged, AddressOf OnLiveDataChanged
        AddHandler _live.SecurityChanged, AddressOf OnLiveSecurityChanged
        AddHandler _live.ConnectionChanged, AddressOf OnLiveConnectionChanged
        _live.Start()
        SmsService.StartQueueWorker()
        PerfLog.Mark("main window OnLoad done")
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        PerfLog.Mark("main window shown")
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
#If DEBUG Then
        ' Debug builds only: Ctrl+Shift+R cycles superadmin -> admin -> cashier to preview each role.
        If e.Control AndAlso e.Shift AndAlso e.KeyCode = Keys.R Then
            Select Case Permissions.NormalizeRole(AppSession.Role)
                Case "superadmin"
                    AppSession.Role = "admin"
                Case "admin"
                    AppSession.Role = "cashier"
                Case Else
                    AppSession.Role = "superadmin"
            End Select
            RebuildForRole()
            e.Handled = True
            Return
        End If
#End If
        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not LoggedOut AndAlso Not RequireSetup AndAlso e.CloseReason = CloseReason.UserClosing Then
            If MessageBox.Show(Me, "Do you want to exit the system?", "Confirm exit",
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then
                e.Cancel = True
                Return
            End If
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _live.Dispose()
            _clock.Stop()
            _clock.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
