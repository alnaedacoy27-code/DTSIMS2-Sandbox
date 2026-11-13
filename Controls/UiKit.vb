Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reflection
Imports System.Windows.Forms

' =====================================================================================
' UiKit: small reusable pieces shared by every module screen
'   Pills, PillLabel, Avatar, StatCard, SearchBox, GridStyle
' =====================================================================================

''' <summary>Rounded "pill" labels (role, status ...) and their colors.</summary>
Public Module Pills

    Private ReadOnly PillFont As Font = Theme.MakeFont("Segoe UI Semibold", 8.5F)

    Public Function Measure(text As String) As Integer
        Return TextRenderer.MeasureText(text, PillFont, New Size(2000, 100), TextFormatFlags.NoPadding).Width + 24
    End Function

    ''' <summary>Draws a pill, left-aligned and vertically centered inside a grid cell.</summary>
    Public Sub Draw(g As Graphics, cell As Rectangle, text As String, back As Color, fore As Color)
        Dim r As New Rectangle(cell.X + Theme.S(10), cell.Y + (cell.Height - Theme.S(24)) \ 2, Measure(text), Theme.S(24))
        PaintPill(g, r, text, back, fore)
    End Sub

    Public Sub PaintPill(g As Graphics, r As Rectangle, text As String, back As Color, fore As Color)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using path As GraphicsPath = Theme.RoundedRect(r, r.Height \ 2)
            Using b As New SolidBrush(back)
                g.FillPath(b, path)
            End Using
        End Using
        TextRenderer.DrawText(g, text, PillFont, r, fore,
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
    End Sub

    Public Function RoleBack(role As String) As Color
        Select Case Permissions.NormalizeRole(role)
            Case "superadmin"
                Return Color.FromArgb(249, 228, 228)
            Case "admin"
                Return Color.FromArgb(228, 238, 252)
            Case Else
                Return Color.FromArgb(238, 238, 238)
        End Select
    End Function

    Public Function RoleFore(role As String) As Color
        Select Case Permissions.NormalizeRole(role)
            Case "superadmin"
                Return Theme.Maroon
            Case "admin"
                Return Color.FromArgb(28, 88, 178)
            Case Else
                Return Color.FromArgb(90, 90, 90)
        End Select
    End Function

    Public Function StatusBack(status As String) As Color
        Select Case status
            Case "Active"
                Return Color.FromArgb(226, 246, 234)
            Case "Locked"
                Return Color.FromArgb(253, 230, 230)
            Case "Disabled"
                Return Color.FromArgb(232, 232, 232)
            Case "Inactive"
                Return Color.FromArgb(235, 239, 245)
            Case Else
                Return Color.FromArgb(255, 243, 219)
        End Select
    End Function

    Public Function StatusFore(status As String) As Color
        Select Case status
            Case "Active"
                Return Color.FromArgb(22, 130, 60)
            Case "Locked"
                Return Theme.AlertRed
            Case "Disabled"
                Return Color.FromArgb(100, 100, 100)
            Case "Inactive"
                Return Color.FromArgb(95, 110, 130)
            Case Else
                Return Color.FromArgb(160, 100, 10)
        End Select
    End Function

End Module

''' <summary>One pill as a control (used in detail panels).</summary>
Public Class PillLabel
    Inherits Control

    Private _text As String = ""
    Private _back As Color = Color.Gainsboro
    Private _fore As Color = Color.DimGray

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(Theme.S(80), Theme.S(24))
    End Sub

    Public Sub SetPill(text As String, back As Color, fore As Color)
        _text = text
        _back = back
        _fore = fore
        Size = New Size(Pills.Measure(text), Theme.S(24))
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))
        Pills.PaintPill(e.Graphics, New Rectangle(0, 0, Width - 1, Height - 1), _text, _back, _fore)
    End Sub
End Class

''' <summary>Round photo, or maroon initials when there is no photo.</summary>
Public Class Avatar
    Inherits Control

    Private _photo As Image
    Private _name As String = ""

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(Theme.S(96), Theme.S(96))
    End Sub

    Public Property DisplayName As String
        Get
            Return _name
        End Get
        Set(v As String)
            _name = If(v, "")
            Invalidate()
        End Set
    End Property

    ''' <summary>The avatar owns the image and disposes the previous one when it changes.</summary>
    Public Property Photo As Image
        Get
            Return _photo
        End Get
        Set(v As Image)
            If _photo IsNot Nothing AndAlso Not Object.ReferenceEquals(_photo, v) Then _photo.Dispose()
            _photo = v
            Invalidate()
        End Set
    End Property

    Public Shared Function Initials(name As String) As String
        Dim parts As String() = If(name, "").Trim().Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length = 0 Then Return "?"
        If parts.Length = 1 Then Return parts(0).Substring(0, 1).ToUpperInvariant()
        Return (parts(0).Substring(0, 1) & parts(parts.Length - 1).Substring(0, 1)).ToUpperInvariant()
    End Function

    ''' <summary>Used by grids too: a maroon-tinted circle with initials.</summary>
    Public Shared Sub PaintInitials(g As Graphics, bounds As Rectangle, name As String, fontSize As Single)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim r As New Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1)
        Using b As New SolidBrush(Theme.MaroonTint)
            g.FillEllipse(b, r)
        End Using
        Using p As New Pen(Color.FromArgb(240, 214, 214), 1.0F)
            g.DrawEllipse(p, r)
        End Using
        Using f As New Font("Segoe UI Semibold", fontSize)
            TextRenderer.DrawText(g, Initials(name), f, bounds, Theme.Maroon,
                                  TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
        End Using
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))
        g.SmoothingMode = SmoothingMode.AntiAlias

        If _photo Is Nothing Then
            PaintInitials(g, New Rectangle(0, 0, Width, Height), _name, Height * 0.26F)
            Return
        End If

        Dim r As New Rectangle(0, 0, Width - 1, Height - 1)
        Dim side As Integer = Math.Min(_photo.Width, _photo.Height)
        Dim src As New Rectangle((_photo.Width - side) \ 2, (_photo.Height - side) \ 2, side, side)
        Using path As New GraphicsPath()
            path.AddEllipse(r)
            g.SetClip(path)
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.DrawImage(_photo, r, src, GraphicsUnit.Pixel)
            g.ResetClip()
        End Using
        Using p As New Pen(Color.FromArgb(235, 220, 220), 1.0F)
            g.DrawEllipse(p, r)
        End Using
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso _photo IsNot Nothing Then
            _photo.Dispose()
            _photo = Nothing
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

