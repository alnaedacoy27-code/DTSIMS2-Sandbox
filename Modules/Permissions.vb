Option Strict On
Option Explicit On

''' <summary>
''' Hardcoded role defaults, so a damaged role_permissions table can never lock anyone out.
'''   superadmin : everything
'''   admin      : everything except Settings (and user monitoring)
'''   cashier    : POS and Sales Report only
''' </summary>
Public Module Permissions

    Private ReadOnly AdminDenied As String() = {"vat", "discounts", "stock_threshold", "database_settings", "user_monitoring"}
    Private ReadOnly CashierAllowed As String() = {"pos", "sales_report", "text_size"}

    Public Function NormalizeRole(role As String) As String
        If role Is Nothing Then Return ""
        Return role.Trim().ToLowerInvariant()
    End Function

    Public Function CanAccess(role As String, moduleKey As String) As Boolean
        Select Case NormalizeRole(role)
            Case "superadmin"
                Return True
            Case "admin"
                Return Array.IndexOf(AdminDenied, moduleKey) < 0
            Case "cashier"
                Return Array.IndexOf(CashierAllowed, moduleKey) >= 0
            Case Else
                Return False
        End Select
    End Function

    Public Function RoleDisplay(role As String) As String
        Select Case NormalizeRole(role)
            Case "superadmin"
                Return "Superadmin"
            Case "admin"
                Return "Admin"
            Case "cashier"
                Return "Cashier"
            Case Else
                Return "No role"
        End Select
    End Function

End Module
