Option Strict On
Option Explicit On

Imports System.Data
Imports MySql.Data.MySqlClient

''' <summary>First-run superadmin creation and login.</summary>
Public Module AuthService

    Public Const MaxFailedAttempts As Integer = 5
    Public Const LockMinutes As Integer = 15

    Public Function SuperadminExists() As Boolean
        Return Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM users WHERE role = 'superadmin'")) > 0
    End Function

    ''' <summary>
    ''' Creates the superadmin only if none exists yet (one atomic statement, so two laptops
    ''' setting up at the same moment cannot both succeed).
    ''' </summary>
    Public Function CreateSuperadmin(fullName As String, username As String, password As String, ByRef message As String) As Boolean
        message = ""
        Try
            Dim rows As Integer = Db.NonQuery(
                "INSERT INTO users (username, password_hash, full_name, role) " &
                "SELECT @u, @p, @n, 'superadmin' FROM DUAL " &
                "WHERE NOT EXISTS (SELECT 1 FROM users WHERE role = 'superadmin')",
                Db.Params("@u", username, "@p", PasswordHasher.Hash(password), "@n", fullName))

            If rows = 1 Then
                AuditLog.Write("CREATE", "Setup", "Superadmin account '" & username & "' was created", 0, username)
                Return True
            End If

            message = "A Superadmin account already exists."
            Return False

        Catch ex As MySqlException When ex.Number = 1062
            message = "That username is already taken."
            Return False
        Catch ex As Exception
            message = "Could not create the account: " & ex.Message
            Return False
        End Try
    End Function

    ''' <summary>Checks the login and, on success, fills AppSession.</summary>
    Public Function TryLogin(username As String, password As String, ByRef message As String) As Boolean
        message = ""
        username = If(username, "").Trim()
        If username.Length = 0 OrElse String.IsNullOrEmpty(password) Then
            message = "Enter your username and password."
            Return False
        End If

        Dim dt As DataTable
        Try
            dt = Db.Table(
                "SELECT id, username, password_hash, full_name, role, is_active, is_locked, failed_attempts, " &
                "COALESCE(locked_until > NOW(), 0) AS temp_locked, " &
                "COALESCE(TIMESTAMPDIFF(MINUTE, NOW(), locked_until), 0) AS mins_left " &
                "FROM users WHERE username = @u",
                Db.Params("@u", username))
        Catch ex As Exception
            message = "Cannot reach the database: " & ex.Message
            Return False
        End Try

        If dt.Rows.Count = 0 Then
            AuditLog.Write("LOGIN_FAILED", "Login", "Unknown username: " & username, 0, username)
            message = "Invalid username or password."
            Return False
        End If

        Dim row As DataRow = dt.Rows(0)
        Dim userId As Integer = Convert.ToInt32(row("id"))
        Dim realName As String = row("username").ToString()
        ' the Superadmin can never be locked out, however many wrong passwords are typed
        Dim isSuper As Boolean = (Permissions.NormalizeRole(row("role").ToString()) = "superadmin")

        If Not isSuper AndAlso Convert.ToInt32(row("temp_locked")) = 1 Then
            Dim mins As Integer = Convert.ToInt32(row("mins_left")) + 1
            message = "Too many failed attempts. Try again in about " & mins & " minute(s)."
            Return False
        End If
        If Not isSuper AndAlso Convert.ToInt32(row("is_locked")) = 1 Then
            message = "This account is locked. Ask an administrator to unlock it."
            Return False
        End If
        If Convert.ToInt32(row("is_active")) = 0 Then
            message = "This account is disabled."
            Return False
        End If

        If Not PasswordHasher.Verify(password, row("password_hash").ToString()) Then
            If isSuper Then
                ' never locked: a short pause only slows down guessing; every attempt is recorded
                AuditLog.Write("LOGIN_FAILED", "Login", "Wrong password for the Superadmin '" & realName & "'", userId, realName)
                System.Threading.Thread.Sleep(1200)
                message = "Invalid username or password."
                Return False
            End If
            Dim previous As Integer = Convert.ToInt32(row("failed_attempts"))
            Try
                ' locked_until is assigned first so it still sees the old failed_attempts value
                Db.NonQuery(
                    "UPDATE users SET " &
                    "locked_until = IF(failed_attempts + 1 >= " & MaxFailedAttempts & ", DATE_ADD(NOW(), INTERVAL " & LockMinutes & " MINUTE), locked_until), " &
                    "failed_attempts = IF(failed_attempts + 1 >= " & MaxFailedAttempts & ", 0, failed_attempts + 1) " &
                    "WHERE id = @id",
                    Db.Params("@id", userId))
            Catch
                ' ignore
            End Try

            AuditLog.Write("LOGIN_FAILED", "Login", "Wrong password for '" & realName & "'", userId, realName)
            Dim attemptsLeft As Integer = MaxFailedAttempts - (previous + 1)
            If attemptsLeft <= 0 Then
                message = "Too many failed attempts. This account is locked for " & LockMinutes & " minutes."
            Else
                message = "Invalid username or password. " & attemptsLeft & " attempt(s) left."
            End If
            Return False
        End If

        Try
            Db.NonQuery("UPDATE users SET failed_attempts = 0, locked_until = NULL, last_login = NOW() WHERE id = @id",
                        Db.Params("@id", userId))
        Catch
            ' ignore
        End Try

        AppSession.UserId = userId
        AppSession.Username = realName
        AppSession.FullName = row("full_name").ToString()
        AppSession.Role = row("role").ToString()
        AuditLog.Write("LOGIN", "Login", "'" & realName & "' logged in")
        Return True
    End Function

End Module
