Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports MySql.Data.MySqlClient

Public Enum DbStatus
    Ok
    CannotConnect
    SchemaMissing
    SchemaOutdated
End Enum

''' <summary>The only place that talks to MySQL. Everything else calls these helpers.</summary>
Public Module Db

    ''' <summary>The schema_version this build of the app expects.</summary>
    Public Const RequiredSchemaVersion As Integer = 6

    Private _settings As New ConnectionSettings()

    Public ReadOnly Property Settings As ConnectionSettings
        Get
            Return _settings
        End Get
    End Property

    Public Sub Configure(s As ConnectionSettings)
        _settings = s
    End Sub

    Public Function BuildConnectionString(s As ConnectionSettings) As String
        Dim b As New MySqlConnectionStringBuilder()
        ' "localhost" can be tried on IPv6 first and wait before falling back; 127.0.0.1 connects at once
        b.Item("Server") = If(String.Equals(s.Server.Trim(), "localhost", StringComparison.OrdinalIgnoreCase), "127.0.0.1", s.Server)
        b.Item("Port") = s.Port.ToString()
        b.Item("Database") = s.Database
        b.Item("User Id") = s.User
        b.Item("Password") = s.Password
        b.Item("CharSet") = "utf8mb4"
        b.Item("Connection Timeout") = "5"
        b.Item("Default Command Timeout") = "30"
        Return b.ConnectionString
    End Function

    ''' <summary>Opens a connection (caller disposes it).</summary>
    Public Function OpenConnection() As MySqlConnection
        Dim conn As New MySqlConnection(BuildConnectionString(_settings))
        Try
            conn.Open()
        Catch
            conn.Dispose()
            Throw
        End Try
        Return conn
    End Function

    ''' <summary>Db.Params("@name", value, "@other", value2) -> parameter dictionary.</summary>
    Public Function Params(ParamArray keyValues As Object()) As Dictionary(Of String, Object)
        Dim d As New Dictionary(Of String, Object)()
        For i As Integer = 0 To keyValues.Length - 2 Step 2
            d(CStr(keyValues(i))) = keyValues(i + 1)
        Next
        Return d
    End Function

    Private Sub Bind(cmd As MySqlCommand, p As Dictionary(Of String, Object))
        If p Is Nothing Then Return
        For Each kv As KeyValuePair(Of String, Object) In p
            cmd.Parameters.AddWithValue(kv.Key, If(kv.Value, DBNull.Value))
        Next
    End Sub

    Public Function Scalar(sql As String, Optional p As Dictionary(Of String, Object) = Nothing) As Object
        Using conn As MySqlConnection = OpenConnection()
            Using cmd As New MySqlCommand(sql, conn)
                Bind(cmd, p)
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    Public Function NonQuery(sql As String, Optional p As Dictionary(Of String, Object) = Nothing) As Integer
        Using conn As MySqlConnection = OpenConnection()
            Using cmd As New MySqlCommand(sql, conn)
                Bind(cmd, p)
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    Public Function Table(sql As String, Optional p As Dictionary(Of String, Object) = Nothing) As DataTable
        Dim dt As New DataTable()
        Using conn As MySqlConnection = OpenConnection()
            Using cmd As New MySqlCommand(sql, conn)
                Bind(cmd, p)
                Using adapter As New MySqlDataAdapter(cmd)
                    adapter.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function

    ' ------------------------------------------------------------------ startup checks
    Public Function CheckStatus(ByRef message As String) As DbStatus
        Return CheckStatusFor(_settings, message)
    End Function

    ''' <summary>Can we reach the server, and is the database set up with the right schema version?</summary>
    Public Function CheckStatusFor(s As ConnectionSettings, ByRef message As String) As DbStatus
        message = ""
        Try
            Using conn As New MySqlConnection(BuildConnectionString(s))
                conn.Open()
                Using cmd As New MySqlCommand("SELECT COALESCE(MAX(version), 0) FROM schema_version", conn)
                    Dim v As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                    If v < RequiredSchemaVersion Then
                        message = "The database is out of date (version " & v & ", this app needs " & RequiredSchemaVersion &
                                  "). Run the newer scripts in the Database folder."
                        Return DbStatus.SchemaOutdated
                    End If
                End Using
            End Using
            Return DbStatus.Ok

        Catch ex As MySqlException
            Select Case ex.Number
                Case 1049
                    message = "Connected to the server, but the database '" & s.Database &
                              "' does not exist. Run 01_create_database.sql and 02_schema.sql."
                    Return DbStatus.SchemaMissing
                Case 1146
                    message = "The database exists but is not set up. Run 02_schema.sql in the Database folder."
                    Return DbStatus.SchemaMissing
                Case 1045
                    message = "The server refused the username or password."
                    Return DbStatus.CannotConnect
                Case 1044
                    message = "This account has no permission on the database '" & s.Database & "'."
                    Return DbStatus.CannotConnect
                Case Else
                    message = "Cannot connect to " & s.Server & ":" & s.Port & ". " & ex.Message
                    Return DbStatus.CannotConnect
            End Select

        Catch ex As Exception
            message = "Cannot connect to " & s.Server & ":" & s.Port & ". " & ex.Message
            Return DbStatus.CannotConnect
        End Try
    End Function

End Module
