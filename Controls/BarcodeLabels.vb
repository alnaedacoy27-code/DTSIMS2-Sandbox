Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.Drawing.Text
Imports System.Windows.Forms

''' <summary>
''' Barcode labels for the 58 mm receipt printer: item name, size, the bars and the number.
''' The same drawing code is used for the preview on the screen and for the printer.
''' </summary>
Public Module BarcodeLabels

    Private Const QuietModules As Integer = 10
    Private Const BarHeight As Single = 46.0F

    ''' <summary>Draws (draw = True) or only measures one label. Returns its height.</summary>
    Public Function RenderLabel(g As Graphics, name As String, size As String, code As String,
                                width As Single, draw As Boolean) As Single
        Dim y As Single = 4.0F
        g.TextRenderingHint = TextRenderingHint.AntiAlias

        Using bold As New Font("Segoe UI Semibold", 8.5F)
            Using small As New Font("Segoe UI", 7.5F)
                Using mono As New Font("Consolas", 8.0F)
                    Using fmt As New StringFormat()
                        fmt.Alignment = StringAlignment.Center

                        Dim h As Single = g.MeasureString(name, bold, CInt(width), fmt).Height
                        If draw Then g.DrawString(name, bold, Brushes.Black, New RectangleF(0, y, width, h), fmt)
                        y += h

                        If size.Length > 0 Then
                            Dim hs As Single = g.MeasureString(size, small, CInt(width), fmt).Height
                            If draw Then g.DrawString(size, small, Brushes.Black, New RectangleF(0, y, width, hs), fmt)
                            y += hs
                        End If
                        y += 3.0F

                        Dim bits As String = Code128.Encode(code)
                        If bits.Length > 0 Then
                            Dim mw As Single = width / (bits.Length + 2 * QuietModules)
                            If draw Then
                                Dim old As SmoothingMode = g.SmoothingMode
                                g.SmoothingMode = SmoothingMode.None
                                Dim x0 As Single = QuietModules * mw
                                Dim i As Integer = 0
                                While i < bits.Length
                                    If bits(i) = "1"c Then
                                        Dim j As Integer = i
                                        While j < bits.Length AndAlso bits(j) = "1"c
                                            j += 1
                                        End While
                                        g.FillRectangle(Brushes.Black, x0 + i * mw, y, (j - i) * mw, BarHeight)
                                        i = j
                                    Else
                                        i += 1
                                    End If
                                End While
                                g.SmoothingMode = old
                            End If
                        ElseIf draw Then
                            g.DrawString("(this code cannot be printed as bars)", small, Brushes.Black, New RectangleF(0, y + 10, width, 30), fmt)
                        End If
                        y += BarHeight + 2.0F

                        Dim hc As Single = g.MeasureString(code, mono, CInt(width), fmt).Height
                        If draw Then g.DrawString(code, mono, Brushes.Black, New RectangleF(0, y, width, hc), fmt)
                        y += hc + 4.0F

                        If draw Then
                            Using pen As New Pen(Color.FromArgb(150, 150, 150), 1.0F)
                                pen.DashStyle = DashStyle.Dot
                                g.DrawLine(pen, 0, y, width, y)
                            End Using
                        End If
                        y += 8.0F
                    End Using
                End Using
            End Using
        End Using
        Return y
    End Function

    ''' <summary>Prints the label several times on the default printer (a 58 mm roll). Throws if it cannot print.</summary>
    Public Sub PrintLabels(name As String, size As String, code As String, copies As Integer)
        If copies < 1 Then Return
        If Not Code128.CanEncode(code) Then Throw New InvalidOperationException("This barcode has characters that cannot be printed.")

        Dim labelH As Single
        Using bmp As New Bitmap(1, 1)
            Using g As Graphics = Graphics.FromImage(bmp)
                labelH = RenderLabel(g, name, size, code, ReceiptRenderer.PaperWidth, False)
            End Using
        End Using

        Dim perPage As Integer = Math.Max(1, CInt(Math.Floor(560.0F / labelH)))
        Dim printed As Integer = 0

        Using doc As New PrintDocument()
            doc.DocumentName = "Barcode " & code
            doc.PrintController = New StandardPrintController()
            LocalDevices.ApplyReceiptPrinter(doc)
            doc.OriginAtMargins = True
            doc.DefaultPageSettings.Margins = New Margins(24, 24, 6, 6)
            doc.DefaultPageSettings.PaperSize = New PaperSize("Label 58mm", 228, CInt(perPage * labelH) + 40)
            AddHandler doc.PrintPage,
                Sub(sender As Object, e As PrintPageEventArgs)
                    Dim y As Single = 0.0F
                    Dim onPage As Integer = 0
                    While onPage < perPage AndAlso printed < copies
                        Dim state As GraphicsState = e.Graphics.Save()
                        e.Graphics.TranslateTransform(0, y)
                        RenderLabel(e.Graphics, name, size, code, ReceiptRenderer.PaperWidth, True)
                        e.Graphics.Restore(state)
                        y += labelH
                        onPage += 1
                        printed += 1
                    End While
                    e.HasMorePages = (printed < copies)
                End Sub
            doc.Print()
        End Using
    End Sub

End Module

''' <summary>One label as it will print, shown on paper on the screen.</summary>
Public Class LabelPreview
    Inherits Panel

    Public Property ItemName As String = ""
    Public Property ItemSize As String = ""
    Public Property Code As String = ""

    Public Sub New()
        DoubleBuffered = True
        BackColor = Color.FromArgb(236, 233, 233)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        Dim scale As Single = Math.Max(0.8F, Math.Min(2.0F, (ClientSize.Width - 40) / ReceiptRenderer.PaperWidth))
        Dim h As Single = RenderLabel(g, ItemName, ItemSize, Code, ReceiptRenderer.PaperWidth, False)
        Dim x0 As Single = (ClientSize.Width - ReceiptRenderer.PaperWidth * scale) / 2.0F
        Dim y0 As Single = Math.Max(10.0F, (ClientSize.Height - h * scale) / 2.0F)
        g.TranslateTransform(x0, y0)
        g.ScaleTransform(scale, scale)
        g.FillRectangle(Brushes.White, 0, 0, ReceiptRenderer.PaperWidth, h)
        RenderLabel(g, ItemName, ItemSize, Code, ReceiptRenderer.PaperWidth, True)
    End Sub

End Class