''' <summary>A small number-and-label card, e.g. "Admin 2 / 5".</summary>
Public Class StatCard
    Inherits Control

    Private Shared ReadOnly ValueFont As Font = Theme.MakeFont("Segoe UI Semibold", 16.0F)

    Private _value As String = "0"
    Private _caption As String = ""
    Private _warn As Boolean

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(Theme.S(200), Theme.S(72))
    End Sub

    Public Property Value As String
        Get
            Return _value
        End Get
        Set(v As String)
            _value = v
            Invalidate()
        End Set
    End Property

    Public Property Caption As String
        Get
            Return _caption
        End Get
        Set(v As String)
            _caption = v
            Invalidate()
        End Set
    End Property

    ''' <summary>Shows the number in red (limit reached, locked accounts ...).</summary>
    Public Property Warn As Boolean
        Get
            Return _warn
        End Get
        Set(v As Boolean)
            _warn = v
            Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))

        Dim r As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(r, 10)
            Using b As New SolidBrush(Color.White)
                g.FillPath(b, path)
            End Using
            Using p As New Pen(Theme.BorderColor, 1.0F)
                g.DrawPath(p, path)
            End Using
        End Using

        TextRenderer.DrawText(g, _value, ValueFont, New Rectangle(16, 8, Width - 24, 36),
                              If(_warn, Theme.AlertRed, Theme.Maroon),
                              TextFormatFlags.Left Or TextFormatFlags.NoPadding)
        TextRenderer.DrawText(g, _caption, Theme.FontSmall, New Rectangle(16, 44, Width - 24, 22),
                              Theme.TextMuted, TextFormatFlags.Left Or TextFormatFlags.NoPadding)
    End Sub
End Class

