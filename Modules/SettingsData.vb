Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports MySql.Data.MySqlClient

''' <summary>Percentages as text: 12 -> "12%", 7.5 -> "7.5%".</summary>
Public Module Percent

    Public Function Format(value As Decimal) As String
        Return value.ToString("0.##", CultureInfo.InvariantCulture) & "%"
    End Function

    Public Function Format(value As Object) As String
        If value Is Nothing OrElse value Is DBNull.Value Then Return "-"
        Return Format(Convert.ToDecimal(value))
    End Function

End Module

''' <summary>The one active VAT rate. Selling prices already include VAT; this is only used to show the VAT inside them.</summary>
Public Module VatRepository

    ' The Philippine VAT rate is fixed by law at 12% (0% for zero-rated sales): nothing above 12 is accepted.
    Public Const MaxVat As Decimal = 12D

    ''' <summary>id, name, percentage, updated_at of the active rate, or Nothing when none is set.</summary>
    Public Function GetActive() As DataRow
        Dim dt As DataTable = Db.Table("SELECT id, name, percentage, updated_at FROM vat_rates WHERE is_active = 1 ORDER BY id LIMIT 1")
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    ''' <summary>0 when no VAT is set.</summary>
    Public Function GetActivePercentage() As Decimal
        Dim r As DataRow = GetActive()
        If r Is Nothing Then Return 0D
        Return Convert.ToDecimal(r("percentage"))
    End Function

    Public Function SetPercentage(id As Integer, percentage As Decimal) As SaveResult
        If percentage < 0D OrElse percentage > MaxVat Then Return SaveResult.Invalid
        Dim rows As Integer = Db.NonQuery("UPDATE vat_rates SET percentage = @p WHERE id = @id", Db.Params("@p", percentage, "@id", id))
        Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
    End Function

    ''' <summary>First-time setup (or the row was removed): creates the active rate.</summary>
    Public Function CreateActive(percentage As Decimal) As SaveResult
        If percentage < 0D OrElse percentage > MaxVat Then Return SaveResult.Invalid
        Try
            Db.NonQuery("UPDATE vat_rates SET is_active = 0")
            Db.NonQuery("INSERT INTO vat_rates (name, percentage, is_active) VALUES ('VAT', @p, 1) " &
                        "ON DUPLICATE KEY UPDATE percentage = @p, is_active = 1", Db.Params("@p", percentage))
            Return SaveResult.Ok
        Catch ex As MySqlException
            Return SaveResult.Failed
        End Try
    End Function

End Module

Public Class DiscountCounts
    Public Property Total As Integer
    Public Property Active As Integer
End Class

''' <summary>Discounts the cashier can pick at the POS (only the active ones).</summary>
Public Module DiscountRepository

    ' A discount can never be 100% off: the highest allowed is 50%
    Public Const MaxPercent As Decimal = 50D

    Private Function EscapeLike(s As String) As String
        Return s.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
    End Function

    Public Function GetAll(search As String) As DataTable
        Dim sql As String =
            "SELECT d.id, d.name, d.percentage, d.status, d.created_at, " &
            "(SELECT COUNT(*) FROM sales s WHERE s.discount_id = d.id) AS used_count " &
            "FROM discounts d WHERE 1 = 1 "
        Dim prm As New Dictionary(Of String, Object)()
        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= "AND d.name LIKE @s "
            prm("@s") = "%" & EscapeLike(search.Trim()) & "%"
        End If
        sql &= "ORDER BY (d.status = 'active') DESC, d.name"
        Return Db.Table(sql, prm)
    End Function

    Public Function GetOne(id As Integer) As DataRow
        Dim dt As DataTable = Db.Table("SELECT id, name, percentage, status FROM discounts WHERE id = @id", Db.Params("@id", id))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    Public Function GetCounts() As DiscountCounts
        Dim dt As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(status = 'active'), 0) AS a FROM discounts")
        Dim c As New DiscountCounts()
        If dt.Rows.Count > 0 Then
            c.Total = Convert.ToInt32(dt.Rows(0)("n"))
            c.Active = Convert.ToInt32(dt.Rows(0)("a"))
        End If
        Return c
    End Function

    ''' <summary>Duplicate = a discount with this name already exists.</summary>
    Public Function Create(name As String, percentage As Decimal) As SaveResult
        If String.IsNullOrWhiteSpace(name) OrElse percentage <= 0D OrElse percentage > MaxPercent Then Return SaveResult.Invalid
        Try
            Db.NonQuery("INSERT INTO discounts (name, percentage, status) VALUES (@n, @p, 'active')",
                        Db.Params("@n", name.Trim(), "@p", percentage))
            Return SaveResult.Ok
        Catch ex As MySqlException When ex.Number = 1062
            Return SaveResult.Duplicate
        End Try
    End Function

    Public Function Update(id As Integer, name As String, percentage As Decimal) As SaveResult
        If String.IsNullOrWhiteSpace(name) OrElse percentage <= 0D OrElse percentage > MaxPercent Then Return SaveResult.Invalid
        Try
            Dim rows As Integer = Db.NonQuery("UPDATE discounts SET name = @n, percentage = @p WHERE id = @id",
                                              Db.Params("@n", name.Trim(), "@p", percentage, "@id", id))
            Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
        Catch ex As MySqlException When ex.Number = 1062
            Return SaveResult.Duplicate
        End Try
    End Function

    Public Function SetActive(id As Integer, active As Boolean) As SaveResult
        Dim rows As Integer = Db.NonQuery("UPDATE discounts SET status = @s WHERE id = @id",
                                          Db.Params("@s", If(active, "active", "disabled"), "@id", id))
        Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
    End Function

    ''' <summary>Past sales keep the discount's name and percentage, so deleting is safe.</summary>
    Public Function Delete(id As Integer) As SaveResult
        Dim rows As Integer = Db.NonQuery("DELETE FROM discounts WHERE id = @id", Db.Params("@id", id))
        Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
    End Function

End Module
