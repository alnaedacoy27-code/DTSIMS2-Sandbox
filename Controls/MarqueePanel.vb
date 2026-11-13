Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Red scrolling warning line (low-stock alerts). Hidden when there is nothing to say.
''' Inventory will call SetMessages(...) with the products that reached their minimum stock.
''' </summary>
Public Class MarqueePanel
    Inherits Panel

    Private Shared ReadOnly BadgeWidth As Integer = Theme.S(112)

    Private ReadOnly _timer As New Timer()
    Private ReadOnly _textFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.5F)
    Private ReadOnly _badgeFont As Font = Theme.MakeFont("Segoe UI Semibold", 9.0F)
    Private _text As String = ""
    Private _textWidth As Single
    Private _offset As Single

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Height = Theme.S(32)
        BackColor = Theme.AlertBack
        Visible = False
        _timer.Interval = 30
        AddHandler _timer.Tick, AddressOf OnTick
    End Sub

    Public Sub SetMessages(messages As IEnumerable(Of String))
        Dim parts As New List(Of String)()
        If messages IsNot Nothing Then
            For Each m As String In messages
                If Not String.IsNullOrWhiteSpace(m) Then parts.Add(m.Trim())
            Next
        End If

        If parts.Count = 0 Then
            _timer.Stop()
            _text = ""
            Visible = False
            Return
        End If

        _text = String.Join("        *        ", parts.ToArray())
        Using g As Graphics = CreateGraphics()
            _textWidth = g.MeasureString(_text, _textFont).Width
        End Using
        _offset = CSng(Width)
        Visible = True
        _timer.Start()
    End Sub

    Private Sub OnTick(sender As Object, e As EventArgs)
        _offset -= 2.0F
        If _offset + _textWidth < BadgeWidth Then _offset = CSng(Width)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.Clear(BackColor)

        ' scrolling text (clipped so it slides in from behind the badge)
        g.SetClip(New Rectangle(BadgeWidth, 0, Math.Max(0, Width - BadgeWidth), Height))
        Using br As New SolidBrush(Theme.AlertRed)
            Dim y As Single = (Height - _textFont.Height) / 2.0F
            g.DrawString(_text, _textFont, br, _offset, y)
        End Using
        g.ResetClip()

        ' badge
        Using br As New SolidBrush(Theme.AlertRed)
            g.FillRectangle(br, 0, 0, BadgeWidth, Height)
        End Using
        TextRenderer.DrawText(g, "LOW STOCK", _badgeFont, New Rectangle(0, 0, BadgeWidth, Height), Color.White,
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _timer.Stop()
            _timer.Dispose()
            _textFont.Dispose()
            _badgeFont.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
