Option Strict On
Option Explicit On

''' <summary>Who is logged in right now (filled in by the login form).</summary>
Public Module AppSession

    Public Property UserId As Integer
    Public Property Username As String = ""
    Public Property FullName As String = ""
    Public Property Role As String = ""

    Public Sub Clear()
        UserId = 0
        Username = ""
        FullName = ""
        Role = ""
    End Sub

End Module
