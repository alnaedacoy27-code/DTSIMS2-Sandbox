Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>One line of the cart.</summary>
Public Class CartItem
    Public Property ProductId As Integer
    Public Property Name As String = ""
    Public Property Size As String = ""
    Public Property Barcode As String = ""
    Public Property UnitPrice As Decimal
    Public Property CostPrice As Decimal
    Public Property Qty As Integer
    Public Property Stock As Integer

    Public ReadOnly Property Label As String
        Get
            Return Name & If(Size.Length > 0, "  " & Size, "")
        End Get
    End Property

    Public ReadOnly Property LineTotal As Decimal
        Get
            Return Qty * UnitPrice
        End Get
    End Property
End Class

''' <summary>Keys of the on-screen number pad ("0".."9", "00", ".", "BACK", "CLEAR") and the physical keyboard.</summary>
Public Module PadKeys

    ''' <summary>Maps a keyboard key to a pad key, or "" when it is not a number key.</summary>
    Public Function FromKeyboard(keyData As Keys) As String
        If (keyData And Keys.Modifiers) <> Keys.None Then Return ""
        Select Case keyData
            Case Keys.D0, Keys.NumPad0
                Return "0"
            Case Keys.D1, Keys.NumPad1
                Return "1"
            Case Keys.D2, Keys.NumPad2
                Return "2"
            Case Keys.D3, Keys.NumPad3
                Return "3"
            Case Keys.D4, Keys.NumPad4
                Return "4"
            Case Keys.D5, Keys.NumPad5
                Return "5"
            Case Keys.D6, Keys.NumPad6
                Return "6"
            Case Keys.D7, Keys.NumPad7
                Return "7"
            Case Keys.D8, Keys.NumPad8
                Return "8"
            Case Keys.D9, Keys.NumPad9
                Return "9"
            Case Keys.OemPeriod, Keys.Decimal
                Return "."
            Case Keys.Back
                Return "BACK"
            Case Keys.Delete
                Return "CLEAR"
            Case Else
                Return ""
        End Select
    End Function

End Module

''' <summary>The number being typed on the pad (digits, one dot, at most 2 decimals).</summary>
Public Class AmountBuffer

    Private _text As String = ""

    Public Property AllowDecimal As Boolean = True
    Public Property MaxIntegerDigits As Integer = 9

    Public ReadOnly Property Text As String
        Get
            Return _text
        End Get
    End Property

    Public ReadOnly Property Value As Decimal
        Get
            Dim v As Decimal
            If Decimal.TryParse(_text, Globalization.NumberStyles.AllowDecimalPoint, Globalization.CultureInfo.InvariantCulture, v) Then Return v
            Return 0D
        End Get
    End Property

    Public Sub SetValue(v As Decimal)
        _text = If(v = 0D, "", v.ToString("0.##", Globalization.CultureInfo.InvariantCulture))
    End Sub

    Public Sub Press(key As String)
        Select Case key
            Case "BACK"
                If _text.Length > 0 Then _text = _text.Substring(0, _text.Length - 1)
            Case "CLEAR"
                _text = ""
            Case "."
                If AllowDecimal AndAlso Not _text.Contains(".") Then _text = If(_text.Length = 0, "0.", _text & ".")
            Case "00"
                Press("0")
                Press("0")
            Case Else
                If key.Length = 1 AndAlso key(0) >= "0"c AndAlso key(0) <= "9"c Then AddDigit(key)
        End Select
    End Sub

    Private Sub AddDigit(d As String)
        Dim dot As Integer = _text.IndexOf("."c)
        If dot >= 0 Then
            If _text.Length - dot - 1 >= 2 Then Return
        Else
            If _text = "0" Then
                _text = d
                Return
            End If
            If _text.Length >= MaxIntegerDigits Then Return
        End If
        _text &= d
    End Sub

End Class

''' <summary>Big touch-friendly number pad.</summary>
Public Class NumpadPad
    Inherits Panel

    Public Event KeyPressed(key As String)

    Public Sub New()
        BackColor = Color.White
        Dim grid As New TableLayoutPanel()
        grid.Dock = DockStyle.Fill
        grid.ColumnCount = 3
        grid.RowCount = 5
        For c As Integer = 0 To 2
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))
        Next
        For r As Integer = 0 To 4
            grid.RowStyles.Add(New RowStyle(SizeType.Percent, 20.0F))
        Next

        Dim keys As String() = {"7", "8", "9", "4", "5", "6", "1", "2", "3", "00", "0", "."}
        For i As Integer = 0 To keys.Length - 1
            grid.Controls.Add(MakeButton(keys(i), keys(i), False), i Mod 3, i \ 3)
        Next
        Dim back As MaroonButton = MakeButton("Backspace", "BACK", True)
        Dim clear As MaroonButton = MakeButton("Clear", "CLEAR", True)
        grid.Controls.Add(back, 0, 4)
        grid.SetColumnSpan(back, 2)
        grid.Controls.Add(clear, 2, 4)
        Controls.Add(grid)
    End Sub

    Private Function MakeButton(text As String, key As String, isAction As Boolean) As MaroonButton
        Dim b As New MaroonButton()
        b.Text = text
        b.Tag = key
        b.Dock = DockStyle.Fill
        b.Margin = New Padding(Theme.S(4))
        b.Font = Theme.MakeFont("Segoe UI Semibold", If(isAction, 12.0F, 20.0F))
        b.MakeSecondary()
        AddHandler b.Click, AddressOf OnPadClick
        Return b
    End Function

    Private Sub OnPadClick(sender As Object, e As EventArgs)
        Dim b As Button = TryCast(sender, Button)
        If b Is Nothing Then Return
        RaiseEvent KeyPressed(b.Tag.ToString())
    End Sub

