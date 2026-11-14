Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Text
Imports System.Globalization
Imports System.IO
Imports System.Text

' =====================================================================================
'  One layout, three outputs. ReportLayout draws a ReportDoc onto a RecordingCanvas
'  (a list of pages made of simple commands). The same pages are then replayed onto
'  the printer (GdiCanvas) or written as a PDF (PdfCanvas). All units are points.
' =====================================================================================

Public Module ReportFonts

    Private ReadOnly _bmp As New Bitmap(1, 1)
    Private ReadOnly _g As Graphics = MakeGraphics()
    Private ReadOnly _fonts As New Dictionary(Of String, Font)()
    Private ReadOnly _lock As New Object()

    Private Function MakeGraphics() As Graphics
        Dim g As Graphics = Graphics.FromImage(_bmp)
        g.PageUnit = GraphicsUnit.Point
        Return g
    End Function

    Public Function GetFont(size As Single, bold As Boolean) As Font
        Dim key As String = size.ToString(CultureInfo.InvariantCulture) & If(bold, "b", "r")
        Dim f As Font = Nothing
        If Not _fonts.TryGetValue(key, f) Then
            f = New Font("Arial", size, If(bold, FontStyle.Bold, FontStyle.Regular), GraphicsUnit.Point)
            _fonts(key) = f
        End If
        Return f
    End Function

    Public Function Measure(s As String, size As Single, bold As Boolean) As Single
        If String.IsNullOrEmpty(s) Then Return 0.0F
        SyncLock _lock
            Return _g.MeasureString(s, GetFont(size, bold), 100000, StringFormat.GenericTypographic).Width
        End SyncLock
    End Function

    ''' <summary>Shortens the text with "..." so it fits in maxWidth.</summary>
    Public Function Fit(s As String, size As Single, bold As Boolean, maxWidth As Single) As String
        If String.IsNullOrEmpty(s) Then Return ""
        If maxWidth <= 4.0F Then Return ""
        If Measure(s, size, bold) <= maxWidth Then Return s
        Dim t As String = s
        While t.Length > 1 AndAlso Measure(t & "...", size, bold) > maxWidth
            t = t.Substring(0, t.Length - 1)
        End While
        Return t.TrimEnd() & "..."
    End Function

End Module

''' <summary>Basic drawing operations. Text() fits and aligns, then calls DrawString().</summary>
Public MustInherit Class CanvasBase

    Public MustOverride Sub DrawString(x As Single, y As Single, s As String, size As Single, bold As Boolean, color As Color)
    Public MustOverride Sub FillRect(x As Single, y As Single, w As Single, h As Single, color As Color)
    Public MustOverride Sub StrokeRect(x As Single, y As Single, w As Single, h As Single, color As Color, lineWidth As Single)
    Public MustOverride Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, color As Color, lineWidth As Single)

    ''' <summary>align: 0 = left, 1 = centre, 2 = right (inside the box x .. x + w).</summary>
    Public Sub Text(x As Single, y As Single, w As Single, s As String, size As Single, bold As Boolean, color As Color, Optional align As Integer = 0)
        If String.IsNullOrEmpty(s) Then Return
        Dim t As String = ReportFonts.Fit(s, size, bold, w)
        If t.Length = 0 Then Return
        Dim tw As Single = ReportFonts.Measure(t, size, bold)
        Dim dx As Single = 0.0F
        If align = 1 Then
            dx = (w - tw) / 2.0F
        ElseIf align = 2 Then
            dx = w - tw
        End If
        DrawString(x + dx, y, t, size, bold, color)
    End Sub

End Class

''' <summary>Stores every drawing command per page so the same pages can be printed or saved as PDF.</summary>
Public Class RecordingCanvas
    Inherits CanvasBase

    Private Class Cmd
        Public Kind As Integer          ' 0 text, 1 fill, 2 stroke, 3 line
        Public X As Single
        Public Y As Single
        Public W As Single
        Public H As Single
        Public S As String = ""
        Public Size As Single
        Public Bold As Boolean
        Public Color As Color
        Public Lw As Single
    End Class

    Private ReadOnly _pages As New List(Of List(Of Cmd))()
    Private _current As Integer = -1

    Public ReadOnly Property PageCount As Integer
        Get
            Return _pages.Count
        End Get
    End Property

    Public Sub BeginPage()
        _pages.Add(New List(Of Cmd)())
        _current = _pages.Count - 1
    End Sub

    Public Sub GoToPage(index As Integer)
        _current = index
    End Sub

    Private Sub Add(c As Cmd)
        If _current < 0 Then BeginPage()
        _pages(_current).Add(c)
    End Sub

    Public Overrides Sub DrawString(x As Single, y As Single, s As String, size As Single, bold As Boolean, color As Color)
        Dim c As New Cmd()
        c.Kind = 0 : c.X = x : c.Y = y : c.S = s : c.Size = size : c.Bold = bold : c.Color = color
        Add(c)
    End Sub

    Public Overrides Sub FillRect(x As Single, y As Single, w As Single, h As Single, color As Color)
        Dim c As New Cmd()
        c.Kind = 1 : c.X = x : c.Y = y : c.W = w : c.H = h : c.Color = color
        Add(c)
    End Sub

    Public Overrides Sub StrokeRect(x As Single, y As Single, w As Single, h As Single, color As Color, lineWidth As Single)
        Dim c As New Cmd()
        c.Kind = 2 : c.X = x : c.Y = y : c.W = w : c.H = h : c.Color = color : c.Lw = lineWidth
        Add(c)
    End Sub

    Public Overrides Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, color As Color, lineWidth As Single)
        Dim c As New Cmd()
        c.Kind = 3 : c.X = x1 : c.Y = y1 : c.W = x2 : c.H = y2 : c.Color = color : c.Lw = lineWidth
        Add(c)
    End Sub

    ''' <summary>Draws one recorded page onto another canvas (printer or PDF).</summary>
    Public Sub Replay(pageIndex As Integer, target As CanvasBase)
        If pageIndex < 0 OrElse pageIndex >= _pages.Count Then Return
        For Each c As Cmd In _pages(pageIndex)
            Select Case c.Kind
                Case 0
                    target.DrawString(c.X, c.Y, c.S, c.Size, c.Bold, c.Color)
                Case 1
                    target.FillRect(c.X, c.Y, c.W, c.H, c.Color)
                Case 2
                    target.StrokeRect(c.X, c.Y, c.W, c.H, c.Color, c.Lw)
                Case Else
                    target.DrawLine(c.X, c.Y, c.W, c.H, c.Color, c.Lw)
            End Select
        Next
    End Sub

End Class

''' <summary>Draws onto a GDI+ surface that uses points (the printer / print preview).</summary>
Public Class GdiCanvas
    Inherits CanvasBase

    Private ReadOnly _g As Graphics

    Public Sub New(g As Graphics)
        _g = g
        _g.PageUnit = GraphicsUnit.Point
        _g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit
        _g.SmoothingMode = Drawing2D.SmoothingMode.None
    End Sub

    Public Overrides Sub DrawString(x As Single, y As Single, s As String, size As Single, bold As Boolean, color As Color)
        Using b As New SolidBrush(color)
            _g.DrawString(s, ReportFonts.GetFont(size, bold), b, x, y, StringFormat.GenericTypographic)
        End Using
    End Sub

    Public Overrides Sub FillRect(x As Single, y As Single, w As Single, h As Single, color As Color)
        Using b As New SolidBrush(color)
            _g.FillRectangle(b, x, y, w, h)
        End Using
    End Sub

    Public Overrides Sub StrokeRect(x As Single, y As Single, w As Single, h As Single, color As Color, lineWidth As Single)
        Using p As New Pen(color, lineWidth)
            _g.DrawRectangle(p, x, y, w, h)
        End Using
    End Sub

    Public Overrides Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, color As Color, lineWidth As Single)
        Using p As New Pen(color, lineWidth)
            _g.DrawLine(p, x1, y1, x2, y2)
        End Using
    End Sub

