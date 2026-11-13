Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Text.RegularExpressions
Imports MySql.Data.MySqlClient

Public Class UserCounts
    Public Property Superadmins As Integer
    Public Property Admins As Integer
    Public Property Cashiers As Integer
    Public Property Locked As Integer
    ' People logged in right now (fresh heartbeat).
    Public Property Online As Integer
End Class

Public Enum UserSaveResult
    Ok
    UsernameTaken
    LimitReached
    NotAllowed
End Enum

''' <summary>Who may see and manage whom, and what a valid username/password is. One place for every screen.</summary>
Public Module UserRules

    Public Const MaxSuperadmins As Integer = 1
    Public Const MaxAdmins As Integer = 5
    Public Const MaxCashiers As Integer = 10

    Public Function LimitFor(role As String) As Integer
        Select Case Permissions.NormalizeRole(role)
            Case "superadmin"
                Return MaxSuperadmins
            Case "admin"
                Return MaxAdmins
            Case Else
                Return MaxCashiers
        End Select
    End Function

    ''' <summary>Admins never see the Superadmin.</summary>
    Public Function CanSee(viewerRole As String, targetRole As String) As Boolean
        If Permissions.NormalizeRole(viewerRole) = "superadmin" Then Return True
        Return Permissions.NormalizeRole(targetRole) <> "superadmin"
    End Function

    ''' <summary>Superadmin manages Admins and Cashiers; Admin manages Cashiers only; nobody manages themselves here.</summary>
    Public Function CanManage(viewerRole As String, targetRole As String, viewerId As Integer, targetId As Integer) As Boolean
        If viewerId = targetId Then Return False
        Dim target As String = Permissions.NormalizeRole(targetRole)
        Select Case Permissions.NormalizeRole(viewerRole)
            Case "superadmin"
                Return target <> "superadmin"
            Case "admin"
                Return target = "cashier"
            Case Else
                Return False
        End Select
    End Function

    Public Function CanAddUsers(viewerRole As String) As Boolean
        Dim r As String = Permissions.NormalizeRole(viewerRole)
        Return r = "superadmin" OrElse r = "admin"
    End Function

    ''' <returns>"" when fine, otherwise the message to show.</returns>
    Public Function ValidateUsername(username As String) As String
        If Not Regex.IsMatch(If(username, ""), "^[A-Za-z0-9._-]{3,30}$") Then
            Return "The username must be 3 to 30 characters: letters, numbers, dot, dash or underscore."
        End If
        Return ""
    End Function

    ''' <returns>"" when fine, otherwise the message to show.</returns>
    Public Function ValidatePassword(password As String, username As String) As String
        Dim pw As String = InputRules.PasswordProblem(password)
        If pw.Length > 0 Then Return pw
        If String.Equals(password, username, StringComparison.OrdinalIgnoreCase) Then
            Return "The password cannot be the same as the username."
        End If
        Return ""
    End Function

End Module

''' <summary>All SQL for the users table lives here.</summary>
Public Module UserRepository

    Private Function EscapeLike(s As String) As String
        Return s.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
    End Function

    ' SQL: a fresh (last 30 seconds) logged-in session for this user id column.
    Private Function OnlineSession(userIdColumn As String) As String
        Return "SELECT 1 FROM user_sessions o WHERE o.user_id = " & userIdColumn &
               " AND o.is_active = 1 AND o.last_seen >= DATE_SUB(NOW(), INTERVAL 30 SECOND)"
    End Function

    ' Who is logged in right now, as a short text. When it changes, the Users list refreshes.
    Public Function GetPresenceSignature() As String
        Dim o As Object = Db.Scalar(
            "SELECT COALESCE(GROUP_CONCAT(DISTINCT user_id ORDER BY user_id SEPARATOR ','), '') " &
            "FROM user_sessions WHERE is_active = 1 AND last_seen >= DATE_SUB(NOW(), INTERVAL 30 SECOND)")
        If o Is Nothing OrElse o Is DBNull.Value Then Return ""
        Return o.ToString()
    End Function

    Private Function NullIfBlank(s As String) As Object
        If String.IsNullOrWhiteSpace(s) Then Return Nothing
        Return s.Trim()
    End Function

    ' ------------------------------------------------------------------ reading
    Public Function GetUsers(viewerRole As String, search As String) As DataTable
        Dim sql As String =
            "SELECT u.id, u.username, u.full_name, u.contact_number, u.address, u.role, u.is_active, u.is_locked, " &
            "u.last_login, u.created_at, (u.photo IS NOT NULL) AS has_photo, " &
            "CASE WHEN u.is_locked = 1 OR (u.locked_until IS NOT NULL AND u.locked_until > NOW()) THEN 'Locked' " &
            "WHEN u.is_active = 0 THEN 'Disabled' " &
            "WHEN EXISTS (" & OnlineSession("u.id") & ") THEN 'Active' " &
            "ELSE 'Inactive' END AS status, " &
            "(SELECT s.device_name FROM user_sessions s WHERE s.user_id = u.id AND s.is_active = 1 " &
            "AND s.last_seen >= DATE_SUB(NOW(), INTERVAL 30 SECOND) ORDER BY s.last_seen DESC LIMIT 1) AS device " &
            "FROM users u WHERE 1 = 1 "

        If Permissions.NormalizeRole(viewerRole) <> "superadmin" Then
            sql &= "AND u.role <> 'superadmin' "
        End If

        Dim p As New System.Collections.Generic.Dictionary(Of String, Object)()
        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= "AND (u.username LIKE @s OR u.full_name LIKE @s OR u.contact_number LIKE @s OR u.role LIKE @s) "
            p("@s") = "%" & EscapeLike(search.Trim()) & "%"
        End If

        sql &= "ORDER BY FIELD(u.role, 'superadmin', 'admin', 'cashier'), u.full_name"
        Return Db.Table(sql, p)
    End Function

    Public Function GetCounts(viewerRole As String) As UserCounts
        Dim hideSuper As String = If(Permissions.NormalizeRole(viewerRole) = "superadmin", "", " AND u.role <> 'superadmin'")
        Dim dt As DataTable = Db.Table(
            "SELECT " &
            "COALESCE(SUM(u.role = 'superadmin'), 0) AS supers, " &
            "COALESCE(SUM(u.role = 'admin'), 0) AS admins, " &
            "COALESCE(SUM(u.role = 'cashier'), 0) AS cashiers, " &
            "COALESCE(SUM(u.is_locked = 1 OR (u.locked_until IS NOT NULL AND u.locked_until > NOW())), 0) AS locked, " &
            "COALESCE(SUM(EXISTS (" & OnlineSession("u.id") & ")), 0) AS online " &
            "FROM users u WHERE 1 = 1" & hideSuper)

        Dim c As New UserCounts()
        If dt.Rows.Count > 0 Then
            Dim r As DataRow = dt.Rows(0)
            c.Superadmins = Convert.ToInt32(r("supers"))
            c.Admins = Convert.ToInt32(r("admins"))
            c.Cashiers = Convert.ToInt32(r("cashiers"))
            c.Locked = Convert.ToInt32(r("locked"))
            c.Online = Convert.ToInt32(r("online"))
        End If
        Return c
    End Function

    ''' <summary>The user's current role straight from the database ("" when the user no longer exists).</summary>
    Public Function GetRole(userId As Integer) As String
        Dim o As Object = Db.Scalar("SELECT role FROM users WHERE id = @id", Db.Params("@id", userId))
        If o Is Nothing OrElse o Is DBNull.Value Then Return ""
        Return o.ToString()
    End Function

    ''' <summary>One row for the edit window, or Nothing.</summary>
    Public Function GetUserForEdit(userId As Integer) As DataRow
        Dim dt As DataTable = Db.Table(
            "SELECT id, username, full_name, contact_number, address, role FROM users WHERE id = @id",
            Db.Params("@id", userId))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    ''' <summary>The user's saved photo, or Nothing. The caller owns (disposes) the returned image.</summary>
    Public Function GetPhoto(userId As Integer) As Image
        Dim o As Object = Db.Scalar("SELECT photo FROM users WHERE id = @id", Db.Params("@id", userId))
        Return PhotoHelper.FromBytes(TryCast(o, Byte()))
    End Function

    ' ------------------------------------------------------------------ writing
    ''' <summary>Adds an Admin or Cashier. The limit check and the insert are one statement.</summary>
    Public Function CreateUser(fullName As String, username As String, contact As String, address As String,
                               role As String, password As String, photo As Byte()) As UserSaveResult
        Dim r As String = Permissions.NormalizeRole(role)
        If r <> "admin" AndAlso r <> "cashier" Then Return UserSaveResult.NotAllowed

        Try
            Dim rows As Integer = Db.NonQuery(
                "INSERT INTO users (username, password_hash, full_name, contact_number, address, role, photo) " &
                "SELECT @u, @p, @n, @c, @a, @r, @photo FROM DUAL " &
                "WHERE (SELECT COUNT(*) FROM users WHERE role = @r) < @max",
                Db.Params("@u", username.Trim(), "@p", PasswordHasher.Hash(password), "@n", fullName.Trim(),
                          "@c", NullIfBlank(contact), "@a", NullIfBlank(address), "@r", r,
                          "@photo", photo, "@max", UserRules.LimitFor(r)))
            Return If(rows = 1, UserSaveResult.Ok, UserSaveResult.LimitReached)
        Catch ex As MySqlException When ex.Number = 1062
            Return UserSaveResult.UsernameTaken
        End Try
    End Function

    ''' <summary>Updates details. The role may only move between Admin and Cashier, within the limits.</summary>
    Public Function UpdateUser(userId As Integer, fullName As String, username As String, contact As String, address As String,
                               role As String, photo As Byte(), changePhoto As Boolean) As UserSaveResult
        Dim r As String = Permissions.NormalizeRole(role)
        Dim current As String = GetRole(userId)
        If current.Length = 0 Then Return UserSaveResult.NotAllowed

        Dim sql As String =
            "UPDATE users SET username = @u, full_name = @n, contact_number = @c, address = @a, role = @r" &
            If(changePhoto, ", photo = @photo", "") &
            " WHERE id = @id " &
            "AND ((role = 'superadmin' AND @r = 'superadmin') OR (role <> 'superadmin' AND @r <> 'superadmin')) " &
            "AND (role = @r OR (SELECT cnt FROM (SELECT COUNT(*) AS cnt FROM users WHERE role = @r) x) < @max)"

        Try
            Dim rows As Integer = Db.NonQuery(sql,
                Db.Params("@u", username.Trim(), "@n", fullName.Trim(),
                          "@c", NullIfBlank(contact), "@a", NullIfBlank(address), "@r", r,
                          "@photo", photo, "@max", UserRules.LimitFor(r), "@id", userId))
            If rows = 1 Then Return UserSaveResult.Ok
            Return If(Permissions.NormalizeRole(current) <> r, UserSaveResult.LimitReached, UserSaveResult.NotAllowed)
        Catch ex As MySqlException When ex.Number = 1062
            Return UserSaveResult.UsernameTaken
        End Try
    End Function

    Public Function SetPassword(userId As Integer, password As String) As Boolean
        Return Db.NonQuery(
            "UPDATE users SET password_hash = @p, failed_attempts = 0, locked_until = NULL WHERE id = @id",
            Db.Params("@p", PasswordHasher.Hash(password), "@id", userId)) = 1
    End Function

    Public Function SetActive(userId As Integer, active As Boolean) As Boolean
        Return Db.NonQuery(
            "UPDATE users SET is_active = @v WHERE id = @id AND role <> 'superadmin'",
            Db.Params("@v", If(active, 1, 0), "@id", userId)) = 1
    End Function

    Public Function Unlock(userId As Integer) As Boolean
        Return Db.NonQuery(
            "UPDATE users SET is_locked = 0, locked_until = NULL, failed_attempts = 0 WHERE id = @id",
            Db.Params("@id", userId)) = 1
    End Function

    ''' <summary>The Superadmin can never be deleted from here.</summary>
    Public Function DeleteUser(userId As Integer) As Boolean
        Return Db.NonQuery(
            "DELETE FROM users WHERE id = @id AND role <> 'superadmin'",
            Db.Params("@id", userId)) = 1
    End Function