End Class

''' <summary>A product card on the POS: tap it to add one to the cart.</summary>
Public Class ProductTile
    Inherits Control

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 11.0F)
    Private Shared ReadOnly PriceFont As Font = Theme.MakeFont("Segoe UI Semibold", 15.0F)

    Private _hover As Boolean

    Public Property ProductId As Integer
    Public Property Title As String = ""
    Public Property SizeText As String = ""
    Public Property Category As String = ""
    Public Property Price As Decimal
    Public Property Stock As Integer

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(Theme.S(236), Theme.S(136))
        Cursor = Cursors.Hand
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _hover = True
        Invalidate()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _hover = False
        Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))

        Dim rect As New Rectangle(1, 1, Width - Theme.S(3), Height - Theme.S(3))
        Using path As GraphicsPath = Theme.RoundedRect(rect, 12)
            Using fill As New SolidBrush(If(_hover, Theme.MaroonTint, Color.White))
                g.FillPath(fill, path)
            End Using
            Using pen As New Pen(If(_hover, Theme.Maroon, Theme.BorderColor), If(_hover, 1.6F, 1.0F))
                g.DrawPath(pen, path)
            End Using
        End Using

        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
        TextRenderer.DrawText(g, Title, NameFont, New Rectangle(14, 10, Width - 28, 48), Theme.TextDark, flags Or TextFormatFlags.WordBreak)
        TextRenderer.DrawText(g, If(SizeText.Length > 0, SizeText, Category), Theme.FontSmall, New Rectangle(14, 60, Width - 28, 20), Theme.TextMuted, flags)
        TextRenderer.DrawText(g, Currency.Format(Price), PriceFont, New Rectangle(14, 98, Width - 28, 30), Theme.Maroon, flags)
        TextRenderer.DrawText(g, Stock.ToString() & " left", Theme.FontSmall, New Rectangle(14, 104, Width - 28, 20),
                              If(Stock <= 5, Theme.AlertRed, Theme.TextMuted),
                              TextFormatFlags.Right Or TextFormatFlags.NoPadding)
    End Sub

End Class

''' <summary>One line in the cart with - / + / quantity / remove buttons.</summary>
Public Class CartLine
    Inherits Panel

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 11.0F)
    Private Shared ReadOnly TotalFont As Font = Theme.MakeFont("Segoe UI Semibold", 12.0F)

    Public Event MinusClicked(item As CartItem)
    Public Event PlusClicked(item As CartItem)
    Public Event QtyClicked(item As CartItem)
    Public Event RemoveClicked(item As CartItem)

    Private ReadOnly _item As CartItem
    Private ReadOnly _minus As New MaroonButton()
    Private ReadOnly _qty As New MaroonButton()
    Private ReadOnly _plus As New MaroonButton()
    Private ReadOnly _remove As New MaroonButton()

    Public Sub New(item As CartItem)
        _item = item
        Height = Theme.S(96)
        BackColor = Color.White
        DoubleBuffered = True

        SetupSmall(_minus, "-")
        SetupSmall(_plus, "+")
        SetupSmall(_remove, "X")
        _remove.MakeDanger()
        _qty.Text = item.Qty.ToString()
        _qty.Height = Theme.S(42)
        _qty.Width = Theme.S(78)
        _qty.Font = Theme.MakeFont("Segoe UI Semibold", 13.0F)
        _qty.MakeSecondary()

        AddHandler _minus.Click, Sub(s, e) RaiseEvent MinusClicked(_item)
        AddHandler _plus.Click, Sub(s, e) RaiseEvent PlusClicked(_item)
        AddHandler _qty.Click, Sub(s, e) RaiseEvent QtyClicked(_item)
        AddHandler _remove.Click, Sub(s, e) RaiseEvent RemoveClicked(_item)

        Controls.Add(_minus)
        Controls.Add(_qty)
        Controls.Add(_plus)
        Controls.Add(_remove)
        PlaceButtons()
    End Sub

    Private Sub SetupSmall(b As MaroonButton, text As String)
        b.Text = text
        b.Size = New Size(Theme.S(48), Theme.S(42))
        b.Font = Theme.MakeFont("Segoe UI Semibold", 14.0F)
        b.MakeSecondary()
    End Sub

    Private Sub PlaceButtons()
        Dim y As Integer = Theme.S(46)
        _minus.Location = New Point(Theme.S(14), y)
        _qty.Location = New Point(Theme.S(14) + Theme.S(48) + Theme.S(6), y)
        _plus.Location = New Point(Theme.S(14) + Theme.S(48) + Theme.S(6) + Theme.S(78) + Theme.S(6), y)
        _remove.Location = New Point(Math.Max(Theme.S(220), Width - Theme.S(14) - _remove.Width), y)
    End Sub

    Protected Overrides Sub OnResize(eventargs As EventArgs)
        MyBase.OnResize(eventargs)
        PlaceButtons()
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        Dim flags As TextFormatFlags = TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding

        Dim totalText As String = Currency.Format(_item.LineTotal)
        Dim totalW As Integer = TextRenderer.MeasureText(totalText, TotalFont).Width
        TextRenderer.DrawText(g, _item.Label, NameFont, New Rectangle(14, 8, Width - totalW - 40, 22), Theme.TextDark, flags Or TextFormatFlags.Left)
        TextRenderer.DrawText(g, totalText, TotalFont, New Rectangle(Width - totalW - 16, 8, totalW + 4, 22), Theme.Maroon, flags Or TextFormatFlags.Right)
        TextRenderer.DrawText(g, Currency.Format(_item.UnitPrice) & " each   |   " & _item.Stock.ToString() & " in stock", Theme.FontSmall,
                              New Rectangle(Theme.S(14), Theme.S(30), Width - Theme.S(28), Theme.S(16)), Theme.TextMuted, flags Or TextFormatFlags.Left)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            g.DrawLine(pen, Theme.S(12), Height - 1, Width - Theme.S(12), Height - 1)
        End Using
    End Sub

