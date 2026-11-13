Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>A clickable rounded card used on the home screen.</summary>
Public Class TileButton
    Inherits Control

    Private _hover As Boolean

    Public Property Title As String = ""
    Public Property Subtitle As String = ""

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        Size = New Size(Theme.S(280), Theme.S(108))
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

            g.SetClip(path)
            Using accent As New SolidBrush(Theme.Maroon)
                g.FillRectangle(accent, rect.X, rect.Y, Theme.S(6), rect.Height)
            End Using
            g.ResetClip()

            Using pen As New Pen(If(_hover, Theme.Maroon, Theme.BorderColor), 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using

        Dim titleRect As New Rectangle(Theme.S(22), Theme.S(16), Width - Theme.S(36), Theme.S(28))
        TextRenderer.DrawText(g, Title, Theme.FontTileTitle, titleRect, Theme.Maroon,
                              TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

        Dim subRect As New Rectangle(Theme.S(22), Theme.S(48), Width - Theme.S(36), Height - Theme.S(58))
        TextRenderer.DrawText(g, Subtitle, Theme.FontSmall, subRect, Theme.TextMuted,
                              TextFormatFlags.Left Or TextFormatFlags.Top Or TextFormatFlags.WordBreak Or TextFormatFlags.EndEllipsis)
    End Sub

End Class
