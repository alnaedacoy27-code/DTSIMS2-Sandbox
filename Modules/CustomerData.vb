Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports MySql.Data.MySqlClient

Public Class CustomerCounts
    Public Property Total As Integer
    Public Property Points As Decimal
    Public Property NewThisMonth As Integer
End Class

''' <summary>The points rules, read from Settings (spend 200 -> 1 point, 1 point = 1 peso).</summary>
Public Module Loyalty

    Public Function SpendPerPoint() As Decimal
        Dim v As Decimal = ReadSetting("points_spend_per_point", 200D)
        Return If(v > 0D, v, 200D)
    End Function

    Public Function PesoPerPoint() As Decimal
        Dim v As Decimal = ReadSetting("point_peso_value", 1D)
        Return If(v > 0D, v, 1D)
    End Function

    Private Function ReadSetting(key As String, fallback As Decimal) As Decimal
        Try
            Dim o As Object = Db.Scalar("SELECT setting_value FROM settings WHERE setting_key = @k", Db.Params("@k", key))
            Dim v As Decimal
            If o IsNot Nothing AndAlso o IsNot DBNull.Value AndAlso
               Decimal.TryParse(o.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, v) Then Return v
        Catch
            ' use the default
        End Try
        Return fallback
    End Function

    ''' <summary>Points earned for a total: every 200 pesos is 1 point, and less than 200 earns a smaller part of a point.</summary>
    Public Function PointsFor(total As Decimal, spendPerPoint As Decimal) As Decimal
        If total <= 0D OrElse spendPerPoint <= 0D Then Return 0D
        Return PosMath.Round2(total / spendPerPoint)
    End Function

    Public Function Fmt(points As Decimal) As String
        Return points.ToString("0.##", CultureInfo.InvariantCulture)
    End Function

End Module

