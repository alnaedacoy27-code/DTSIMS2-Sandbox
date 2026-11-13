Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Drawing.Printing
Imports System.Drawing.Text
Imports System.Globalization
Imports System.IO
Imports System.Windows.Forms

Public Class CardModel
    Public Property ShopName As String = "DARWIN TRADING"
    Public Property CustomerName As String = ""
    Public Property CardNumber As String = ""
End Class

''' <summary>
''' The loyalty card, drawn at the real size of a bank-style card (CR80: 85.6 x 54 mm).
''' The same drawing is used for the screen, a PDF / PNG file and a card printer.
''' </summary>
Public Module CardRenderer

    ' size in 1/100 inch (the unit a printer uses): 85.6 mm and 54 mm
    Public Const CardW As Single = 337.0F
    Public Const CardH As Single = 213.0F
    Public Const WidthMm As Double = 85.6
    Public Const HeightMm As Double = 54.0

    ' Fonts are sized in card units (1/100 inch), not points, so the text always keeps its size relative
    ' to the card whatever the resolution (screen 96 dpi, PDF / PNG 600 dpi, printer).
    Private Function UnitFont(family As String, sizeUnits As Single, style As FontStyle) As Font
        Return New Font(family, sizeUnits, style, GraphicsUnit.World)
    End Function

    Public Function Grouped(number As String) As String
        Dim sb As New System.Text.StringBuilder()
        For i As Integer = 0 To number.Length - 1
            If i > 0 AndAlso i Mod 4 = 0 Then sb.Append(" "c)
            sb.Append(number(i))
        Next
        Return sb.ToString()
    End Function

    Public Sub Draw(g As Graphics, m As CardModel)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = TextRenderingHint.AntiAlias

        ' background: maroon, edge to edge (a card printer trims it to the rounded card)
        Dim full As New RectangleF(0, 0, CardW, CardH)
        Using br As New LinearGradientBrush(full, Theme.Maroon, Theme.MaroonDark, 35.0F)
            g.FillRectangle(br, full)
        End Using
        Using soft As New SolidBrush(Color.FromArgb(26, 255, 255, 255))
            g.FillEllipse(soft, CardW - 170, -80, 230, 230)
            g.FillEllipse(soft, CardW - 100, 30, 170, 170)
        End Using

        Using fShop As Font = UnitFont("Segoe UI Semibold", 18.0F, FontStyle.Regular)
            g.DrawString(m.ShopName, fShop, Brushes.White, 18.0F, 13.0F)
        End Using
        Using fTag As Font = UnitFont("Segoe UI", 9.0F, FontStyle.Regular)
            Using tint As New SolidBrush(Color.FromArgb(235, 215, 215))
                g.DrawString("L O Y A L T Y     C A R D", fTag, tint, 19.0F, 36.0F)
            End Using
        End Using

        ' member name: shrinks a little when it is long
        Dim nameSize As Single = 21.0F
        Dim nameFont As Font = UnitFont("Segoe UI Semibold", nameSize, FontStyle.Regular)
        While nameSize > 12.5F AndAlso g.MeasureString(m.CustomerName, nameFont).Width > CardW - 40.0F
            nameFont.Dispose()
            nameSize -= 0.7F
            nameFont = UnitFont("Segoe UI Semibold", nameSize, FontStyle.Regular)
        End While
        Using fmt As New StringFormat()
            fmt.Trimming = StringTrimming.EllipsisCharacter
            fmt.FormatFlags = StringFormatFlags.NoWrap
            g.DrawString(m.CustomerName, nameFont, Brushes.White, New RectangleF(18.0F, 66.0F, CardW - 36.0F, 28.0F), fmt)
        End Using
        nameFont.Dispose()

        Using fCap As Font = UnitFont("Segoe UI", 8.3F, FontStyle.Regular)
            Using tint As New SolidBrush(Color.FromArgb(235, 215, 215))
                g.DrawString("MEMBER NO.", fCap, tint, 19.0F, 95.0F)
            End Using
        End Using
        Using fNum As Font = UnitFont("Consolas", 14.6F, FontStyle.Bold)
            g.DrawString(Grouped(m.CardNumber), fNum, Brushes.White, 18.0F, 105.0F)
        End Using

        ' white strip with the barcode (the white around the bars is the quiet zone scanners need)
        Dim strip As New RectangleF(16.0F, CardH - 84.0F, CardW - 32.0F, 68.0F)
        Using path As GraphicsPath = Rounded(strip, 7.0F)
            g.FillPath(Brushes.White, path)
        End Using

        Dim bits As String = Code128.Encode(m.CardNumber)
        If bits.Length > 0 Then
            ' scanners need about 10 bar-widths of plain white on each side of the bars (the "quiet zone")
            Dim mw As Single = strip.Width / (bits.Length + 20)
            Dim x0 As Single = strip.Left + 10.0F * mw
            Dim y0 As Single = strip.Top + 8.0F
            Dim old As SmoothingMode = g.SmoothingMode
            g.SmoothingMode = SmoothingMode.None
            Dim i As Integer = 0
            While i < bits.Length
                If bits(i) = "1"c Then
                    Dim j As Integer = i
                    While j < bits.Length AndAlso bits(j) = "1"c
                        j += 1
                    End While
                    g.FillRectangle(Brushes.Black, x0 + i * mw, y0, (j - i) * mw, 38.0F)
                    i = j
                Else
                    i += 1
                End If
            End While
            g.SmoothingMode = old
        End If
        Using fSmall As Font = UnitFont("Consolas", 10.4F, FontStyle.Regular)
            Using fmt As New StringFormat()
                fmt.Alignment = StringAlignment.Center
                g.DrawString(m.CardNumber, fSmall, Brushes.Black, New RectangleF(strip.Left, strip.Top + 49.0F, strip.Width, 14.0F), fmt)
            End Using
        End Using
    End Sub

    Private Function Rounded(r As RectangleF, radius As Single) As GraphicsPath
        Dim p As New GraphicsPath()
        Dim d As Single = radius * 2.0F
        p.AddArc(r.Left, r.Top, d, d, 180.0F, 90.0F)
        p.AddArc(r.Right - d, r.Top, d, d, 270.0F, 90.0F)
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0.0F, 90.0F)
        p.AddArc(r.Left, r.Bottom - d, d, d, 90.0F, 90.0F)
        p.CloseFigure()
        Return p
    End Function

    ''' <summary>The card as a picture at the real size, for example 600 dpi for a crisp file.</summary>
    Public Function RenderBitmap(m As CardModel, dpi As Integer) As Bitmap
        Dim w As Integer = CInt(Math.Round(WidthMm / 25.4 * dpi))
        Dim h As Integer = CInt(Math.Round(HeightMm / 25.4 * dpi))
        Dim bmp As New Bitmap(w, h)
        bmp.SetResolution(dpi, dpi)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.Clear(Color.White)
            g.ScaleTransform(CSng(w) / CardW, CSng(h) / CardH)
            Draw(g, m)
        End Using
        Return bmp
    End Function

    Private Function ToJpeg(bmp As Bitmap, quality As Long) As Byte()
        Dim codec As ImageCodecInfo = Nothing
        For Each c As ImageCodecInfo In ImageCodecInfo.GetImageEncoders()
            If c.MimeType = "image/jpeg" Then codec = c
        Next
        Using ms As New MemoryStream()
            If codec Is Nothing Then
                bmp.Save(ms, ImageFormat.Jpeg)
            Else
                Using ep As New EncoderParameters(1)
                    ep.Param(0) = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality)
                    bmp.Save(ms, codec, ep)
                End Using
            End If
            Return ms.ToArray()
        End Using
    End Function

    ''' <summary>Saves the card as a PDF page of exactly 85.6 x 54 mm (no printer needed).</summary>
    Public Sub SavePdf(m As CardModel, path As String)
        Using bmp As Bitmap = RenderBitmap(m, 600)
            Dim jpeg As Byte() = ToJpeg(bmp, 92L)
            File.WriteAllBytes(path, PdfImagePage.Build(jpeg, bmp.Width, bmp.Height, WidthMm, HeightMm))
        End Using
    End Sub

    Public Sub SavePng(m As CardModel, path As String)
        Using bmp As Bitmap = RenderBitmap(m, 600)
            bmp.Save(path, ImageFormat.Png)
        End Using
    End Sub

    ''' <summary>Sends the card to the default printer on card-size paper. Throws if there is no printer.</summary>
    Public Sub PrintCard(m As CardModel)
        Using doc As New PrintDocument()
            doc.DocumentName = "Loyalty card " & m.CardNumber
            doc.PrintController = New StandardPrintController()
            doc.DefaultPageSettings.Margins = New Margins(0, 0, 0, 0)
            doc.DefaultPageSettings.PaperSize = New PaperSize("CR80 card", 337, 213)
            AddHandler doc.PrintPage,
                Sub(sender As Object, e As PrintPageEventArgs)
                    Draw(e.Graphics, m)
                    e.HasMorePages = False
                End Sub
            doc.Print()
        End Using
    End Sub

End Module

''' <summary>A one-page PDF holding one picture, written by hand (no extra software needed).</summary>
Public Module PdfImagePage

    Private ReadOnly Lf As String = Convert.ToChar(10).ToString()

    Private Sub Put(ms As MemoryStream, text As String)
        Dim b As Byte() = System.Text.Encoding.ASCII.GetBytes(text)
        ms.Write(b, 0, b.Length)
    End Sub

    Public Function Build(jpeg As Byte(), pxW As Integer, pxH As Integer, pageWmm As Double, pageHmm As Double) As Byte()
        Dim inv As CultureInfo = CultureInfo.InvariantCulture
        Dim wPt As String = (pageWmm / 25.4 * 72.0).ToString("0.###", inv)
        Dim hPt As String = (pageHmm / 25.4 * 72.0).ToString("0.###", inv)
        Dim offsets As New List(Of Long)()

        Using ms As New MemoryStream()
            Put(ms, "%PDF-1.4" & Lf)
            ms.Write(New Byte() {37, 226, 227, 207, 211, 10}, 0, 6)

            offsets.Add(ms.Position)
            Put(ms, "1 0 obj" & Lf & "<< /Type /Catalog /Pages 2 0 R >>" & Lf & "endobj" & Lf)
            offsets.Add(ms.Position)
            Put(ms, "2 0 obj" & Lf & "<< /Type /Pages /Kids [3 0 R] /Count 1 >>" & Lf & "endobj" & Lf)
            offsets.Add(ms.Position)
            Put(ms, "3 0 obj" & Lf & "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " & wPt & " " & hPt & "] " &
                    "/Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>" & Lf & "endobj" & Lf)

            offsets.Add(ms.Position)
            Put(ms, "4 0 obj" & Lf & "<< /Type /XObject /Subtype /Image /Width " & pxW.ToString(inv) & " /Height " & pxH.ToString(inv) &
                    " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " & jpeg.Length.ToString(inv) & " >>" & Lf & "stream" & Lf)
            ms.Write(jpeg, 0, jpeg.Length)
            Put(ms, Lf & "endstream" & Lf & "endobj" & Lf)

            Dim content As String = "q " & wPt & " 0 0 " & hPt & " 0 0 cm /Im0 Do Q" & Lf
            offsets.Add(ms.Position)
            Put(ms, "5 0 obj" & Lf & "<< /Length " & content.Length.ToString(inv) & " >>" & Lf & "stream" & Lf & content & "endstream" & Lf & "endobj" & Lf)

            Dim xref As Long = ms.Position
            Put(ms, "xref" & Lf & "0 6" & Lf & "0000000000 65535 f " & Lf)
            For Each o As Long In offsets
                Put(ms, o.ToString("D10", inv) & " 00000 n " & Lf)
            Next
            Put(ms, "trailer" & Lf & "<< /Size 6 /Root 1 0 R >>" & Lf & "startxref" & Lf & xref.ToString(inv) & Lf & "%%EOF" & Lf)
            Return ms.ToArray()
        End Using
    End Function

