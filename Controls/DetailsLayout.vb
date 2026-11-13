Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' The details panel on the right of a list screen: the information scrolls, and the action buttons
''' stay pinned in a bar at the bottom, so a button can never end up out of reach.
''' </summary>
Public Module DetailsLayout

    ''' <param name="host">The panel placed on the screen (Dock Right). Set its BackColor first.</param>
    ''' <param name="scroll">Receives the information rows (they scroll).</param>
    ''' <param name="bar">Receives the action buttons (always visible).</param>
    Public Sub Setup(host As Panel, scroll As FlowLayoutPanel, bar As FlowLayoutPanel, padding As Padding)
        scroll.Dock = DockStyle.Fill
        scroll.FlowDirection = FlowDirection.TopDown
        scroll.WrapContents = False
        scroll.AutoScroll = True
        ' extra room after the last row so nothing is cut off when scrolled to the end
        scroll.AutoScrollMargin = New Size(0, Theme.S(48))
        scroll.Padding = padding
        scroll.BackColor = host.BackColor

        bar.Dock = DockStyle.Bottom
        bar.FlowDirection = FlowDirection.TopDown
        bar.WrapContents = False
        bar.AutoSize = False
        bar.Height = Theme.S(120)
        bar.Padding = New Padding(padding.Left, Theme.S(14), padding.Right, Theme.S(16))
        bar.BackColor = host.BackColor
        AddHandler bar.Paint,
            Sub(sender As Object, e As PaintEventArgs)
                Using p As New Pen(Color.FromArgb(232, 226, 226), 1.0F)
                    e.Graphics.DrawLine(p, 0, 0, bar.Width, 0)
                End Using
            End Sub

        ' The bar's height is set here (not left to AutoSize): it follows the buttons that are showing,
        ' so the scrolling part above always ends exactly where the bar begins and nothing hides behind it.
        ' On a very short window the bar takes at most 60% of the height and scrolls itself.
        Dim busy As Boolean = False
        Dim fit As Action =
            Sub()
                If busy Then Return
                busy = True
                Try
                    Dim need As Integer = bar.GetPreferredSize(New Size(Math.Max(1, bar.Width), 0)).Height
                    Dim maxH As Integer = Math.Max(Theme.S(140), CInt(host.ClientSize.Height * 0.6))
                    bar.AutoScroll = (need > maxH)
                    Dim h As Integer = Math.Min(need, maxH)
                    If h > 0 AndAlso bar.Height <> h Then bar.Height = h
                Finally
                    busy = False
                End Try
            End Sub
        AddHandler bar.Layout, Sub(sender As Object, e As LayoutEventArgs) fit()
        AddHandler host.Resize, Sub(sender As Object, e As EventArgs) fit()
        AddHandler host.VisibleChanged, Sub(sender As Object, e As EventArgs) fit()
        AddHandler bar.ControlAdded, Sub(sender As Object, e As ControlEventArgs) fit()

        ' the first control added fills what is left; the bar takes the bottom
        host.Controls.Add(scroll)
        host.Controls.Add(bar)
    End Sub

End Module
