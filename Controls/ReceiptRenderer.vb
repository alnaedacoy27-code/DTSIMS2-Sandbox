Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.Drawing.Text
Imports System.Globalization

Public Class ReceiptLine
    Public Property Name As String = ""
    Public Property Size As String = ""
    Public Property Qty As Integer
    Public Property UnitPrice As Decimal
    Public Property LineTotal As Decimal
End Class

Public Class ReceiptModel
    Public Property ShopName As String = "DARWIN TRADING"
    Public Property ShopAddress As String = ""
    Public Property ShopContact As String = ""
    Public Property ShopTin As String = ""
    Public Property ReceiptNo As String = ""
    Public Property SoldAt As DateTime = DateTime.Now
    Public Property CashierName As String = ""
    Public Property Lines As New List(Of ReceiptLine)()
    Public Property Subtotal As Decimal
    Public Property DiscountName As String = ""
    Public Property DiscountAmount As Decimal
    Public Property SpecialAmount As Decimal
    Public Property ApprovedBy As String = ""
    Public Property VatName As String = "VAT"
    Public Property VatPercent As Decimal
    Public Property VatAmount As Decimal
    Public Property VatableSales As Decimal
    Public Property Total As Decimal
    Public Property Paid As Decimal
    Public Property Change As Decimal
    Public Property CustomerName As String = ""
    Public Property PointsUsed As Decimal
    Public Property PointsValue As Decimal
    Public Property PointsEarned As Decimal
    Public Property PointsBalance As Decimal
    Public Property HasPointsBalance As Boolean
End Class