End Class

''' <summary>Slides a panel in from the right edge of its container (and back out).</summary>
Public Class SlideAnimator

    Private ReadOnly _timer As New Timer()
    Private _panel As Control
    Private _host As Control
    Private _fromX As Integer
    Private _toX As Integer
    Private _started As DateTime
    Private _hideAtEnd As Boolean
    Private Const DurationMs As Double = 240.0

    Public Sub New()
        _timer.Interval = 15
        AddHandler _timer.Tick, AddressOf OnTick
    End Sub

    Public Sub SlideIn(panel As Control, host As Control)
        Start(panel, host, 0, False, True)
    End Sub

    Public Sub SlideOut(panel As Control, host As Control)
        Start(panel, host, host.ClientSize.Width, True, False)
    End Sub

    Private Sub Start(panel As Control, host As Control, toX As Integer, hideAtEnd As Boolean, bringFront As Boolean)
        _panel = panel
        _host = host
        If Not panel.Visible Then panel.Left = host.ClientSize.Width
        panel.Visible = True
        If bringFront Then panel.BringToFront()
        _fromX = panel.Left
        _toX = toX
        _hideAtEnd = hideAtEnd
        _started = DateTime.Now
        _timer.Start()
    End Sub

    Private Sub OnTick(sender As Object, e As EventArgs)
        Dim t As Double = Math.Min(1.0, (DateTime.Now - _started).TotalMilliseconds / DurationMs)
        Dim ease As Double = 1.0 - Math.Pow(1.0 - t, 3)
        _panel.Left = CInt(_fromX + (_toX - _fromX) * ease)
        If t >= 1.0 Then
            _timer.Stop()
            _panel.Left = _toX
            If _hideAtEnd Then _panel.Visible = False
        End If
    End Sub

    Public Sub Dispose()
        _timer.Stop()
        _timer.Dispose()
    End Sub

End Class

''' <summary>The receipt as it will print, shown on paper on the screen.</summary>
Public Class ReceiptPaper
    Inherits Panel

    Private _model As ReceiptModel

    Public Sub New()
        DoubleBuffered = True
        AutoScroll = True
        BackColor = Color.FromArgb(236, 233, 233)
    End Sub

    Public Property Model As ReceiptModel
        Get
            Return _model
        End Get
        Set(v As ReceiptModel)
            _model = v
            Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnResize(eventargs As EventArgs)
        MyBase.OnResize(eventargs)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        If _model Is Nothing Then Return
        Dim g As Graphics = e.Graphics

        Dim scale As Single = Math.Max(0.8F, Math.Min(2.2F, (ClientSize.Width - 48) / ReceiptRenderer.PaperWidth))
        Dim paperH As Single = ReceiptRenderer.Render(g, _model, ReceiptRenderer.PaperWidth, False) + 12
        AutoScrollMinSize = New Size(0, CInt(paperH * scale) + Theme.S(48))

        Dim x0 As Single = (ClientSize.Width - ReceiptRenderer.PaperWidth * scale) / 2.0F
        g.TranslateTransform(x0 + AutoScrollPosition.X, 24 + AutoScrollPosition.Y)
        g.ScaleTransform(scale, scale)
        g.FillRectangle(Brushes.White, 0, 0, ReceiptRenderer.PaperWidth, paperH)
        Using pen As New Pen(Color.FromArgb(215, 210, 210), 1.0F)
            g.DrawRectangle(pen, 0, 0, ReceiptRenderer.PaperWidth, paperH)
        End Using
        g.TranslateTransform(0, 6)
        ReceiptRenderer.Render(g, _model, ReceiptRenderer.PaperWidth, True)
    End Sub

End Class
