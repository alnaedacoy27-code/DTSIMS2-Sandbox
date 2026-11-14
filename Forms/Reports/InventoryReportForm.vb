Option Strict On
Option Explicit On

''' <summary>
''' Reports > Inventory Report. Stock received, sold, lost and adjusted for a day, week or month:
''' a summary with charts and a full movement log. (Current stock levels are in Reports > Inventory.)
''' </summary>
Public Class InventoryReportForm
    Inherits ReportFormBase

    Protected Overrides ReadOnly Property ReportTitle As String
        Get
            Return "Inventory Report"
        End Get
    End Property

    Protected Overrides ReadOnly Property ReportBlurb As String
        Get
            Return "Stock in and stock out records"
        End Get
    End Property

    Protected Overrides ReadOnly Property ViewNames As String()
        Get
            Return New String() {"Summary", "Movements"}
        End Get
    End Property

    Public Overrides ReadOnly Property WatchedTables As String()
        Get
            Return New String() {"inventory_movements", "products"}
        End Get
    End Property

    Protected Overrides Function BuildDoc(view As Integer, range As DateRange, period As ReportPeriod) As ReportDoc
        If view = 1 Then Return ReportData.InventoryMovements(period, range)
        Return ReportData.InventorySummary(period, range)
    End Function

    Protected Overrides Function SearchEnabled(view As Integer) As Boolean
        Return view = 1
    End Function

End Class
