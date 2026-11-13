Option Strict On
Option Explicit On

Imports System.Security.Cryptography

''' <summary>
''' Salted PBKDF2-SHA256 password hashes. Stored as
''' PBKDF2-SHA256$iterations$salt$hash  (never the password itself).
''' </summary>
Public Module PasswordHasher

    Private Const Iterations As Integer = 120000
    Private Const SaltSize As Integer = 16
    Private Const HashSize As Integer = 32
    Private Const Prefix As String = "PBKDF2-SHA256"

    Public Function Hash(password As String) As String
        Dim salt(SaltSize - 1) As Byte
        Using rng As New RNGCryptoServiceProvider()
            rng.GetBytes(salt)
        End Using
        Dim key As Byte() = Derive(password, salt, Iterations)
        Return Prefix & "$" & Iterations.ToString() & "$" & Convert.ToBase64String(salt) & "$" & Convert.ToBase64String(key)
    End Function

    Public Function Verify(password As String, stored As String) As Boolean
        Try
            If String.IsNullOrEmpty(stored) Then Return False
            Dim parts As String() = stored.Split("$"c)
            If parts.Length <> 4 OrElse parts(0) <> Prefix Then Return False

            Dim iter As Integer = Integer.Parse(parts(1))
            Dim salt As Byte() = Convert.FromBase64String(parts(2))
            Dim expected As Byte() = Convert.FromBase64String(parts(3))
            Dim actual As Byte() = Derive(password, salt, iter)

            ' constant-time compare
            If actual.Length <> expected.Length Then Return False
            Dim diff As Integer = 0
            For i As Integer = 0 To actual.Length - 1
                diff = diff Or (actual(i) Xor expected(i))
            Next
            Return diff = 0
        Catch
            Return False
        End Try
    End Function

    Private Function Derive(password As String, salt As Byte(), iter As Integer) As Byte()
        Using kdf As New Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256)
            Return kdf.GetBytes(HashSize)
        End Using
    End Function

End Module