Public Module CustomerRepository

    Private Function EscapeLike(s As String) As String
        Return s.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
    End Function

    Private Const Cols As String =
        "c.id, c.card_number, c.name, c.contact_number, c.address, c.points_balance, c.last_purchase_at, c.created_at"

    Public Function GetAll(search As String) As DataTable
        Dim sql As String =
            "SELECT " & Cols & ", " &
            "COALESCE((SELECT SUM(l.points) FROM points_ledger l WHERE l.customer_id = c.id AND l.points > 0), 0) AS earned_total, " &
            "COALESCE(-(SELECT SUM(l.points) FROM points_ledger l WHERE l.customer_id = c.id AND l.points < 0), 0) AS used_total " &
            "FROM customers c WHERE 1 = 1 "
        Dim prm As New Dictionary(Of String, Object)()
        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= "AND (c.name LIKE @s OR c.contact_number LIKE @s OR c.card_number LIKE @s OR c.address LIKE @s) "
            prm("@s") = "%" & EscapeLike(search.Trim()) & "%"
        End If
        sql &= "ORDER BY c.name LIMIT 2000"
        Return Db.Table(sql, prm)
    End Function

    Public Function GetOne(id As Integer) As DataRow
        Dim dt As DataTable = Db.Table("SELECT " & Cols & " FROM customers c WHERE c.id = @id", Db.Params("@id", id))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    ''' <summary>The customer whose card has exactly this number, or Nothing.</summary>
    Public Function FindByCard(code As String) As DataRow
        Dim dt As DataTable = Db.Table("SELECT " & Cols & " FROM customers c WHERE c.card_number = @c", Db.Params("@c", code.Trim()))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    ''' <summary>Look-up for the POS: a few customers by name, phone or card number.</summary>
    Public Function Search(text As String) As DataTable
        Dim pattern As String = "%" & EscapeLike(text.Trim()) & "%"
        Return Db.Table(
            "SELECT " & Cols & " FROM customers c WHERE c.name LIKE @s OR c.contact_number LIKE @s OR c.card_number LIKE @s " &
            "ORDER BY c.name LIMIT 8", Db.Params("@s", pattern))
    End Function

    Public Function GetCounts() As CustomerCounts
        Dim dt As DataTable = Db.Table(
            "SELECT COUNT(*) AS n, COALESCE(SUM(points_balance), 0) AS pts, " &
            "COALESCE(SUM(created_at >= DATE_FORMAT(NOW(), '%Y-%m-01')), 0) AS fresh FROM customers")
        Dim c As New CustomerCounts()
        If dt.Rows.Count > 0 Then
            c.Total = Convert.ToInt32(dt.Rows(0)("n"))
            c.Points = Convert.ToDecimal(dt.Rows(0)("pts"))
            c.NewThisMonth = Convert.ToInt32(dt.Rows(0)("fresh"))
        End If
        Return c
    End Function

    Public Function GetLedger(customerId As Integer, count As Integer) As DataTable
        Return Db.Table(
            "SELECT entry_type, points, balance_after, note, created_at FROM points_ledger " &
            "WHERE customer_id = @c ORDER BY id DESC LIMIT " & Math.Max(1, count).ToString(CultureInfo.InvariantCulture),
            Db.Params("@c", customerId))
    End Function

    ''' <summary>Adds the customer and gives the loyalty card its number (21xxxxxxxxxxx) in one step.</summary>
    Public Function Create(name As String, contact As String, address As String, ByRef newId As Integer) As SaveResult
        newId = 0
        If String.IsNullOrWhiteSpace(name) OrElse String.IsNullOrWhiteSpace(contact) OrElse String.IsNullOrWhiteSpace(address) Then Return SaveResult.Invalid
        Try
            Using conn As MySqlConnection = Db.OpenConnection()
                Using tx As MySqlTransaction = conn.BeginTransaction()
                    Dim id As Integer
                    Using cmd As New MySqlCommand(
                        "INSERT INTO customers (card_number, name, contact_number, address) VALUES (@t, @n, @c, @a)", conn, tx)
                        cmd.Parameters.AddWithValue("@t", "NEW-" & Guid.NewGuid().ToString("N").Substring(0, 20))
                        cmd.Parameters.AddWithValue("@n", name.Trim())
                        cmd.Parameters.AddWithValue("@c", contact.Trim())
                        cmd.Parameters.AddWithValue("@a", address.Trim())
                        cmd.ExecuteNonQuery()
                        id = Convert.ToInt32(cmd.LastInsertedId)
                    End Using
                    Using cmd2 As New MySqlCommand("UPDATE customers SET card_number = @b WHERE id = @id", conn, tx)
                        cmd2.Parameters.AddWithValue("@b", Barcodes.CardNumber(id))
                        cmd2.Parameters.AddWithValue("@id", id)
                        cmd2.ExecuteNonQuery()
                    End Using
                    tx.Commit()
                    newId = id
                End Using
            End Using
            Return SaveResult.Ok
        Catch ex As MySqlException
            Return SaveResult.Failed
        End Try
    End Function

    Public Function Update(id As Integer, name As String, contact As String, address As String) As SaveResult
        If String.IsNullOrWhiteSpace(name) OrElse String.IsNullOrWhiteSpace(contact) OrElse String.IsNullOrWhiteSpace(address) Then Return SaveResult.Invalid
        Dim rows As Integer = Db.NonQuery("UPDATE customers SET name = @n, contact_number = @c, address = @a WHERE id = @id",
                                          Db.Params("@n", name.Trim(), "@c", contact.Trim(), "@a", address.Trim(), "@id", id))
        Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
    End Function

    ''' <summary>Past sales keep the customer's name; the points history goes with the account.</summary>
    Public Function Delete(id As Integer) As SaveResult
        Dim rows As Integer = Db.NonQuery("DELETE FROM customers WHERE id = @id", Db.Params("@id", id))
        Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
    End Function

End Module
