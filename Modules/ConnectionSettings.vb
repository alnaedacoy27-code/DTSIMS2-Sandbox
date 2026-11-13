Option Strict On
Option Explicit On

Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Where this laptop finds the database. The main laptop uses "localhost";
''' the other laptops use the main laptop's address. Saved per Windows user in
''' %APPDATA%\DTSIMS2\connection.cfg, with the password encrypted (DPAPI).
''' </summary>
Public Class ConnectionSettings

    Public Property Server As String = "localhost"
    Public Property Port As Integer = 3306
    Public Property Database As String = "dtsims2_sandbox"
    Public Property User As String = "darwin_server"
    Public Property Password As String = "darwin_123"

    Private Shared ReadOnly Entropy As Byte() = Encoding.UTF8.GetBytes("DTSIMS2-connection")

    Public Shared Function FilePath() As String
        Dim dir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DTSIMS2-Sandbox")
        Return Path.Combine(dir, "connection.cfg")
    End Function

    Public Function Copy() As ConnectionSettings
        Dim c As New ConnectionSettings()
        c.Server = Server
        c.Port = Port
        c.Database = Database
        c.User = User
        c.Password = Password
        Return c
    End Function

    Public Shared Function Load() As ConnectionSettings
        Dim s As New ConnectionSettings()
        Try
            Dim cfgFile As String = FilePath()
            If Not File.Exists(cfgFile) Then Return s

            For Each line As String In File.ReadAllLines(cfgFile)
                Dim idx As Integer = line.IndexOf("="c)
                If idx <= 0 Then Continue For
                Dim key As String = line.Substring(0, idx).Trim().ToLowerInvariant()
                Dim value As String = line.Substring(idx + 1).Trim()

                Select Case key
                    Case "server"
                        If value.Length > 0 Then s.Server = value
                    Case "port"
                        Dim p As Integer
                        If Integer.TryParse(value, p) AndAlso p > 0 AndAlso p < 65536 Then s.Port = p
                    Case "database"
                        If value.Length > 0 Then s.Database = value
                    Case "user"
                        If value.Length > 0 Then s.User = value
                    Case "passwordprotected"
                        Try
                            Dim raw As Byte() = ProtectedData.Unprotect(Convert.FromBase64String(value), Entropy, DataProtectionScope.CurrentUser)
                            s.Password = Encoding.UTF8.GetString(raw)
                        Catch
                            ' saved by another Windows user - keep the default, they can re-enter it
                        End Try
                End Select
            Next
        Catch
            ' unreadable file: fall back to defaults
        End Try
        Return s
    End Function

    Public Sub Save()
        Dim cfgFile As String = FilePath()
        Directory.CreateDirectory(Path.GetDirectoryName(cfgFile))
        Dim protectedPw As String = Convert.ToBase64String(
            ProtectedData.Protect(Encoding.UTF8.GetBytes(Password), Entropy, DataProtectionScope.CurrentUser))
        Dim lines As String() = {
            "Server=" & Server,
            "Port=" & Port.ToString(),
            "Database=" & Database,
            "User=" & User,
            "PasswordProtected=" & protectedPw}
        File.WriteAllLines(cfgFile, lines)
    End Sub

End Class
