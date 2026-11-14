Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' Minimal bar chart for the reports. Vertical bars for trends (per hour / per day), horizontal bars for rankings
''' (top 10 products). One or two series. Hover a bar to see its exact value.
''' </summary>
Public Class BarChart
    Inherits Control

    Private Shared ReadOnly TitleFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly SmallFont As Font = Theme.MakeFont("Segoe UI", 8.5F)

    Private _chart As ReportChart
    Private _hover As Integer = -1
    Private _mouse As Point

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        BackColor = Color.White
        Size = New Size(Theme.S(360), Theme.S(220))
    End Sub

    Public Sub SetChart(c As ReportChart)
        _chart = c
        _hover = -1
        Invalidate()
    End Sub

    ' ------------------------------------------------------------------ geometry
    Private Function LabelWidth() As Integer
        Return Math.Min(CInt(Width * 0.36F), Theme.S(150))
    End Function

    Private Function PlotRect() As Rectangle
        If _chart IsNot Nothing AndAlso _chart.Horizontal Then
            Dim lw As Integer = LabelWidth()
            Return New Rectangle(lw + Theme.S(12), Theme.S(40), Math.Max(10, Width - lw - Theme.S(12) - Theme.S(60)), Math.Max(10, Height - Theme.S(40) - Theme.S(10)))
        End If
        Return New Rectangle(Theme.S(46), Theme.S(46), Math.Max(10, Width - Theme.S(46) - Theme.S(12)), Math.Max(10, Height - Theme.S(46) - Theme.S(34)))
    End Function

    Private Function MaxValue() As Decimal
        Dim m As Decimal = 0D
        If _chart Is Nothing Then Return m
        For Each s As ChartSeries In _chart.Series
            For Each v As Decimal In s.Values
                If v > m Then m = v
            Next
        Next
        Return m
    End Function

    Private Function ValueText(v As Decimal) As String
        Return If(_chart IsNot Nothing AndAlso _chart.IsMoney, ReportFormat.Peso(v), ReportFormat.Whole(v))
    End Function

    ' ------------------------------------------------------------------ painting
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Color.White)
        If _chart Is Nothing Then Return

        TextRenderer.DrawText(g, _chart.Title, TitleFont, New Rectangle(Theme.S(2), Theme.S(2), Width - Theme.S(4), Theme.S(24)), Theme.TextDark,
                              TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

        If Not _chart.HasData OrElse _chart.Labels.Count = 0 Then
            TextRenderer.DrawText(g, "No data for this period", Theme.FontBase, New Rectangle(0, 0, Width, Height), Theme.TextMuted,
                                  TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
            Return
        End If

        DrawLegend(g)

        Dim plot As Rectangle = PlotRect()
        Dim stepV As Decimal = ReportFormat.NiceStep(MaxValue())
        Dim topV As Decimal = stepV * 4D
        If _chart.Horizontal Then
            DrawHorizontal(g, plot, topV)
        Else
            DrawVertical(g, plot, stepV, topV)
        End If
        DrawTooltip(g)
    End Sub

    Private Sub DrawLegend(g As Graphics)
        If _chart.Series.Count < 2 Then Return
        Dim x As Integer = Width - Theme.S(4)
        For i As Integer = _chart.Series.Count - 1 To 0 Step -1
            Dim name As String = _chart.Series(i).Name
            Dim tw As Integer = TextRenderer.MeasureText(name, SmallFont).Width
            x -= tw
            TextRenderer.DrawText(g, name, SmallFont, New Point(x, Theme.S(6)), Theme.TextMuted, TextFormatFlags.NoPadding)
            x -= Theme.S(14)
            Using b As New SolidBrush(_chart.Series(i).Color)
                g.FillRectangle(b, x, Theme.S(9), Theme.S(9), Theme.S(9))
            End Using
            x -= Theme.S(10)
        Next
    End Sub

    Private Sub DrawVertical(g As Graphics, plot As Rectangle, stepV As Decimal, topV As Decimal)
        Dim n As Integer = _chart.Labels.Count
        Dim sc As Integer = _chart.Series.Count

        For i As Integer = 0 To 4
            Dim gy As Integer = plot.Bottom - CInt(plot.Height * i / 4.0R)
            Using p As New Pen(Color.FromArgb(235, 235, 235), 1.0F)
                g.DrawLine(p, plot.Left, gy, plot.Right, gy)
            End Using
            TextRenderer.DrawText(g, ReportFormat.AxisLabel(stepV * i), SmallFont, New Rectangle(0, gy - Theme.S(8), plot.Left - Theme.S(6), Theme.S(16)),
                                  Theme.TextMuted, TextFormatFlags.Right Or TextFormatFlags.NoPadding)
        Next

        Dim groupW As Single = plot.Width / CSng(n)
        Dim barW As Single = Math.Max(2.0F, groupW * 0.66F / sc)
        Dim every As Integer = Math.Max(1, CInt(Math.Ceiling(n / Math.Max(1.0R, plot.Width / (Theme.S(38) * 1.0R)))))

        For i As Integer = 0 To n - 1
            Dim gx As Single = plot.Left + i * groupW
            If i = _hover Then
                Using b As New SolidBrush(Theme.MaroonTint)
                    g.FillRectangle(b, gx, plot.Top, groupW, plot.Height)
                End Using
            End If
            Dim startX As Single = gx + (groupW - barW * sc) / 2.0F
            For s As Integer = 0 To sc - 1
                Dim v As Decimal = _chart.Series(s).Values(i)
                Dim bh As Single = CSng(If(topV > 0D, v / topV, 0D)) * plot.Height
                If bh > 0.5F Then
                    Using b As New SolidBrush(_chart.Series(s).Color)
                        g.FillRectangle(b, startX + s * barW, plot.Bottom - bh, barW, bh)
                    End Using
                    If n <= 12 AndAlso sc = 1 AndAlso v <> 0D Then
                        TextRenderer.DrawText(g, ReportFormat.AxisLabel(v), SmallFont,
                                              New Rectangle(CInt(gx), CInt(plot.Bottom - bh) - Theme.S(16), CInt(groupW), Theme.S(15)),
                                              Theme.TextMuted, TextFormatFlags.HorizontalCenter Or TextFormatFlags.NoPadding)
                    End If
                End If
            Next
            If i Mod every = 0 Then
                TextRenderer.DrawText(g, _chart.Labels(i), SmallFont,
                                      New Rectangle(CInt(gx) - Theme.S(6), plot.Bottom + Theme.S(5), CInt(groupW) + Theme.S(12), Theme.S(18)),
                                      Theme.TextMuted, TextFormatFlags.HorizontalCenter Or TextFormatFlags.NoPadding)
            End If
        Next
    End Sub

    Private Sub DrawHorizontal(g As Graphics, plot As Rectangle, topV As Decimal)
        Dim n As Integer = _chart.Labels.Count
        Dim rowH As Single = plot.Height / CSng(n)
        Dim lw As Integer = LabelWidth()
        Dim s0 As ChartSeries = _chart.Series(0)

        For i As Integer = 0 To n - 1
            Dim ry As Single = plot.Top + i * rowH
            If i = _hover Then
                Using b As New SolidBrush(Theme.MaroonTint)
                    g.FillRectangle(b, 0.0F, ry, CSng(Width), rowH)
                End Using
            End If
            TextRenderer.DrawText(g, _chart.Labels(i), SmallFont, New Rectangle(Theme.S(4), CInt(ry), lw, CInt(rowH)),
                                  Theme.TextDark, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)
            Dim v As Decimal = s0.Values(i)
            Dim bw As Single = CSng(If(topV > 0D, v / topV, 0D)) * plot.Width
            Dim bh As Single = Math.Min(rowH * 0.6F, Theme.S(16) * 1.0F)
            If bw > 0.5F Then
                Using b As New SolidBrush(s0.Color)
                    g.FillRectangle(b, plot.Left, ry + (rowH - bh) / 2.0F, bw, bh)
                End Using
            End If
            TextRenderer.DrawText(g, ReportFormat.AxisLabel(v), SmallFont,
                                  New Rectangle(CInt(plot.Left + bw) + Theme.S(4), CInt(ry), Theme.S(60), CInt(rowH)),
                                  Theme.TextMuted, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
        Next
    End Sub

    Private Sub DrawTooltip(g As Graphics)
        If _hover < 0 OrElse _chart Is Nothing OrElse _hover >= _chart.Labels.Count Then Return
        Dim lines As New List(Of String)()
        lines.Add(_chart.Labels(_hover))
        For Each s As ChartSeries In _chart.Series
            lines.Add(If(_chart.Series.Count > 1, s.Name & ": ", "") & ValueText(s.Values(_hover)))
        Next
        Dim text As String = String.Join(Environment.NewLine, lines)
        Dim sz As Size = TextRenderer.MeasureText(text, SmallFont)
        Dim box As New Rectangle(_mouse.X + Theme.S(14), _mouse.Y + Theme.S(14), sz.Width + Theme.S(16), sz.Height + Theme.S(12))
        If box.Right > Width - 2 Then box.X = _mouse.X - box.Width - Theme.S(10)
        If box.Bottom > Height - 2 Then box.Y = _mouse.Y - box.Height - Theme.S(10)
        If box.X < 0 Then box.X = 0
        If box.Y < 0 Then box.Y = 0
        Using path As GraphicsPath = Theme.RoundedRect(box, 8)
            Using b As New SolidBrush(Color.FromArgb(240, 40, 40, 40))
                g.FillPath(b, path)
            End Using
        End Using
        TextRenderer.DrawText(g, text, SmallFont, New Rectangle(box.X + Theme.S(8), box.Y + Theme.S(6), sz.Width + 2, sz.Height), Color.White,
                              TextFormatFlags.Left Or TextFormatFlags.NoPadding)
    End Sub

    ' ------------------------------------------------------------------ mouse
    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If _chart Is Nothing OrElse _chart.Labels.Count = 0 Then Return
        Dim plot As Rectangle = PlotRect()
        Dim idx As Integer = -1
        If _chart.Horizontal Then
            If e.Y >= plot.Top AndAlso e.Y < plot.Bottom Then idx = CInt((e.Y - plot.Top) / (plot.Height / CSng(_chart.Labels.Count)))
        Else
            If e.X >= plot.Left AndAlso e.X < plot.Right AndAlso e.Y >= plot.Top - Theme.S(10) Then idx = CInt((e.X - plot.Left) / (plot.Width / CSng(_chart.Labels.Count)))
        End If
        If idx >= _chart.Labels.Count Then idx = -1
        _mouse = e.Location
        If idx <> _hover OrElse idx >= 0 Then
            _hover = idx
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        If _hover <> -1 Then
            _hover = -1
            Invalidate()
        End If
    End Sub

End Class
