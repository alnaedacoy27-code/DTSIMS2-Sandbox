Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Windows.Forms

Public Class ModuleInfo
    Public ReadOnly Property Key As String
    Public ReadOnly Property Group As String
    Public ReadOnly Property Title As String
    Public ReadOnly Property Description As String
    Public ReadOnly Property Shortcut As Keys

    Public Sub New(key As String, group As String, title As String, description As String, Optional shortcut As Keys = Keys.None)
        Me.Key = key
        Me.Group = group
        Me.Title = title
        Me.Description = description
        Me.Shortcut = shortcut
    End Sub
End Class

''' <summary>One list of every module: drives the menu bar AND the home screen tiles.</summary>
Public Module ModuleCatalog

    Public ReadOnly GroupOrder As String() = {"File Maintenance", "Reports", "Transactions", "Settings", "Utilities"}

    Public ReadOnly All As List(Of ModuleInfo) = BuildAll()

    Private Function BuildAll() As List(Of ModuleInfo)
        Dim l As New List(Of ModuleInfo)()

        l.Add(New ModuleInfo("users", "File Maintenance", "Users", "Add, edit and remove users and their roles", Keys.Alt Or Keys.U))
        l.Add(New ModuleInfo("suppliers", "File Maintenance", "Suppliers", "Suppliers, companies and branches", Keys.Alt Or Keys.S))
        l.Add(New ModuleInfo("products", "File Maintenance", "Products", "Products, sizes, barcodes and categories", Keys.Alt Or Keys.P))

        l.Add(New ModuleInfo("inventory", "Reports", "Inventory", "Current stock and low-stock warnings", Keys.Alt Or Keys.I))
        l.Add(New ModuleInfo("inventory_report", "Reports", "Inventory Report", "Stock in and stock out records"))
        l.Add(New ModuleInfo("delivery_report", "Reports", "Delivery Report", "Deliveries received from suppliers"))
        l.Add(New ModuleInfo("sales_report", "Reports", "Sales Report", "Sales, top products and receipt copies"))
        l.Add(New ModuleInfo("loss_report", "Reports", "Loss Report", "Discounts, special prices and points given"))

        l.Add(New ModuleInfo("pos", "Transactions", "POS", "Point of sale: ring up customers", Keys.F2))
        l.Add(New ModuleInfo("customers", "Transactions", "Customer Accounts", "Loyalty cards and points"))
        l.Add(New ModuleInfo("delivery", "Transactions", "Delivery Management", "Receive supplier deliveries into inventory", Keys.Alt Or Keys.D))

        l.Add(New ModuleInfo("vat", "Settings", "VAT", "VAT name and percentage"))
        l.Add(New ModuleInfo("discounts", "Settings", "Discounts", "Discount names and percentages"))
        l.Add(New ModuleInfo("stock_threshold", "Settings", "Stock Threshold", "Minimum stock warning levels"))
        l.Add(New ModuleInfo("database_settings", "Settings", "Database Settings", "Backup, restore and backup folder"))
        l.Add(New ModuleInfo("text_size", "Settings", "Text Size", "Make the text bigger or smaller on this computer"))

        l.Add(New ModuleInfo("user_monitoring", "Utilities", "User Monitoring", "Who is logged in, on which PC, and what they are doing", Keys.Alt Or Keys.M))
        l.Add(New ModuleInfo("devices", "Utilities", "Devices", "SMS modem, receipt printer and barcode scanner: check and test", Keys.Alt Or Keys.E))

        Return l
    End Function

    ''' <summary>Modules this role may open, in menu order.</summary>
    Public Function ForRole(role As String) As List(Of ModuleInfo)
        Dim result As New List(Of ModuleInfo)()
        For Each g As String In GroupOrder
            For Each m As ModuleInfo In All
                If m.Group = g AndAlso Permissions.CanAccess(role, m.Key) Then result.Add(m)
            Next
        Next
        Return result
    End Function

End Module
