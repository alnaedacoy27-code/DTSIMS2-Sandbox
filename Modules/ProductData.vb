Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Text.RegularExpressions
Imports MySql.Data.MySqlClient

''' <summary>Result of saving a record (shared by suppliers, products and categories).</summary>
Public Enum SaveResult
    Ok
    ''' <summary>Same name / same product + size already exists.</summary>
    Duplicate
    BarcodeTaken
    ''' <summary>Cannot delete: other records use it (deliveries, sales ...).</summary>
    InUse
    NotFound
    Failed
    ' a required value is missing (name, size ...)
    Invalid
End Enum

''' <summary>Makes "2 x 2 x 3 MM" and "2X2X3mm" the same thing: 2x2x3mm.</summary>
Public Module MeasurementRules

    Public Function Normalize(text As String) As String
        Dim t As String = Regex.Replace(If(text, "").Trim(), "\s+", " ")
        ' "3 mm" -> "3mm"
        t = Regex.Replace(t, "(?<=[0-9])\s+(?=(mm|cm|m|in|ft|kg|g|pcs|pc|ga)\b)", "", RegexOptions.IgnoreCase)
        ' "3MM" -> "3mm"
        t = Regex.Replace(t, "(?<=[0-9])(mm|cm|kg|pcs|pc|ft|in|ga|m|g)\b",
                          Function(m As Match) m.Value.ToLowerInvariant(), RegexOptions.IgnoreCase)
        ' "2 X 2" -> "2x2"  and  "10mm x 6m" -> "10mmx6m"
        t = Regex.Replace(t, "(?<=[0-9.])\s*[xX*]\s*(?=[0-9])", "x")
        t = Regex.Replace(t, "(?<=[0-9](mm|cm|in|ft|m))\s*[xX*]\s*(?=[0-9])", "x")
        Return t
    End Function

End Module

