Option Strict On
Option Explicit On

Imports System.Data
Imports System.Windows.Forms

''' <summary>
''' Reports > Loss Report. What the shop gave up to its customers: percentage discounts, approved special prices
''' and points redeemed. Summary, the sales behind it, who gave it (cashiers and approvers), and the sales that
''' went below cost (the only real loss). Daily, weekly or monthly. Admin and Superadmin only.
''' </summary>
Public Class LossReportForm
    Inherits ReportFormBase

    Protected Overrides ReadOnly Property ReportTitle As String
        Get
            Return "Loss Report"
        End Get
    End Property

    Protected Overrides ReadOnly Property ReportBlurb As String
        Get
            Return "Discounts, special prices and points given to customers"
        End Get
    End Property

    Protected Overrides ReadOnly Property ViewNames As String()
        Get
            Return New String() {"Summary", "Sales", "Cashiers", "Below cost"}
        End Get
    End Property

    Public Overrides ReadOnly Property WatchedTables As String()
        Get
            Return New String() {"sales", "sale_items"}
        End Get
    End Property

    Protected Overrides Function BuildDoc(view As Integer, range As DateRange, period As ReportPeriod) As ReportDoc
        Select Case view
            Case 1
                Return LossData.SalesList(period, range)
            Case 2
                Return LossData.Cashiers(period, range)
            Case 3
                Return LossData.BelowCost(period, range)
            Case Else
                Return LossData.Summary(period, range)
        End Select
    End Function

    Protected Overrides Function RowActionText(view As Integer) As String
        Return If(view = 1 OrElse view = 3, "View receipt", "")
    End Function

    Protected Overrides Function SearchEnabled(view As Integer) As Boolean
        Return view = 1 OrElse view = 3
    End Function

    Protected Overrides Sub OnRowAction(view As Integer, row As DataRowView)
        Dim saleId As Integer = Convert.ToInt32(row("id"))
        Dim model As ReceiptModel = Nothing
        Try
            model = PosRepository.LoadReceipt(saleId)
        Catch ex As Exception
            MessageBox.Show(OwnerOf(Me), "The receipt could not be loaded." & vbCrLf & ex.Message, "Loss Report", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try
        If model Is Nothing Then
            MessageBox.Show(OwnerOf(Me), "This sale could not be found any more.", "Loss Report", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using dlg As New ReceiptCopyDialog(model)
            dlg.ShowDialog(OwnerOf(Me))
        End Using
    End Sub

End Class