End Module

''' <summary>Turns a picked picture into a small square JPEG so the database stays light.</summary>
Public Module PhotoHelper

    Public Const AvatarSize As Integer = 256

    Public Function ToAvatarBytes(path As String, ByRef errorMessage As String) As Byte()
        errorMessage = ""
        Try
            Dim info As New FileInfo(path)
            If info.Length > 15L * 1024L * 1024L Then
                errorMessage = "That picture is too large. Choose one under 15 MB."
                Return Nothing
            End If

            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using src As Image = Image.FromStream(fs)
                    Dim side As Integer = Math.Min(src.Width, src.Height)
                    Dim crop As New Rectangle((src.Width - side) \ 2, (src.Height - side) \ 2, side, side)
                    Using bmp As New Bitmap(AvatarSize, AvatarSize)
                        Using g As Graphics = Graphics.FromImage(bmp)
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic
                            g.SmoothingMode = SmoothingMode.HighQuality
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality
                            g.Clear(Color.White)
                            g.DrawImage(src, New Rectangle(0, 0, AvatarSize, AvatarSize), crop, GraphicsUnit.Pixel)
                        End Using
                        Using ms As New MemoryStream()
                            bmp.Save(ms, ImageFormat.Jpeg)
                            Return ms.ToArray()
                        End Using
                    End Using
                End Using
            End Using
        Catch
            errorMessage = "Could not use that picture. Try a JPG or PNG file."
        End Try
        Return Nothing
    End Function

    ''' <summary>Bytes to an image the caller owns, or Nothing.</summary>
    Public Function FromBytes(bytes As Byte()) As Image
        If bytes Is Nothing OrElse bytes.Length = 0 Then Return Nothing
        Using ms As New MemoryStream(bytes)
            Using img As Image = Image.FromStream(ms)
                Return New Bitmap(img)
            End Using
        End Using
    End Function

End Module