''' <summary>Barcodes made by the system: EAN-13 starting with 20 (the range reserved for in-store use).</summary>
Public Module Barcodes

    Public Function CheckDigit(first12 As String) As Integer
        Dim sum As Integer = 0
        For i As Integer = 0 To 11
            Dim d As Integer = Integer.Parse(first12(i).ToString())
            sum += If(i Mod 2 = 0, d, d * 3)
        Next
        Return (10 - (sum Mod 10)) Mod 10
    End Function

    Public Function Ean13(sequence As Long) As String
        Dim first12 As String = "20" & sequence.ToString("D10")
        Return first12 & CheckDigit(first12).ToString()
    End Function

    ' Loyalty card numbers start with 21 (products start with 20), so a scan is never mixed up
    Public Function CardNumber(sequence As Long) As String
        Dim first12 As String = "21" & sequence.ToString("D10")
        Return first12 & CheckDigit(first12).ToString()
    End Function

    Public Function IsValidEan13(code As String) As Boolean
        If code Is Nothing OrElse code.Length <> 13 Then Return False
        For Each c As Char In code
            If c < "0"c OrElse c > "9"c Then Return False
        Next
        Return CheckDigit(code.Substring(0, 12)) = Integer.Parse(code(12).ToString())
    End Function

End Module

Public Class ProductCounts
    Public Property Items As Integer
    Public Property Names As Integer
    Public Property Categories As Integer
End Class

Public Module CategoryRepository

    Public Function GetAll() As DataTable
        Return Db.Table(
            "SELECT c.id, c.name, c.description, " &
            "(SELECT COUNT(*) FROM products p WHERE p.category_id = c.id) AS product_count " &
            "FROM categories c ORDER BY c.name")
    End Function

    Public Function Create(name As String, description As String) As SaveResult
        Try
            Db.NonQuery("INSERT INTO categories (name, description) VALUES (@n, @d)",
                        Db.Params("@n", name.Trim(), "@d", If(String.IsNullOrWhiteSpace(description), Nothing, description.Trim())))
            Return SaveResult.Ok
        Catch ex As MySqlException When ex.Number = 1062
            Return SaveResult.Duplicate
        End Try
    End Function

    Public Function Update(id As Integer, name As String, description As String) As SaveResult
        Try
            Dim rows As Integer = Db.NonQuery("UPDATE categories SET name = @n, description = @d WHERE id = @id",
                        Db.Params("@n", name.Trim(), "@d", If(String.IsNullOrWhiteSpace(description), Nothing, description.Trim()), "@id", id))
            Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
        Catch ex As MySqlException When ex.Number = 1062
            Return SaveResult.Duplicate
        End Try
    End Function

    Public Function Delete(id As Integer) As SaveResult
        Try
            Dim rows As Integer = Db.NonQuery("DELETE FROM categories WHERE id = @id", Db.Params("@id", id))
            Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
        Catch ex As MySqlException When ex.Number = 1451
            Return SaveResult.InUse
        End Try
    End Function

End Module

Public Module ProductRepository

    Private Function EscapeLike(s As String) As String
        Return s.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
    End Function

    ''' <summary>categoryId = 0 means all categories.</summary>
    Public Function GetProducts(search As String, categoryId As Integer) As DataTable
        Dim sql As String =
            "SELECT p.id, p.category_id, c.name AS category, p.name, p.measurement, p.barcode, " &
            "p.qty_on_hand, p.low_stock_threshold, p.created_at " &
            "FROM products p INNER JOIN categories c ON c.id = p.category_id WHERE 1 = 1 "

        Dim prm As New Dictionary(Of String, Object)()
        If categoryId > 0 Then
            sql &= "AND p.category_id = @cat "
            prm("@cat") = categoryId
        End If
        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= "AND (p.name LIKE @s OR p.measurement LIKE @s OR p.barcode LIKE @s OR c.name LIKE @s) "
            prm("@s") = "%" & EscapeLike(search.Trim()) & "%"
        End If
        sql &= "ORDER BY c.name, p.name, p.measurement"
        Return Db.Table(sql, prm)
    End Function

    Public Function GetProduct(id As Integer) As DataRow
        Dim dt As DataTable = Db.Table(
            "SELECT id, category_id, name, measurement, barcode FROM products WHERE id = @id", Db.Params("@id", id))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    Public Function GetCounts() As ProductCounts
        Dim dt As DataTable = Db.Table(
            "SELECT COUNT(*) AS items, COUNT(DISTINCT CONCAT(category_id, '|', name)) AS names, " &
            "(SELECT COUNT(*) FROM categories) AS cats FROM products")
        Dim c As New ProductCounts()
        If dt.Rows.Count > 0 Then
            c.Items = Convert.ToInt32(dt.Rows(0)("items"))
            c.Names = Convert.ToInt32(dt.Rows(0)("names"))
            c.Categories = Convert.ToInt32(dt.Rows(0)("cats"))
        End If
        Return c
    End Function

    Private Function Classify(ex As MySqlException) As SaveResult
        If ex.Number = 1062 Then
            If ex.Message.Contains("uq_products_barcode") Then Return SaveResult.BarcodeTaken
            Return SaveResult.Duplicate
        End If
        If ex.Number = 1451 Then Return SaveResult.InUse
        Return SaveResult.Failed
    End Function

    ''' <summary>Adds a product size. A blank barcode gets an automatic one (EAN-13).</summary>
    Public Function CreateProduct(categoryId As Integer, name As String, measurement As String, barcode As String) As SaveResult
        If String.IsNullOrWhiteSpace(name) OrElse MeasurementRules.Normalize(measurement).Length = 0 Then Return SaveResult.Invalid

        Dim code As String = If(barcode, "").Trim().ToUpperInvariant()
        Dim isAuto As Boolean = (code.Length = 0)
        If isAuto Then code = "TMP" & Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant()

        Using conn As MySqlConnection = Db.OpenConnection()
            Using tx As MySqlTransaction = conn.BeginTransaction()
                Try
                    Dim id As Long
                    Using cmd As New MySqlCommand(
                        "INSERT INTO products (category_id, name, measurement, barcode) VALUES (@c, @n, @m, @b)", conn, tx)
                        cmd.Parameters.AddWithValue("@c", categoryId)
                        cmd.Parameters.AddWithValue("@n", name.Trim())
                        cmd.Parameters.AddWithValue("@m", MeasurementRules.Normalize(measurement))
                        cmd.Parameters.AddWithValue("@b", code)
                        cmd.ExecuteNonQuery()
                        id = cmd.LastInsertedId
                    End Using

                    If isAuto Then
                        Using cmd2 As New MySqlCommand("UPDATE products SET barcode = @b WHERE id = @id", conn, tx)
                            cmd2.Parameters.AddWithValue("@b", Barcodes.Ean13(id))
                            cmd2.Parameters.AddWithValue("@id", id)
                            cmd2.ExecuteNonQuery()
                        End Using
                    End If

                    tx.Commit()
                    Return SaveResult.Ok
                Catch ex As MySqlException
                    tx.Rollback()
                    Return Classify(ex)
                End Try
            End Using
        End Using
    End Function

    Public Function UpdateProduct(id As Integer, categoryId As Integer, name As String, measurement As String, barcode As String) As SaveResult
        If String.IsNullOrWhiteSpace(name) OrElse MeasurementRules.Normalize(measurement).Length = 0 Then Return SaveResult.Invalid
        Try
            Dim rows As Integer = Db.NonQuery(
                "UPDATE products SET category_id = @c, name = @n, measurement = @m, barcode = @b WHERE id = @id",
                Db.Params("@c", categoryId, "@n", name.Trim(), "@m", MeasurementRules.Normalize(measurement),
                          "@b", If(barcode, "").Trim().ToUpperInvariant(), "@id", id))
            Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
        Catch ex As MySqlException
            Return Classify(ex)
        End Try
    End Function

    ''' <summary>Products that already have deliveries or sales cannot be deleted (InUse).</summary>
    Public Function DeleteProduct(id As Integer) As SaveResult
        Try
            Dim rows As Integer = Db.NonQuery("DELETE FROM products WHERE id = @id", Db.Params("@id", id))
            Return If(rows = 1, SaveResult.Ok, SaveResult.NotFound)
        Catch ex As MySqlException
            Return Classify(ex)
        End Try
    End Function

End Module
