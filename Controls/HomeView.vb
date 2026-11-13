Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Welcome screen: a tile for every module the logged-in role may open.</summary>
Public Class HomeView
    Inherits Panel

    Public Event ModuleRequested(info As ModuleInfo)

    Private ReadOnly _host As New FlowLayoutPanel()
    Private ReadOnly _groupFlows As New List(Of FlowLayoutPanel)()

    Public Sub New(fullName As String, role As String, modules As List(Of ModuleInfo))
        Dock = DockStyle.Fill
        BackColor = Color.White

        _host.Dock = DockStyle.Fill
        _host.FlowDirection = FlowDirection.TopDown
        _host.WrapContents = False
        _host.AutoScroll = True
        _host.Padding = New Padding(Theme.S(36), Theme.S(28), Theme.S(24), Theme.S(24))
        _host.BackColor = Color.White
        Controls.Add(_host)

        Dim hello As New Label()
        hello.AutoSize = True
        hello.Font = Theme.FontTitle
        hello.ForeColor = Theme.TextDark
        hello.Text = "Welcome back, " & fullName
        hello.Margin = New Padding(0, 0, 0, Theme.S(2))
        _host.Controls.Add(hello)

        Dim subtitle As New Label()
        subtitle.AutoSize = True
        subtitle.Font = Theme.FontBase
        subtitle.ForeColor = Theme.TextMuted
        subtitle.Text = Permissions.RoleDisplay(role) & "  |  " & DateTime.Now.ToString("dddd, MMMM d, yyyy")
        subtitle.Margin = New Padding(Theme.S(2), 0, 0, Theme.S(18))
        _host.Controls.Add(subtitle)

        If modules.Count = 0 Then
            Dim none As New Label()
            none.AutoSize = True
            none.Font = Theme.FontBase
            none.ForeColor = Theme.TextMuted
            none.Text = "There are no modules available for this account."
            _host.Controls.Add(none)
        End If

        For Each groupName As String In ModuleCatalog.GroupOrder
            Dim flow As FlowLayoutPanel = Nothing

            For Each m As ModuleInfo In modules
                If m.Group <> groupName Then Continue For

                If flow Is Nothing Then
                    Dim header As New Label()
                    header.AutoSize = True
                    header.Font = Theme.FontHeading
                    header.ForeColor = Theme.Maroon
                    header.Text = groupName
                    header.Margin = New Padding(0, Theme.S(10), 0, Theme.S(8))
                    _host.Controls.Add(header)

                    flow = New FlowLayoutPanel()
                    flow.AutoSize = True
                    flow.AutoSizeMode = AutoSizeMode.GrowAndShrink
                    flow.WrapContents = True
                    flow.FlowDirection = FlowDirection.LeftToRight
                    flow.BackColor = Color.White
                    flow.Margin = New Padding(0, 0, 0, Theme.S(4))
                    _host.Controls.Add(flow)
                    _groupFlows.Add(flow)
                End If

                Dim tile As New TileButton()
                tile.Title = m.Title
                tile.Subtitle = m.Description
                tile.Tag = m
                tile.Margin = New Padding(0, 0, Theme.S(16), Theme.S(16))
                AddHandler tile.Click, AddressOf OnTileClick
                flow.Controls.Add(tile)
            Next
        Next

        AddHandler _host.SizeChanged, AddressOf UpdateFlowWidths
        UpdateFlowWidths(Me, EventArgs.Empty)
    End Sub

    ' keep the tile rows wrapping to the window width, and stretch the tiles so a wide
    ' screen is filled without going overboard (max 4 per row, each at most 400 wide)
    Private Sub UpdateFlowWidths(sender As Object, e As EventArgs)
        Dim w As Integer = _host.ClientSize.Width - _host.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth
        w = Math.Max(300, w)

        Dim cols As Integer = Math.Max(1, Math.Min(4, w \ Theme.S(276)))
        Dim tileW As Integer = Math.Max(Theme.S(250), Math.Min(Theme.S(400), w \ cols - Theme.S(16)))

        For Each f As FlowLayoutPanel In _groupFlows
            f.SuspendLayout()
            f.MaximumSize = New Size(w, 0)
            For Each c As Control In f.Controls
                If TypeOf c Is TileButton Then c.Size = New Size(tileW, Theme.S(108))
            Next
            f.ResumeLayout()
        Next
    End Sub

    Private Sub OnTileClick(sender As Object, e As EventArgs)
        Dim t As TileButton = TryCast(sender, TileButton)
        If t Is Nothing Then Return
        Dim info As ModuleInfo = TryCast(t.Tag, ModuleInfo)
        If info IsNot Nothing Then RaiseEvent ModuleRequested(info)
    End Sub

End Class