End Module

''' <summary>The card on the screen, scaled to fit.</summary>
Public Class CardPreview
    Inherits Panel

    Private _model As CardModel

    Public Sub New()
        DoubleBuffered = True
        BackColor = Color.FromArgb(236, 233, 233)
    End Sub

    Public Property Model As CardModel
        Get
            Return _model
        End Get
        Set(v As CardModel)
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
        Dim scale As Single = Math.Min((ClientSize.Width - 24) / CardRenderer.CardW, (ClientSize.Height - 24) / CardRenderer.CardH)
        If scale <= 0.1F Then Return
        Dim w As Single = CardRenderer.CardW * scale
        Dim h As Single = CardRenderer.CardH * scale
        Dim x0 As Single = (ClientSize.Width - w) / 2.0F
        Dim y0 As Single = (ClientSize.Height - h) / 2.0F

        g.SmoothingMode = SmoothingMode.AntiAlias
        Using shadow As New SolidBrush(Color.FromArgb(40, 0, 0, 0))
            g.FillRectangle(shadow, x0 + 3, y0 + 4, w, h)
        End Using

        Dim state As GraphicsState = g.Save()
        g.TranslateTransform(x0, y0)
        g.SetClip(New RectangleF(0, 0, w, h))
        g.ScaleTransform(scale, scale)
        CardRenderer.Draw(g, _model)
        g.Restore(state)
    End Sub

End Class