End Class

''' <summary>Writes a real PDF (built-in Helvetica fonts, no extra library needed).</summary>
Public Class PdfCanvas
    Inherits CanvasBase

    Private Const Ascent As Single = 0.905F

    Private ReadOnly _w As Single
    Private ReadOnly _h As Single
    Private ReadOnly _pages As New List(Of StringBuilder)()
    Private _cur As StringBuilder
    Private Shared ReadOnly Inv As CultureInfo = CultureInfo.InvariantCulture

    Public Sub New(pageWidth As Single, pageHeight As Single)
        _w = pageWidth
        _h = pageHeight
    End Sub

    Public Sub BeginPage()
        _cur = New StringBuilder()
        _pages.Add(_cur)
    End Sub

    Private Shared Function N(v As Single) As String
        Return v.ToString("0.##", Inv)
    End Function

    Private Shared Function Rgb(c As Color) As String
        Return (c.R / 255.0F).ToString("0.###", Inv) & " " & (c.G / 255.0F).ToString("0.###", Inv) & " " & (c.B / 255.0F).ToString("0.###", Inv)
    End Function

    Private Shared Function Esc(s As String) As String
        Dim t As String = s.Replace(ChrW(&H20B1), "PHP ")
        t = t.Replace("\", "\\").Replace("(", "\(").Replace(")", "\)")
        Return t
    End Function

    Public Overrides Sub DrawString(x As Single, y As Single, s As String, size As Single, bold As Boolean, color As Color)
        If _cur Is Nothing Then BeginPage()
        _cur.Append(Rgb(color)).Append(" rg BT /").Append(If(bold, "F2", "F1")).Append(" ").Append(N(size)).Append(" Tf ")
        _cur.Append(N(x)).Append(" ").Append(N(_h - y - size * Ascent)).Append(" Td (").Append(Esc(s)).Append(") Tj ET" & vbLf)
    End Sub

    Public Overrides Sub FillRect(x As Single, y As Single, w As Single, h As Single, color As Color)
        If _cur Is Nothing Then BeginPage()
        _cur.Append(Rgb(color)).Append(" rg ").Append(N(x)).Append(" ").Append(N(_h - y - h)).Append(" ").Append(N(w)).Append(" ").Append(N(h)).Append(" re f" & vbLf)
    End Sub

    Public Overrides Sub StrokeRect(x As Single, y As Single, w As Single, h As Single, color As Color, lineWidth As Single)
        If _cur Is Nothing Then BeginPage()
        _cur.Append(Rgb(color)).Append(" RG ").Append(N(lineWidth)).Append(" w ").Append(N(x)).Append(" ").Append(N(_h - y - h)).Append(" ").Append(N(w)).Append(" ").Append(N(h)).Append(" re S" & vbLf)
    End Sub

    Public Overrides Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, color As Color, lineWidth As Single)
        If _cur Is Nothing Then BeginPage()
        _cur.Append(Rgb(color)).Append(" RG ").Append(N(lineWidth)).Append(" w ").Append(N(x1)).Append(" ").Append(N(_h - y1)).Append(" m ")
        _cur.Append(N(x2)).Append(" ").Append(N(_h - y2)).Append(" l S" & vbLf)
    End Sub

    Public Sub Save(path As String)
        Dim enc As Encoding = Encoding.GetEncoding(1252)
        Using fs As New FileStream(path, FileMode.Create, FileAccess.Write)
            Dim offsets As New List(Of Long)()
            Dim writeText As Action(Of String) = Sub(t As String)
                                                     Dim b As Byte() = enc.GetBytes(t)
                                                     fs.Write(b, 0, b.Length)
                                                 End Sub
            writeText("%PDF-1.4" & vbLf)

            ' objects: 1 catalog, 2 page tree, 3 and 4 fonts, then (page, content) pairs
            Dim pageCount As Integer = Math.Max(1, _pages.Count)
            Dim kids As New StringBuilder()
            For i As Integer = 0 To pageCount - 1
                kids.Append(5 + i * 2).Append(" 0 R ")
            Next

            offsets.Add(fs.Position) : writeText("1 0 obj" & vbLf & "<< /Type /Catalog /Pages 2 0 R >>" & vbLf & "endobj" & vbLf)
            offsets.Add(fs.Position) : writeText("2 0 obj" & vbLf & "<< /Type /Pages /Kids [" & kids.ToString() & "] /Count " & pageCount & " >>" & vbLf & "endobj" & vbLf)
            offsets.Add(fs.Position) : writeText("3 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" & vbLf & "endobj" & vbLf)
            offsets.Add(fs.Position) : writeText("4 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>" & vbLf & "endobj" & vbLf)

            For i As Integer = 0 To pageCount - 1
                Dim content As String = If(i < _pages.Count, _pages(i).ToString(), "")
                Dim bytes As Byte() = enc.GetBytes(content)
                Dim pageObj As Integer = 5 + i * 2
                offsets.Add(fs.Position)
                writeText(pageObj & " 0 obj" & vbLf & "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " & N(_w) & " " & N(_h) & "] " &
                          "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " & (pageObj + 1) & " 0 R >>" & vbLf & "endobj" & vbLf)
                offsets.Add(fs.Position)
                writeText((pageObj + 1) & " 0 obj" & vbLf & "<< /Length " & bytes.Length & " >>" & vbLf & "stream" & vbLf)
                fs.Write(bytes, 0, bytes.Length)
                writeText(vbLf & "endstream" & vbLf & "endobj" & vbLf)
            Next

            Dim xref As Long = fs.Position
            writeText("xref" & vbLf & "0 " & (offsets.Count + 1) & vbLf)
            writeText("0000000000 65535 f " & vbLf)
            For Each o As Long In offsets
                writeText(o.ToString("D10", Inv) & " 00000 n " & vbLf)
            Next
            writeText("trailer" & vbLf & "<< /Size " & (offsets.Count + 1) & " /Root 1 0 R >>" & vbLf & "startxref" & vbLf & xref & vbLf & "%%EOF" & vbLf)
        End Using
    End Sub

End Class

''' <summary>Lays a ReportDoc out on pages: title, KPI boxes, charts, then the table (repeating its header on every page).</summary>
Public Module ReportLayout

    Private Const Margin As Single = 36.0F
    Private ReadOnly Ink As Color = Color.FromArgb(33, 37, 41)
    Private ReadOnly Muted As Color = Color.FromArgb(100, 108, 116)
    Private ReadOnly Rule As Color = Color.FromArgb(215, 215, 215)
    Private ReadOnly Wine As Color = Color.FromArgb(128, 0, 0)
    Private ReadOnly Tint As Color = Color.FromArgb(250, 245, 245)
    Private ReadOnly Zebra As Color = Color.FromArgb(248, 248, 248)
    Private ReadOnly Pale As Color = Color.FromArgb(196, 160, 160)

    Public Function Build(doc As ReportDoc, pageW As Single, pageH As Single) As RecordingCanvas
        Dim c As New RecordingCanvas()
        c.BeginPage()
        Dim contentW As Single = pageW - Margin * 2.0F
        Dim bottom As Single = pageH - 50.0F
        Dim y As Single = Margin

        ' ---- title block
        c.Text(Margin, y, contentW, doc.ShopName, 9.0F, True, Wine)
        y += 14.0F
        c.Text(Margin, y, contentW, doc.Title, 20.0F, True, Ink)
        y += 28.0F
        c.Text(Margin, y, contentW, doc.PeriodLabel & If(doc.Note.Length > 0, "   |   " & doc.Note, ""), 10.0F, False, Muted)
        y += 18.0F
        c.DrawLine(Margin, y, pageW - Margin, y, Wine, 1.2F)
        y += 14.0F

        ' ---- KPI boxes
        If doc.Kpis.Count > 0 Then
            Dim n As Integer = doc.Kpis.Count
            Dim gap As Single = 8.0F
            Dim bw As Single = (contentW - gap * (n - 1)) / n
            For i As Integer = 0 To n - 1
                Dim x As Single = Margin + i * (bw + gap)
                Dim k As ReportKpi = doc.Kpis(i)
                c.FillRect(x, y, bw, 44.0F, Tint)
                c.StrokeRect(x, y, bw, 44.0F, Rule, 0.6F)
                c.Text(x + 8.0F, y + 7.0F, bw - 16.0F, k.Value.Replace(ReportFormat.PesoSign, "PHP "), 14.0F, True, If(k.Warn, Color.FromArgb(180, 30, 30), Wine))
                c.Text(x + 8.0F, y + 28.0F, bw - 16.0F, k.Caption, 8.0F, False, Muted)
            Next
            y += 44.0F + 14.0F
        End If

        ' ---- charts (side by side)
        Dim charts As New List(Of ReportChart)()
        For Each ch As ReportChart In doc.Charts
            If ch.HasData Then charts.Add(ch)
        Next
        If charts.Count > 0 Then
            Dim chartH As Single = 150.0F
            If y + chartH > bottom Then
                c.BeginPage()
                y = Margin
            End If
            Dim gap As Single = 14.0F
            Dim cw As Single = (contentW - gap * (charts.Count - 1)) / charts.Count
            For i As Integer = 0 To charts.Count - 1
                DrawChart(c, charts(i), Margin + i * (cw + gap), y, cw, chartH)
            Next
            y += chartH + 14.0F
        End If

        ' ---- table
        If doc.Columns.Count > 0 Then
            If doc.TableTitle.Length > 0 Then
                If y + 60.0F > bottom Then
                    c.BeginPage()
                    y = Margin
                End If
                c.Text(Margin, y, contentW, doc.TableTitle, 11.0F, True, Ink)
                y += 18.0F
            End If

            Dim totalWeight As Single = 0.0F
            For Each col As ReportColumn In doc.Columns
                totalWeight += col.Weight
            Next
            Dim xs As New List(Of Single)()
            Dim ws As New List(Of Single)()
            Dim cx As Single = Margin
            For Each col As ReportColumn In doc.Columns
                Dim w As Single = contentW * col.Weight / totalWeight
                xs.Add(cx)
                ws.Add(w)
                cx += w
            Next

            DrawTableHeader(c, doc, xs, ws, y, contentW)
            y += 20.0F

            Dim rowH As Single = 17.0F
            Dim rowIndex As Integer = 0
            If doc.Data Is Nothing OrElse doc.Data.Rows.Count = 0 Then
                c.Text(Margin, y + 8.0F, contentW, doc.EmptyText, 10.0F, False, Muted, 1)
            Else
                For Each r As DataRow In doc.Data.Rows
                    If y + rowH > bottom Then
                        c.BeginPage()
                        y = Margin
                        DrawTableHeader(c, doc, xs, ws, y, contentW)
                        y += 20.0F
                    End If
                    If rowIndex Mod 2 = 1 Then c.FillRect(Margin, y, contentW, rowH, Zebra)
                    For i As Integer = 0 To doc.Columns.Count - 1
                        Dim col As ReportColumn = doc.Columns(i)
                        Dim txt As String = ReportFormat.Cell(r(col.Field), col.Kind)
                        If col.RightAligned Then
                            c.Text(xs(i) + 4.0F, y + 4.0F, ws(i) - 10.0F, txt, 8.5F, False, Ink, 2)
                        Else
                            c.Text(xs(i) + 6.0F, y + 4.0F, ws(i) - 10.0F, txt, 8.5F, False, Ink, 0)
                        End If
                    Next
                    c.DrawLine(Margin, y + rowH, pageW - Margin, y + rowH, Color.FromArgb(235, 235, 235), 0.4F)
                    y += rowH
                    rowIndex += 1
                Next
            End If

            If doc.TruncatedNote.Length > 0 Then
                y += 6.0F
                If y + 12.0F <= bottom Then c.Text(Margin, y, contentW, doc.TruncatedNote, 8.0F, False, Muted)
            End If
        End If

        ' ---- footer on every page: who printed it, and "Page x of y"
        Dim total As Integer = c.PageCount
        For p As Integer = 0 To total - 1
            c.GoToPage(p)
            c.DrawLine(Margin, pageH - 38.0F, pageW - Margin, pageH - 38.0F, Rule, 0.6F)
            c.Text(Margin, pageH - 32.0F, contentW * 0.7F,
                   doc.ShopName & "  -  " & doc.Title & "  -  generated " & doc.GeneratedAt.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) &
                   If(doc.GeneratedBy.Length > 0, " by " & doc.GeneratedBy, ""), 8.0F, False, Muted)
            c.Text(pageW - Margin - 120.0F, pageH - 32.0F, 120.0F, "Page " & (p + 1) & " of " & total, 8.0F, False, Muted, 2)
        Next

        Return c
    End Function

    Private Sub DrawTableHeader(c As CanvasBase, doc As ReportDoc, xs As List(Of Single), ws As List(Of Single), y As Single, contentW As Single)
        c.FillRect(Margin, y, contentW, 20.0F, Color.FromArgb(243, 236, 236))
        For i As Integer = 0 To doc.Columns.Count - 1
            Dim col As ReportColumn = doc.Columns(i)
            If col.RightAligned Then
                c.Text(xs(i) + 4.0F, y + 6.0F, ws(i) - 10.0F, col.ExportHeader, 7.5F, True, Wine, 2)
            Else
                c.Text(xs(i) + 6.0F, y + 6.0F, ws(i) - 10.0F, col.ExportHeader, 7.5F, True, Wine, 0)
            End If
        Next
    End Sub

    ''' <summary>A bar chart (vertical, or horizontal for rankings) with a light grid.</summary>
    Private Sub DrawChart(c As CanvasBase, ch As ReportChart, x As Single, y As Single, w As Single, h As Single)
        c.StrokeRect(x, y, w, h, Rule, 0.6F)
        c.Text(x + 8.0F, y + 6.0F, w - 16.0F, ch.Title & If(ch.IsMoney, " (PHP)", ""), 9.0F, True, Ink)

        ' legend for two series
        If ch.Series.Count > 1 Then
            Dim lx As Single = x + w - 8.0F
            For i As Integer = ch.Series.Count - 1 To 0 Step -1
                Dim nm As String = ch.Series(i).Name
                Dim tw As Single = ReportFonts.Measure(nm, 7.5F, False)
                lx -= tw
                c.Text(lx, y + 8.0F, tw + 2.0F, nm, 7.5F, False, Muted)
                lx -= 10.0F
                c.FillRect(lx, y + 9.0F, 7.0F, 7.0F, ch.Series(i).Color)
                lx -= 8.0F
            Next
        End If

        Dim maxV As Decimal = 0D
        For Each s As ChartSeries In ch.Series
            For Each v As Decimal In s.Values
                If v > maxV Then maxV = v
            Next
        Next
        Dim stepV As Decimal = ReportFormat.NiceStep(maxV)
        Dim top As Decimal = stepV * 4D
        Dim n As Integer = ch.Labels.Count
        If n = 0 Then Return

        If ch.Horizontal Then
            Dim labelW As Single = Math.Min(w * 0.38F, 120.0F)
            Dim px As Single = x + 8.0F + labelW
            Dim pw As Single = w - 16.0F - labelW - 34.0F
            Dim py As Single = y + 26.0F
            Dim ph As Single = h - 32.0F
            Dim rowH As Single = ph / n
            For i As Integer = 0 To n - 1
                Dim ry As Single = py + i * rowH
                c.Text(x + 8.0F, ry + rowH / 2.0F - 5.0F, labelW - 6.0F, ch.Labels(i), 7.5F, False, Ink)
                Dim v As Decimal = ch.Series(0).Values(i)
                Dim bw As Single = CSng(If(top > 0D, v / top, 0D)) * pw
                Dim bh As Single = Math.Min(rowH * 0.62F, 12.0F)
                If bw > 0.5F Then c.FillRect(px, ry + (rowH - bh) / 2.0F, bw, bh, ch.Series(0).Color)
                c.Text(px + bw + 3.0F, ry + rowH / 2.0F - 5.0F, 60.0F, ReportFormat.AxisLabel(v), 7.0F, False, Muted)
            Next
        Else
            Dim px As Single = x + 36.0F
            Dim pw As Single = w - 36.0F - 10.0F
            Dim py As Single = y + 28.0F
            Dim ph As Single = h - 28.0F - 20.0F
            For g As Integer = 0 To 4
                Dim gy As Single = py + ph - ph * g / 4.0F
                c.DrawLine(px, gy, px + pw, gy, Color.FromArgb(232, 232, 232), 0.4F)
                c.Text(x + 2.0F, gy - 4.5F, 32.0F, ReportFormat.AxisLabel(stepV * g), 7.0F, False, Muted, 2)
            Next
            Dim groupW As Single = pw / n
            Dim sc As Integer = ch.Series.Count
            Dim barW As Single = Math.Max(1.5F, groupW * 0.66F / sc)
            Dim every As Integer = Math.Max(1, CInt(Math.Ceiling(n / Math.Max(1.0, pw / 26.0))))
            For i As Integer = 0 To n - 1
                Dim gx As Single = px + i * groupW + (groupW - barW * sc) / 2.0F
                For s As Integer = 0 To sc - 1
                    Dim v As Decimal = ch.Series(s).Values(i)
                    Dim bh As Single = CSng(If(top > 0D, v / top, 0D)) * ph
                    If bh > 0.3F Then c.FillRect(gx + s * barW, py + ph - bh, barW, bh, ch.Series(s).Color)
                Next
                If i Mod every = 0 Then c.Text(px + i * groupW - 4.0F, py + ph + 4.0F, groupW + 8.0F, ch.Labels(i), 7.0F, False, Muted, 1)
            Next
        End If
    End Sub

End Module
