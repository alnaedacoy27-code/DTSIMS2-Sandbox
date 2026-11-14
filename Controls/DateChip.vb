Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' A compact date button for the report screens: a small calendar icon and the period text
''' ("Fri, Nov 13, 2026" / "Nov 9 - Nov 15, 2026" / "November 2026"). Click it to open a month calendar and pick any date.
''' </summary>
Public Class DateChip
    Inherits Control

    Public Event DateChosen(value As DateTime)

    Private Shared ReadOnly TextFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)

    Private _value As DateTime = DateTime.Today
    Private _label As String = ""
    Private _hover As Boolean

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        BackColor = Color.White
        Cursor = Cursors.Hand
        Height = Theme.S(40)
        Width = Theme.S(200)
    End Sub

    Public Sub SetValue(value As DateTime, label As String)
        _value = value.Date
        _label = label
        Width = Theme.S(52) + TextRenderer.MeasureText(label, TextFont).Width + Theme.S(18)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))

        Dim r As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(r, Height \ 2)
            Using b As New SolidBrush(If(_hover, Theme.MaroonTint, Color.White))
                g.FillPath(b, path)
            End Using
            Using pen As New Pen(If(_hover, Theme.Maroon, Theme.BorderColor), 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using

        DrawCalendarIcon(g, New Rectangle(Theme.S(16), (Height - Theme.S(18)) \ 2, Theme.S(18), Theme.S(18)))

        TextRenderer.DrawText(g, _label, TextFont, New Rectangle(Theme.S(44), 0, Width - Theme.S(44) - Theme.S(8), Height), Theme.TextDark,
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.EndEllipsis)
    End Sub

    ''' <summary>A small calendar: frame, top band, two binding rings and a few day dots.</summary>
    Private Sub DrawCalendarIcon(g As Graphics, r As Rectangle)
        Dim body As New Rectangle(r.X, r.Y + Theme.S(2), r.Width, r.Height - Theme.S(2))
        Using path As GraphicsPath = Theme.RoundedRect(body, Theme.S(3))
            Using pen As New Pen(Theme.Maroon, 1.5F)
                g.DrawPath(pen, path)
            End Using
        End Using
        Using b As New SolidBrush(Theme.Maroon)
            g.FillRectangle(b, body.X, body.Y, body.Width, Theme.S(5))
            Dim dot As Integer = Math.Max(2, Theme.S(2))
            For row As Integer = 0 To 1
                For col As Integer = 0 To 2
                    g.FillRectangle(b, body.X + Theme.S(3) + col * Theme.S(5), body.Y + Theme.S(8) + row * Theme.S(4), dot, dot)
                Next
            Next
        End Using
        Using pen As New Pen(Theme.Maroon, 1.5F)
            g.DrawLine(pen, r.X + Theme.S(5), r.Y, r.X + Theme.S(5), r.Y + Theme.S(4))
            g.DrawLine(pen, r.Right - Theme.S(5), r.Y, r.Right - Theme.S(5), r.Y + Theme.S(4))
        End Using
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        _hover = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hover = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnClick(e As EventArgs)
        MyBase.OnClick(e)
        ShowCalendar()
    End Sub

    Private Sub ShowCalendar()
        Dim cal As New MonthCalendar()
        cal.Font = Theme.FontBase
        cal.MaxSelectionCount = 1
        cal.ShowToday = True
        cal.ShowTodayCircle = True
        cal.SetDate(_value)

        Dim host As New ToolStripControlHost(cal)
        host.AutoSize = False
        host.Size = cal.Size
        host.Margin = Padding.Empty
        host.Padding = Padding.Empty

        Dim dd As New ToolStripDropDown()
        dd.AutoClose = True
        dd.DropShadowEnabled = True
        dd.Padding = New Padding(2)
        dd.Items.Add(host)

        AddHandler cal.DateSelected,
            Sub(s As Object, ev As DateRangeEventArgs)
                dd.Close()
                RaiseEvent DateChosen(ev.Start)
            End Sub
        AddHandler dd.Closed,
            Sub(s As Object, ev As ToolStripDropDownClosedEventArgs)
                BeginInvoke(New Action(Sub() dd.Dispose()))
            End Sub

        dd.Show(Me, New Point(0, Height + Theme.S(4)))
    End Sub

End Class