''' <summary>Rounded search box with a magnifier and placeholder text. Raises SearchChanged after typing pauses.</summary>
Public Class SearchBox
    Inherits Panel

    Private Declare Auto Function SendMessage Lib "user32.dll" (hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As String) As IntPtr
    Private Const EM_SETCUEBANNER As Integer = &H1501

    Public Event SearchChanged()

    Private ReadOnly _text As New TextBox()
    Private ReadOnly _timer As New Timer()
    Private ReadOnly _cue As String
    Private _focused As Boolean

    Public Sub New(placeholder As String)
        _cue = placeholder
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(Theme.S(280), Theme.S(40))
        BackColor = Color.White

        _text.BorderStyle = BorderStyle.None
        _text.Font = Theme.FontBase
        _text.BackColor = Color.White
        _text.ForeColor = Theme.TextDark
        Controls.Add(_text)
        LayoutText()

        AddHandler _text.HandleCreated, AddressOf OnTextHandleCreated
        AddHandler _text.TextChanged, AddressOf OnInnerTextChanged
        AddHandler _text.Enter, AddressOf OnTextEnter
        AddHandler _text.Leave, AddressOf OnTextLeave

        _timer.Interval = 300
        AddHandler _timer.Tick, AddressOf OnTimerTick
    End Sub

    ''' <summary>What the user typed (trimmed).</summary>
    Public ReadOnly Property Query As String
        Get
            Return _text.Text.Trim()
        End Get
    End Property

    Private Sub LayoutText()
        _text.Left = Theme.S(38)
        _text.Width = Math.Max(Theme.S(20), Width - Theme.S(38) - Theme.S(14))
        _text.Top = (Height - _text.Height) \ 2
    End Sub

    Protected Overrides Sub OnResize(eventargs As EventArgs)
        MyBase.OnResize(eventargs)
        LayoutText()
    End Sub

    Private Sub OnTextHandleCreated(sender As Object, e As EventArgs)
        SendMessage(_text.Handle, EM_SETCUEBANNER, New IntPtr(1), _cue)
    End Sub

    Private Sub OnInnerTextChanged(sender As Object, e As EventArgs)
        _timer.Stop()
        _timer.Start()
    End Sub

    Private Sub OnTimerTick(sender As Object, e As EventArgs)
        _timer.Stop()
        RaiseEvent SearchChanged()
    End Sub

    Private Sub OnTextEnter(sender As Object, e As EventArgs)
        _focused = True
        Invalidate()
    End Sub

    Private Sub OnTextLeave(sender As Object, e As EventArgs)
        _focused = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        _text.Focus()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))

        Dim r As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(r, 8)
            Using b As New SolidBrush(Color.White)
                g.FillPath(b, path)
            End Using
            Using p As New Pen(If(_focused, Theme.Maroon, Theme.BorderColor), If(_focused, 1.8F, 1.0F))
                g.DrawPath(p, path)
            End Using
        End Using

        Dim cy As Integer = Height \ 2
        Using p As New Pen(Theme.TextMuted, 1.8F)
            g.DrawEllipse(p, 13, cy - 8, 11, 11)
            g.DrawLine(p, Theme.S(22), cy + 1, Theme.S(27), cy + Theme.S(6))
        End Using
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _timer.Stop()
            _timer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

''' <summary>One modern, minimal look for every list in the system.</summary>
Public Module GridStyle

    Public Sub Apply(grid As DataGridView, Optional rowHeight As Integer = 60)
        grid.AutoGenerateColumns = False
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AllowUserToResizeRows = False
        grid.AllowUserToResizeColumns = False
        grid.AllowUserToOrderColumns = False
        grid.ReadOnly = True
        grid.MultiSelect = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.RowHeadersVisible = False
        grid.BorderStyle = BorderStyle.None
        grid.BackgroundColor = Color.White
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        grid.GridColor = Color.FromArgb(238, 238, 238)
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        grid.ColumnHeadersHeight = Theme.S(40)

        With grid.ColumnHeadersDefaultCellStyle
            .BackColor = Color.White
            .ForeColor = Theme.TextMuted
            .Font = Theme.FontCaption
            .SelectionBackColor = Color.White
            .SelectionForeColor = Theme.TextMuted
            .Alignment = DataGridViewContentAlignment.MiddleLeft
            .Padding = New Padding(Theme.S(10), 0, 0, 0)
        End With

        With grid.DefaultCellStyle
            .BackColor = Color.White
            .ForeColor = Theme.TextDark
            .Font = Theme.FontBase
            .SelectionBackColor = Theme.MaroonTint
            .SelectionForeColor = Theme.TextDark
            .Padding = New Padding(Theme.S(10), 0, 0, 0)
        End With

        grid.RowTemplate.Height = Theme.S(rowHeight)

        ' smoother scrolling
        Dim pi As PropertyInfo = GetType(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance Or BindingFlags.NonPublic)
        If pi IsNot Nothing Then pi.SetValue(grid, True, Nothing)
    End Sub

End Module
