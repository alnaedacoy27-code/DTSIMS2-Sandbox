Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Windows.Forms

''' <summary>Save a ReportDoc as PDF or Excel, or print it. Nothing here needs an extra library.</summary>
Public Module ReportExport

    Private ReadOnly Inv As CultureInfo = CultureInfo.InvariantCulture

    ' ------------------------------------------------------------------ PDF
    Public Sub SavePdf(doc As ReportDoc, path As String)
        Const W As Single = 842.0F      ' A4 landscape, in points
        Const H As Single = 595.0F
        Dim pages As RecordingCanvas = ReportLayout.Build(doc, W, H)
        Dim pdf As New PdfCanvas(W, H)
        For i As Integer = 0 To pages.PageCount - 1
            pdf.BeginPage()
            pages.Replay(i, pdf)
        Next
        pdf.Save(path)
    End Sub

    ' ------------------------------------------------------------------ print
    ''' <summary>Full-size paper (A5 and up). A receipt roll is far smaller, so it can never hold a report. Sizes are in 1/100 inch.</summary>
    Private Function IsFullSize(ps As PaperSize) As Boolean
        Return Math.Min(ps.Width, ps.Height) >= 550 AndAlso Math.Max(ps.Width, ps.Height) >= 800
    End Function

    ''' <summary>A4 if the printer has it, otherwise Letter (short bond), otherwise Legal (long bond).</summary>
    Private Function FindPaper(settings As PrinterSettings) As PaperSize
        Dim letter As PaperSize = Nothing
        Dim legal As PaperSize = Nothing
        For Each ps As PaperSize In settings.PaperSizes
            If ps.Kind = PaperKind.A4 Then Return ps
            If ps.Kind = PaperKind.Letter AndAlso letter Is Nothing Then letter = ps
            If ps.Kind = PaperKind.Legal AndAlso legal Is Nothing Then legal = ps
        Next
        If letter IsNot Nothing Then Return letter
        Return legal
    End Function

    ''' <summary>A sensible first choice in the Print window: a normal printer, never the receipt printer.</summary>
    Private Function SuggestPrinter() As String
        Dim receipt As String = LocalDevices.ReceiptPrinter
        Dim virtualOne As String = ""
        For Each name As String In PrinterSettings.InstalledPrinters
            If receipt.Length > 0 AndAlso String.Equals(name, receipt, StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim ps As New PrinterSettings()
            ps.PrinterName = name
            If Not ps.IsValid OrElse FindPaper(ps) Is Nothing Then Continue For
            Dim lower As String = name.ToLowerInvariant()
            If lower.Contains("pdf") OrElse lower.Contains("onenote") OrElse lower.Contains("xps") OrElse lower.Contains("fax") Then
                If virtualOne.Length = 0 Then virtualOne = name
            Else
                Return name
            End If
        Next
        Return virtualOne
    End Function

    ''' <summary>
    ''' Asks which printer to use (never silently the receipt printer), then opens the print preview on full-size paper.
    ''' </summary>
    Public Sub PrintPreview(doc As ReportDoc, owner As IWin32Window)
        If PrinterSettings.InstalledPrinters.Count = 0 Then
            MessageBox.Show(owner, "No printer was found on this computer. You can still save the report as a PDF.", "Print",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' 1. choose the printer
        Dim chosen As PrinterSettings
        Using dlg As New PrintDialog()
            dlg.PrinterSettings = New PrinterSettings()
            Dim suggested As String = SuggestPrinter()
            If suggested.Length > 0 Then dlg.PrinterSettings.PrinterName = suggested
            dlg.AllowSelection = False
            dlg.AllowSomePages = False
            dlg.AllowPrintToFile = False
            dlg.UseEXDialog = True
            If dlg.ShowDialog(owner) <> DialogResult.OK Then Return
            chosen = dlg.PrinterSettings
        End Using

        Using pd As New PrintDocument()
            pd.DocumentName = doc.Title
            pd.PrinterSettings = chosen
            pd.OriginAtMargins = False

            ' 2. full-size paper only (A4 / Letter / Legal), landscape
            Dim paper As PaperSize = pd.DefaultPageSettings.PaperSize
            If Not IsFullSize(paper) Then paper = FindPaper(chosen)
            If paper Is Nothing Then
                MessageBox.Show(owner, "The printer """ & chosen.PrinterName & """ only has small paper (like a receipt roll)." & vbCrLf & vbCrLf &
                                "Choose a printer that uses A4 or Letter paper, or save the report as a PDF.", "Print",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            pd.DefaultPageSettings.PaperSize = paper
            pd.DefaultPageSettings.Landscape = True
            pd.DefaultPageSettings.Margins = New Margins(0, 0, 0, 0)

            Dim wPt As Single = pd.DefaultPageSettings.Bounds.Width * 0.72F      ' 1/100 inch -> points
            Dim hPt As Single = pd.DefaultPageSettings.Bounds.Height * 0.72F
            Dim pages As RecordingCanvas = ReportLayout.Build(doc, wPt, hPt)
            Dim index As Integer = 0

            AddHandler pd.BeginPrint, Sub(s As Object, e As PrintEventArgs) index = 0
            AddHandler pd.PrintPage,
                Sub(s As Object, e As PrintPageEventArgs)
                    Dim canvas As New GdiCanvas(e.Graphics)
                    pages.Replay(index, canvas)
                    index += 1
                    e.HasMorePages = (index < pages.PageCount)
                End Sub

            ' 3. preview; its Print button sends the report to the chosen printer
            Using dlg As New PrintPreviewDialog()
                dlg.Document = pd
                dlg.UseAntiAlias = True
                dlg.Text = "Print preview - " & doc.Title & "  (" & chosen.PrinterName & ")"
                dlg.StartPosition = FormStartPosition.CenterParent
                dlg.Size = New Size(Theme.S(1100), Theme.S(760))
                dlg.ShowDialog(owner)
            End Using
        End Using
    End Sub

    ' ------------------------------------------------------------------ Excel (.xlsx)
    Private Function Clean(s As String) As String
        If s Is Nothing Then Return ""
        Dim sb As New StringBuilder(s.Length)
        For Each ch As Char In s
            If ch = ChrW(9) OrElse ch = ChrW(10) OrElse ch = ChrW(13) OrElse ch >= " "c Then sb.Append(ch)
        Next
        Return System.Security.SecurityElement.Escape(sb.ToString())
    End Function

    Private Function ColName(index As Integer) As String          ' 0 -> A, 26 -> AA
        Dim s As String = ""
        Dim n As Integer = index + 1
        While n > 0
            Dim m As Integer = (n - 1) Mod 26
            s = ChrW(AscW("A"c) + m) & s
            n = (n - m - 1) \ 26
        End While
        Return s
    End Function

    Private Function TextCell(col As Integer, row As Integer, text As String, style As Integer) As String
        Return "<c r=""" & ColName(col) & row & """ t=""inlineStr"" s=""" & style & """><is><t xml:space=""preserve"">" & Clean(text) & "</t></is></c>"
    End Function

    Private Function NumberCell(col As Integer, row As Integer, value As Decimal, style As Integer) As String
        Return "<c r=""" & ColName(col) & row & """ s=""" & style & """><v>" & value.ToString(Inv) & "</v></c>"
    End Function

    Public Sub SaveXlsx(doc As ReportDoc, path As String)
        Dim rows As New StringBuilder()
        Dim r As Integer = 1
        Dim widths As New List(Of Integer)()
        For i As Integer = 0 To Math.Max(doc.Columns.Count, 2) - 1
            widths.Add(12)
        Next

        ' title block
        rows.Append("<row r=""" & r & """>").Append(TextCell(0, r, doc.ShopName & "  -  " & doc.Title, 5)).Append("</row>") : r += 1
        rows.Append("<row r=""" & r & """>").Append(TextCell(0, r, doc.PeriodLabel & If(doc.Note.Length > 0, "   |   " & doc.Note, ""), 0)).Append("</row>") : r += 1
        rows.Append("<row r=""" & r & """>").Append(TextCell(0, r, "Generated " & doc.GeneratedAt.ToString("MMM d, yyyy h:mm tt", Inv) &
                    If(doc.GeneratedBy.Length > 0, " by " & doc.GeneratedBy, ""), 0)).Append("</row>") : r += 1
        r += 1

        ' status figures
        For Each k As ReportKpi In doc.Kpis
            rows.Append("<row r=""" & r & """>").Append(TextCell(0, r, k.Caption, 6)).Append(TextCell(1, r, k.Value.Replace(ReportFormat.PesoSign, "PHP "), 0)).Append("</row>")
            widths(0) = Math.Max(widths(0), Math.Min(40, k.Caption.Length + 2))
            widths(1) = Math.Max(widths(1), Math.Min(30, k.Value.Length + 2))
            r += 1
        Next
        If doc.Kpis.Count > 0 Then r += 1

        ' table
        If doc.Columns.Count > 0 Then
            If doc.TableTitle.Length > 0 Then
                rows.Append("<row r=""" & r & """>").Append(TextCell(0, r, doc.TableTitle, 6)).Append("</row>") : r += 1
            End If
            rows.Append("<row r=""" & r & """>")
            For i As Integer = 0 To doc.Columns.Count - 1
                rows.Append(TextCell(i, r, doc.Columns(i).ExportHeader, 1))
                widths(i) = Math.Max(widths(i), Math.Min(45, doc.Columns(i).ExportHeader.Length + 3))
            Next
            rows.Append("</row>")
            Dim headerRow As Integer = r
            r += 1

            If doc.Data IsNot Nothing Then
                For Each dr As DataRow In doc.Data.Rows
                    rows.Append("<row r=""" & r & """>")
                    For i As Integer = 0 To doc.Columns.Count - 1
                        Dim col As ReportColumn = doc.Columns(i)
                        Dim v As Object = dr(col.Field)
                        If v Is Nothing OrElse v Is DBNull.Value Then
                            ' empty cell
                        ElseIf col.Kind = ColKind.Money Then
                            rows.Append(NumberCell(i, r, Convert.ToDecimal(v), 2))
                        ElseIf col.Kind = ColKind.Number Then
                            rows.Append(NumberCell(i, r, Convert.ToDecimal(v), 3))
                        ElseIf col.Kind = ColKind.Percent Then
                            rows.Append(NumberCell(i, r, Convert.ToDecimal(v), 4))
                        Else
                            Dim t As String = ReportFormat.Cell(v, col.Kind)
                            rows.Append(TextCell(i, r, t, 0))
                            widths(i) = Math.Max(widths(i), Math.Min(45, t.Length + 2))
                        End If
                    Next
                    rows.Append("</row>")
                    r += 1
                Next
            End If
        End If

        Dim cols As New StringBuilder("<cols>")
        For i As Integer = 0 To widths.Count - 1
            cols.Append("<col min=""" & (i + 1) & """ max=""" & (i + 1) & """ width=""" & widths(i) & """ customWidth=""1""/>")
        Next
        cols.Append("</cols>")

        Dim sheet As String = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
            "<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">" & cols.ToString() &
            "<sheetData>" & rows.ToString() & "</sheetData></worksheet>"

        Dim sheetName As String = doc.Title
        For Each bad As Char In New Char() {"["c, "]"c, ":"c, "*"c, "?"c, "/"c, "\"c}
            sheetName = sheetName.Replace(bad, " "c)
        Next
        If sheetName.Length > 31 Then sheetName = sheetName.Substring(0, 31)
        If sheetName.Trim().Length = 0 Then sheetName = "Report"

        Dim styles As String = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
            "<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">" &
            "<numFmts count=""1""><numFmt numFmtId=""164"" formatCode=""0.0&quot;%&quot;""/></numFmts>" &
            "<fonts count=""4"">" &
            "<font><sz val=""11""/><name val=""Calibri""/></font>" &
            "<font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font>" &
            "<font><b/><sz val=""16""/><color rgb=""FF800000""/><name val=""Calibri""/></font>" &
            "<font><b/><sz val=""11""/><name val=""Calibri""/></font>" &
            "</fonts>" &
            "<fills count=""3""><fill><patternFill patternType=""none""/></fill><fill><patternFill patternType=""gray125""/></fill>" &
            "<fill><patternFill patternType=""solid""><fgColor rgb=""FF800000""/><bgColor indexed=""64""/></patternFill></fill></fills>" &
            "<borders count=""1""><border><left/><right/><top/><bottom/><diagonal/></border></borders>" &
            "<cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>" &
            "<cellXfs count=""7"">" &
            "<xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>" &
            "<xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>" &
            "<xf numFmtId=""4"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyNumberFormat=""1""/>" &
            "<xf numFmtId=""3"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyNumberFormat=""1""/>" &
            "<xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyNumberFormat=""1""/>" &
            "<xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>" &
            "<xf numFmtId=""0"" fontId=""3"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>" &
            "</cellXfs></styleSheet>"

        Dim contentTypes As String = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
            "<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">" &
            "<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>" &
            "<Default Extension=""xml"" ContentType=""application/xml""/>" &
            "<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>" &
            "<Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>" &
            "<Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>" &
            "</Types>"

        Dim rootRels As String = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
            "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
            "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>" &
            "</Relationships>"

        Dim workbook As String = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
            "<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">" &
            "<sheets><sheet name=""" & Clean(sheetName) & """ sheetId=""1"" r:id=""rId1""/></sheets></workbook>"

        Dim workbookRels As String = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
            "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
            "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>" &
            "<Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>" &
            "</Relationships>"

        If File.Exists(path) Then File.Delete(path)
        Using zip As ZipArchive = ZipFile.Open(path, ZipArchiveMode.Create)
            AddEntry(zip, "[Content_Types].xml", contentTypes)
            AddEntry(zip, "_rels/.rels", rootRels)
            AddEntry(zip, "xl/workbook.xml", workbook)
            AddEntry(zip, "xl/_rels/workbook.xml.rels", workbookRels)
            AddEntry(zip, "xl/styles.xml", styles)
            AddEntry(zip, "xl/worksheets/sheet1.xml", sheet)
        End Using
    End Sub

    Private Sub AddEntry(zip As ZipArchive, name As String, content As String)
        Dim entry As ZipArchiveEntry = zip.CreateEntry(name)
        Using s As Stream = entry.Open()
            Dim bytes As Byte() = New UTF8Encoding(False).GetBytes(content)
            s.Write(bytes, 0, bytes.Length)
        End Using
    End Sub

End Module
