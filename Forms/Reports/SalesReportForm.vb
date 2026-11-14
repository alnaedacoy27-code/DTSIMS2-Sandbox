Option Strict On
Option Explicit On

Imports System.Data
Imports System.Windows.Forms

''' <summary>
''' Reports > Sales Report. Summary (total sales, transactions, top 10 products, sales per hour / day),
''' Transactions (every sale, open a receipt copy and reprint it) and Categories. Daily, weekly or monthly.
''' A cashier sees only their own sales.
''' </summary>
Public Class SalesReportForm
    Inherits ReportFormBase

    Protected Overrides ReadOnly Property ReportTitle As String
        Get
            Return "Sales Report"
        End Get
    End Property

    Protected Overrides ReadOnly Property ReportBlurb As String
        Get
            Return "Sales made at the POS, best sellers and receipt copies"
        End Get
    End Property

    Protected Overrides ReadOnly Property ViewNames As String()
        Get
            Return New String() {"Summary", "Receipts", "Categories"}
        End Get
    End Property

    Public Overrides ReadOnly Property WatchedTables As String()
        Get
            Return New String() {"sales", "sale_items", "products", "categories"}
        End Get
    End Property

    Private Shared Function CashierScope() As Integer
        If String.Equals(AppSession.Role, "cashier", StringComparison.OrdinalIgnoreCase) Then Return AppSession.UserId
        Return 0
    End Function

    Protected Overrides Function BuildDoc(view As Integer, range As DateRange, period As ReportPeriod) As ReportDoc
        Dim cid As Integer = CashierScope()
        Select Case view
            Case 1
                Return ReportData.SalesTransactions(period, range, cid)
            Case 2
                Return ReportData.SalesCategories(period, range, cid)
            Case Else
                Return ReportData.SalesSummary(period, range, cid)
        End Select
    End Function

    Protected Overrides ReadOnly Property ShortcutText As String
        Get
            Return "Receipt copy"
        End Get
    End Property

    ''' <summary>The "Receipt copy" button at the top: jump to the Receipts tab where a sale can be found and reprinted.</summary>
    Protected Overrides Sub OnShortcut()
        ShowView(1)
    End Sub

    Protected Overrides Function RowActionText(view As Integer) As String
        Return If(view = 1, "View receipt", "")
    End Function

    Protected Overrides Function RowAction2Text(view As Integer) As String
        Return If(view = 1, "Print copy", "")
    End Function

    Protected Overrides Function SearchEnabled(view As Integer) As Boolean
        Return view = 1
    End Function

    ''' <summary>Prints a copy of the selected sale's receipt straight away (on the receipt printer), stamped REPRINT / COPY.</summary>
    Protected Overrides Sub OnRowAction2(view As Integer, row As DataRowView)
        Dim saleId As Integer = Convert.ToInt32(row("id"))
        Dim receiptNo As String = Convert.ToString(row("receipt_no"))
        If Not Confirm(Me, "Print receipt copy", "Print a copy of receipt " & receiptNo & " on the receipt printer?", MessageBoxIcon.Question) Then Return

        Try
            Dim model As ReceiptModel = PosRepository.LoadReceipt(saleId)
            If model Is Nothing Then
                MessageBox.Show(OwnerOf(Me), "This sale could not be found any more.", "Sales Report", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            model.IsReprint = True
            ReceiptRenderer.PrintReceipt(model)
        Catch ex As Exception
            MessageBox.Show(OwnerOf(Me), "The receipt could not be printed." & vbCrLf & vbCrLf & ex.Message & vbCrLf & vbCrLf &
                            "Check the receipt printer in Utilities > Devices.", "Sales Report", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        Try
            AuditLog.Write("REPRINT", "Sales Report", "Receipt copy printed: " & receiptNo)
        Catch
            ' the audit trail must never stop a reprint
        End Try
    End Sub

    Protected Overrides Sub OnRowAction(view As Integer, row As DataRowView)
        Dim saleId As Integer = Convert.ToInt32(row("id"))
        Dim model As ReceiptModel = Nothing
        Try
            model = PosRepository.LoadReceipt(saleId)
        Catch ex As Exception
            MessageBox.Show(OwnerOf(Me), "The receipt could not be loaded." & vbCrLf & ex.Message, "Sales Report", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try
        If model Is Nothing Then
            MessageBox.Show(OwnerOf(Me), "This sale could not be found any more.", "Sales Report", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using dlg As New ReceiptCopyDialog(model)
            dlg.ShowDialog(OwnerOf(Me))
        End Using
    End Sub

End Class
