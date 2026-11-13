Option Strict On
Option Explicit On

''' <summary>Writes to audit_trail. Never throws: logging must not break the app.</summary>
Public Module AuditLog

    Public Sub Write(action As String, moduleName As String, description As String,
                     Optional userId As Integer = 0, Optional username As String = Nothing)
        Try
            Dim uid As Integer = If(userId > 0, userId, AppSession.UserId)
            Dim uname As String = If(username, AppSession.Username)

            Db.NonQuery(
                "INSERT INTO audit_trail (user_id, username, action, module, description, device_name) " &
                "VALUES (@uid, @un, @a, @m, @d, @dev)",
                Db.Params("@uid", If(uid > 0, CObj(uid), Nothing),
                          "@un", If(String.IsNullOrEmpty(uname), Nothing, uname),
                          "@a", action,
                          "@m", moduleName,
                          "@d", description,
                          "@dev", Environment.MachineName))
        Catch
            ' ignore
        End Try
    End Sub

End Module
