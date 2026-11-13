Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Drawing.Text
Imports System.IO
Imports System.IO.Ports
Imports System.Management
Imports System.Text.RegularExpressions

Public Class PrinterInfo
    Public Property Name As String = ""
    Public Property IsDefault As Boolean
    Public Property StatusText As String = "Unknown"
    Public Property IsReady As Boolean
    Public Property Port As String = ""
    Public Property Driver As String = ""
End Class

''' <summary>Settings that belong to THIS laptop (the printer plugged into it), kept in %APPDATA%\DTSIMS2\devices.cfg.</summary>
Public Module LocalDevices

    Private Function FilePath() As String
        Dim dir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DTSIMS2-Sandbox")
        Return Path.Combine(dir, "devices.cfg")
    End Function

    Private Function ReadAll() As Dictionary(Of String, String)
        Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Try
            If File.Exists(FilePath()) Then
                For Each line As String In File.ReadAllLines(FilePath())
                    Dim i As Integer = line.IndexOf("="c)
                    If i > 0 Then d(line.Substring(0, i).Trim()) = line.Substring(i + 1).Trim()
                Next
            End If
        Catch
            ' start empty
        End Try
        Return d
    End Function

    Public Function GetValue(key As String) As String
        Dim d As Dictionary(Of String, String) = ReadAll()
        Return If(d.ContainsKey(key), d(key), "")
    End Function

    Public Sub SetValue(key As String, value As String)
        Dim d As Dictionary(Of String, String) = ReadAll()
        d(key) = value
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath()))
        Dim lines As New List(Of String)()
        For Each kv As KeyValuePair(Of String, String) In d
            lines.Add(kv.Key & "=" & kv.Value)
        Next
        File.WriteAllLines(FilePath(), lines.ToArray())
    End Sub

    ''' <summary>The printer chosen for receipts and barcode labels on this laptop ("" = the Windows default).</summary>
    Public Property ReceiptPrinter As String
        Get
            Return GetValue("receipt_printer")
        End Get
        Set(v As String)
            SetValue("receipt_printer", If(v, ""))
        End Set
    End Property

    ''' <summary>Points a print job at the chosen receipt printer, when it is still installed.</summary>
    Public Sub ApplyReceiptPrinter(doc As PrintDocument)
        Dim name As String = ReceiptPrinter
        If name.Length = 0 Then Return
        For Each p As String In PrinterSettings.InstalledPrinters
            If String.Equals(p, name, StringComparison.OrdinalIgnoreCase) Then
                doc.PrinterSettings.PrinterName = p
                Return
            End If
        Next
    End Sub

End Module

Public Module PrinterTools

    Private Function StatusFor(workOffline As Boolean, errorState As Integer, printerStatus As Integer) As String
        If workOffline Then Return "Offline: switched off, unplugged or paused"
        Select Case errorState
            Case 4 : Return "Out of paper"
            Case 3 : Return "Paper is low"
            Case 7 : Return "Cover is open"
            Case 8 : Return "Paper jam"
            Case 9 : Return "Offline: switched off or unplugged"
            Case 10 : Return "Needs service"
            Case 11 : Return "Output bin is full"
        End Select
        Select Case printerStatus
            Case 3 : Return "Ready"
            Case 4 : Return "Printing"
            Case 5 : Return "Warming up"
            Case 6 : Return "Stopped"
            Case 7 : Return "Offline: switched off or unplugged"
        End Select
        Return "Unknown"
    End Function

    ''' <summary>Every printer Windows knows, with its state (ready / offline / out of paper) when Windows can tell.</summary>
    Public Function ListPrinters() As List(Of PrinterInfo)
        Dim result As New List(Of PrinterInfo)()
        Dim defaultName As String = ""
        Try
            Dim ps As New PrinterSettings()
            defaultName = ps.PrinterName
        Catch
            ' no default
        End Try

        For Each name As String In PrinterSettings.InstalledPrinters
            Dim p As New PrinterInfo()
            p.Name = name
            p.IsDefault = String.Equals(name, defaultName, StringComparison.OrdinalIgnoreCase)
            result.Add(p)
        Next

        ' the state of each printer, from Windows
        Try
            Using searcher As New ManagementObjectSearcher("SELECT Name, WorkOffline, PrinterStatus, DetectedErrorState, PortName, DriverName FROM Win32_Printer")
                For Each mo As ManagementObject In searcher.Get()
                    Dim n As String = Convert.ToString(mo("Name"))
                    For Each p As PrinterInfo In result
                        If String.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase) Then
                            Dim off As Boolean = (mo("WorkOffline") IsNot Nothing AndAlso Convert.ToBoolean(mo("WorkOffline")))
                            Dim err As Integer = If(mo("DetectedErrorState") Is Nothing, 0, Convert.ToInt32(mo("DetectedErrorState")))
                            Dim st As Integer = If(mo("PrinterStatus") Is Nothing, 0, Convert.ToInt32(mo("PrinterStatus")))
                            p.StatusText = StatusFor(off, err, st)
                            p.IsReady = (p.StatusText = "Ready" OrElse p.StatusText = "Printing" OrElse p.StatusText = "Warming up" OrElse p.StatusText = "Paper is low")
                            p.Port = Convert.ToString(mo("PortName"))
                            p.Driver = Convert.ToString(mo("DriverName"))
                        End If
                    Next
                Next
            End Using
        Catch
            ' Windows would not say: the status stays "Unknown" and the test print is the real check
        End Try
        Return result
    End Function

    ''' <summary>Prints a short test slip on the chosen printer (58 mm roll). Throws when it cannot print.</summary>
    Public Sub PrintTestSlip(printerName As String, shopName As String)
        Using doc As New PrintDocument()
            doc.PrinterSettings.PrinterName = printerName
            If Not doc.PrinterSettings.IsValid Then Throw New InvalidOperationException("Windows does not know a printer named '" & printerName & "'.")
            doc.DocumentName = "DTSIMS2 printer test"
            doc.PrintController = New StandardPrintController()
            doc.OriginAtMargins = True
            doc.DefaultPageSettings.Margins = New Margins(24, 24, 6, 6)
            doc.DefaultPageSettings.PaperSize = New PaperSize("Receipt 58mm", 228, 560)
            AddHandler doc.PrintPage,
                Sub(sender As Object, e As PrintPageEventArgs)
                    DrawTestSlip(e.Graphics, shopName, printerName)
                    e.HasMorePages = False
                End Sub
            doc.Print()
        End Using
    End Sub

    Private Sub DrawTestSlip(g As Graphics, shopName As String, printerName As String)
        Dim w As Single = ReceiptRenderer.PaperWidth
        g.TextRenderingHint = TextRenderingHint.AntiAlias
        Using bold As New Font("Consolas", 10.0F, FontStyle.Bold)
            Using normal As New Font("Consolas", 8.0F)
                Using small As New Font("Consolas", 7.0F)
                    Using center As New StringFormat()
                        center.Alignment = StringAlignment.Center
                        Dim y As Single = 2.0F

                        ' a box along the full printable width: if an edge is cut, the width is wrong
                        g.DrawRectangle(Pens.Black, 0.5F, 0.5F, w - 1.0F, 360.0F)
                        y += 6.0F

                        g.DrawString(shopName, bold, Brushes.Black, New RectangleF(0, y, w, 20), center)
                        y += 20.0F
                        g.DrawString("PRINTER TEST", bold, Brushes.Black, New RectangleF(0, y, w, 20), center)
                        y += 24.0F
                        g.DrawString(DateTime.Now.ToString("MMM d, yyyy  h:mm tt"), normal, Brushes.Black, New RectangleF(0, y, w, 14), center)
                        y += 16.0F
                        g.DrawString(printerName, small, Brushes.Black, New RectangleF(4, y, w - 8, 28), center)
                        y += 30.0F

                        g.DrawString("1234567890123456789012345678", normal, Brushes.Black, New RectangleF(2, y, w - 4, 14))
                        y += 16.0F
                        g.DrawString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", normal, Brushes.Black, New RectangleF(2, y, w - 4, 14))
                        y += 20.0F

                        ' a sample barcode: the same kind the labels and cards use
                        Dim bits As String = Code128.Encode("TEST-123456")
                        If bits.Length > 0 Then
                            Dim mw As Single = (w - 24.0F) / bits.Length
                            Dim i As Integer = 0
                            While i < bits.Length
                                If bits(i) = "1"c Then
                                    Dim j As Integer = i
                                    While j < bits.Length AndAlso bits(j) = "1"c
                                        j += 1
                                    End While
                                    g.FillRectangle(Brushes.Black, 12.0F + i * mw, y, (j - i) * mw, 40.0F)
                                    i = j
                                Else
                                    i += 1
                                End If
                            End While
                            y += 42.0F
                            g.DrawString("TEST-123456", small, Brushes.Black, New RectangleF(0, y, w, 12), center)
                            y += 20.0F
                        End If

                        g.DrawString("If you can read all of this," & Environment.NewLine & "the printer is working.", normal, Brushes.Black, New RectangleF(0, y, w, 30), center)
                    End Using
                End Using
            End Using
        End Using
    End Sub

End Module

Public Module ScannerTools

    ''' <summary>Devices Windows lists that look like a barcode scanner (many scanners pose as a plain keyboard and cannot be told apart).</summary>
    Public Function LookLikeScanners() As List(Of String)
        Dim names As New List(Of String)()
        Try
            Using searcher As New ManagementObjectSearcher("SELECT Name FROM Win32_PnPEntity WHERE Name IS NOT NULL")
                For Each mo As ManagementObject In searcher.Get()
                    Dim n As String = Convert.ToString(mo("Name"))
                    If Regex.IsMatch(n, "scanner|barcode|bar code|symbol|honeywell|zebra|datalogic|newland|opticon|code reader|imager", RegexOptions.IgnoreCase) AndAlso Not names.Contains(n) Then names.Add(n)
                Next
            End Using
        Catch
            ' cannot ask Windows
        End Try
        Return names
    End Function

    Public Function SerialPortsText() As String
        Try
            Dim p As String() = SerialPort.GetPortNames()
            Return If(p.Length = 0, "none", String.Join(", ", p))
        Catch
            Return "unknown"
        End Try
    End Function

    ''' <summary>
    ''' A scanner types the code very fast (a few milliseconds per character) and ends with Enter,
    ''' a person types slowly. Returns True when this scan looks like it came from a scanner.
    ''' </summary>
    Public Function LooksLikeScan(charCount As Integer, totalMs As Double) As Boolean
        If charCount < 4 Then Return False
        Return (totalMs / Math.Max(1, charCount - 1)) <= 45.0
    End Function

End Module