''' <summary>
''' Draws the receipt on a screen OR on a printer with the same code, so what you see is what prints.
''' Widths are in drawing units: 180 fits a 58 mm roll.
''' </summary>
Public Module ReceiptRenderer

    ' 58 mm roll: about 48 mm is printable = 180 drawing units
    Public Const PaperWidth As Single = 180.0F

    Public Function Num(amount As Decimal) As String
        Return amount.ToString("N2", CultureInfo.InvariantCulture)
    End Function

    Private Class Painter
        Public G As Graphics
        Public Width As Single
        Public Y As Single
        Public DrawIt As Boolean
        Public ReadOnly Normal As New Font("Consolas", 8.0F)
        Public ReadOnly Bold As New Font("Consolas", 8.0F, FontStyle.Bold)
        Public ReadOnly Big As New Font("Consolas", 11.0F, FontStyle.Bold)
        Public ReadOnly Small As New Font("Consolas", 7.0F)

        Public Sub Center(text As String, font As Font)
            Using fmt As New StringFormat()
                fmt.Alignment = StringAlignment.Center
                Dim h As Single = G.MeasureString(text, font, CInt(Width), fmt).Height
                If DrawIt Then G.DrawString(text, font, Brushes.Black, New RectangleF(0, Y, Width, h), fmt)
                Y += h
            End Using
        End Sub

        Public Sub Wrapped(text As String, font As Font)
            Dim h As Single = G.MeasureString(text, font, CInt(Width)).Height
            If DrawIt Then G.DrawString(text, font, Brushes.Black, New RectangleF(0, Y, Width, h))
            Y += h
        End Sub

        Public Sub Row(left As String, right As String, font As Font)
            Dim rightW As Single = G.MeasureString(right, font).Width
            Dim leftW As Single = Math.Max(20.0F, Width - rightW - 4.0F)
            Dim h As Single = Math.Max(G.MeasureString(left, font, CInt(leftW)).Height, font.GetHeight(G))
            If DrawIt Then
                G.DrawString(left, font, Brushes.Black, New RectangleF(0, Y, leftW, h))
                Using fmt As New StringFormat()
                    fmt.Alignment = StringAlignment.Far
                    G.DrawString(right, font, Brushes.Black, New RectangleF(0, Y, Width, h), fmt)
                End Using
            End If
            Y += h
        End Sub

        Public Sub Rule()
            Y += 3
            If DrawIt Then
                Using p As New Pen(Color.Black, 1.0F)
                    p.DashStyle = DashStyle.Dash
                    G.DrawLine(p, 0, Y, Width, Y)
                End Using
            End If
            Y += 5
        End Sub

        Public Sub Gap(h As Single)
            Y += h
        End Sub

        Public Sub DisposeFonts()
            Normal.Dispose()
            Bold.Dispose()
            Big.Dispose()
            Small.Dispose()
        End Sub
    End Class

    ''' <summary>Draws (draw = True) or only measures (draw = False) the receipt. Returns its height.</summary>
    Public Function Render(g As Graphics, m As ReceiptModel, width As Single, draw As Boolean) As Single
        Dim p As New Painter()
        p.G = g
        p.Width = width
        p.DrawIt = draw
        Try
            g.TextRenderingHint = TextRenderingHint.AntiAlias

            p.Center(m.ShopName, p.Big)
            If m.ShopAddress.Length > 0 Then p.Center(m.ShopAddress, p.Small)
            If m.ShopContact.Length > 0 Then p.Center(m.ShopContact, p.Small)
            If m.ShopTin.Length > 0 Then p.Center("TIN: " & m.ShopTin, p.Small)
            p.Rule()
            p.Center("SALES RECEIPT", p.Bold)
            p.Gap(2)
            p.Row("No: " & m.ReceiptNo, "", p.Normal)
            p.Row("Date: " & m.SoldAt.ToString("MMM d, yyyy"), m.SoldAt.ToString("h:mm tt"), p.Normal)
            If m.CashierName.Length > 0 Then p.Row("Cashier: " & m.CashierName, "", p.Normal)
            p.Rule()

            For Each l As ReceiptLine In m.Lines
                p.Wrapped(l.Name & If(l.Size.Length > 0, " " & l.Size, ""), p.Normal)
                p.Row("   " & l.Qty.ToString() & " x " & Num(l.UnitPrice), Num(l.LineTotal), p.Normal)
            Next
            p.Rule()

            p.Row("Subtotal", Num(m.Subtotal), p.Normal)
            If m.DiscountAmount > 0D Then
                p.Row("Discount" & If(m.DiscountName.Length > 0, " " & m.DiscountName, ""), "-" & Num(m.DiscountAmount), p.Normal)
            End If
            If m.SpecialAmount > 0D Then p.Row("Special price", "-" & Num(m.SpecialAmount), p.Normal)
            p.Gap(2)
            p.Row("TOTAL", Num(m.Total), p.Big)
            If m.PointsValue > 0D Then p.Row("Points (" & Loyalty.Fmt(m.PointsUsed) & " pts)", "-" & Num(m.PointsValue), p.Normal)
            p.Row("Cash", Num(m.Paid), p.Normal)
            p.Row("Change", Num(m.Change), p.Normal)
            p.Rule()

            If m.CustomerName.Length > 0 Then
                p.Wrapped("Loyalty member: " & m.CustomerName, p.Normal)
                If m.PointsUsed > 0D Then p.Row("Points used", Loyalty.Fmt(m.PointsUsed), p.Normal)
                If m.PointsEarned > 0D Then p.Row("Points earned", "+" & Loyalty.Fmt(m.PointsEarned), p.Normal)
                If m.HasPointsBalance Then p.Row("Points balance", Loyalty.Fmt(m.PointsBalance), p.Bold)
                p.Rule()
            End If

            If m.VatPercent > 0D Then
                p.Row("VATable sales", Num(m.VatableSales), p.Small)
                p.Row(m.VatName & " " & m.VatPercent.ToString("0.##", CultureInfo.InvariantCulture) & "% (included)", Num(m.VatAmount), p.Small)
                p.Rule()
            End If
            If m.SpecialAmount > 0D AndAlso m.ApprovedBy.Length > 0 Then
                p.Center("Special price approved by " & m.ApprovedBy, p.Small)
                p.Gap(2)
            End If
            p.Center("Thank you! Please come again.", p.Normal)
            p.Gap(10)
            Return p.Y
        Finally
            p.DisposeFonts()
        End Try
    End Function

    ''' <summary>Sends the receipt to the default printer (a 58 mm roll). Throws if there is no printer.</summary>
    Public Sub PrintReceipt(m As ReceiptModel)
        Dim height As Single
        Using bmp As New Bitmap(1, 1)
            Using g As Graphics = Graphics.FromImage(bmp)
                height = Render(g, m, PaperWidth, False)
            End Using
        End Using

        Using doc As New PrintDocument()
            doc.DocumentName = m.ReceiptNo
            doc.PrintController = New StandardPrintController()
            LocalDevices.ApplyReceiptPrinter(doc)
            doc.OriginAtMargins = True
            doc.DefaultPageSettings.Margins = New Margins(24, 24, 6, 6)
            doc.DefaultPageSettings.PaperSize = New PaperSize("Receipt 58mm", 228, CInt(height) + 60)
            AddHandler doc.PrintPage,
                Sub(sender As Object, e As PrintPageEventArgs)
                    Render(e.Graphics, m, PaperWidth, True)
                    e.HasMorePages = False
                End Sub
            doc.Print()
        End Using
    End Sub

End Module
