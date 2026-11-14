Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>A row of pill buttons where one is selected (Daily | Weekly | Monthly, or report views).</summary>
Public Class PillTabs
    Inherits Control

    Public Event SelectedIndexChanged As EventHandler

    Private Shared ReadOnly TabFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)

    Private ReadOnly _items As New List(Of String)()
    Private ReadOnly _widths As New List(Of Integer)()
    Private _selected As Integer
    Private _hover As Integer = -1

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        BackColor = Color.White
        Cursor = Cursors.Hand
        Height = Theme.S(40)
    End Sub

    Public Sub SetItems(ParamArray items As String())
        _items.Clear()
        _widths.Clear()
        Dim total As Integer = Theme.S(8)
        For Each s As String In items
            _items.Add(s)
            Dim w As Integer = TextRenderer.MeasureText(s, TabFont).Width + Theme.S(30)
            _widths.Add(w)
            total += w
        Next
        Width = total + Theme.S(4)
        If _selected >= _items.Count Then _selected = 0
        Invalidate()
    End Sub

    Public Property SelectedIndex As Integer
        Get
            Return _selected
        End Get
        Set(value As Integer)
            If value < 0 OrElse value >= _items.Count OrElse value = _selected Then Return
            _selected = value
            Invalidate()
            RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Private Function SegmentAt(x As Integer) As Integer
        Dim left As Integer = Theme.S(4)
        For i As Integer = 0 To _items.Count - 1
            If x >= left AndAlso x < left + _widths(i) Then Return i
            left += _widths(i)
        Next
        Return -1
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))

        Dim outer As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(outer, Height \ 2)
            Using pen As New Pen(Theme.BorderColor, 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using

        Dim left As Integer = Theme.S(4)
        For i As Integer = 0 To _items.Count - 1
            Dim seg As New Rectangle(left, Theme.S(4), _widths(i), Height - Theme.S(8) - 1)
            If i = _selected Then
                Using path As GraphicsPath = Theme.RoundedRect(seg, seg.Height \ 2)
                    Using b As New SolidBrush(Theme.Maroon)
                        g.FillPath(b, path)
                    End Using
                End Using
            ElseIf i = _hover Then
                Using path As GraphicsPath = Theme.RoundedRect(seg, seg.Height \ 2)
                    Using b As New SolidBrush(Theme.MaroonTint)
                        g.FillPath(b, path)
                    End Using
                End Using
            End If
            TextRenderer.DrawText(g, _items(i), TabFont, seg, If(i = _selected, Color.White, Theme.TextDark),
                                  TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
            left += _widths(i)
        Next
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim h As Integer = SegmentAt(e.X)
        If h <> _hover Then
            _hover = h
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hover = -1
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        MyBase.OnMouseClick(e)
        Dim i As Integer = SegmentAt(e.X)
        If i >= 0 Then SelectedIndex = i
    End Sub

End Class
