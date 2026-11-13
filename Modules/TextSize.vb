Option Strict On
Option Explicit On

''' <summary>
''' Text size of THIS computer (Settings > Text Size). Kept in the local settings file
''' (%APPDATA%\DTSIMS2-Sandbox\devices.cfg), not in the database, because every laptop has its own screen.
''' Default = the current look; Medium and Large are bigger. Read once when the app starts.
''' </summary>
Public Module TextSize

    Public ReadOnly Names As String() = {"Default", "Medium", "Large"}

    Public Function ScaleFor(name As String) As Single
        Select Case name
            Case "Medium"
                Return 1.26F
            Case "Large"
                Return 1.4F
            Case Else
                Return 1.12F
        End Select
    End Function

    Public Function Current() As String
        Dim v As String = LocalDevices.GetValue("text_size")
        For Each n As String In Names
            If String.Equals(n, v, StringComparison.OrdinalIgnoreCase) Then Return n
        Next
        Return "Default"
    End Function

    Public Function CurrentScale() As Single
        Return ScaleFor(Current())
    End Function

    Public Sub Save(name As String)
        LocalDevices.SetValue("text_size", name)
    End Sub

End Module
