Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization

''' <summary>Money as text: the peso sign and two decimals.</summary>
Public Module Currency

    Public ReadOnly Peso As String = Char.ConvertFromUtf32(&H20B1)

    Public Function Format(amount As Decimal) As String
        Return Peso & amount.ToString("N2", CultureInfo.InvariantCulture)
    End Function

    Public Function Format(amount As Object) As String
        If amount Is Nothing OrElse amount Is DBNull.Value Then Return Peso & "0.00"
        Return Format(Convert.ToDecimal(amount))
    End Function

    ' A cost per piece: at least 2 decimals, up to 4 (2 / 3 of a peso is 0.6667)
    Public Function FormatCost(amount As Object) As String
        If amount Is Nothing OrElse amount Is DBNull.Value Then Return Peso & "0.00"
        Return Peso & Convert.ToDecimal(amount).ToString("#,##0.00##", CultureInfo.InvariantCulture)
    End Function

End Module

''' <summary>Low-stock warnings (the red moving line) and the threshold setting.</summary>
Public Module LowStockAlerts

    ''' <summary>One sentence per product that is at or under its minimum stock.</summary>
    Public Function GetMessages() As List(Of String)
        Dim result As New List(Of String)()
        Dim dt As DataTable = Db.Table(
            "SELECT p.name, p.measurement, p.qty_on_hand, p.low_stock_threshold " &
            "FROM products p WHERE p.low_stock_threshold > 0 AND p.qty_on_hand <= p.low_stock_threshold " &
            "ORDER BY (p.qty_on_hand = 0) DESC, p.name, p.measurement LIMIT 25")

        For Each r As DataRow In dt.Rows
            Dim label As String = r("name").ToString()
            Dim m As String = r("measurement").ToString()
            If m.Length > 0 Then label &= " " & m
            Dim qty As Integer = Convert.ToInt32(r("qty_on_hand"))
            If qty <= 0 Then
                result.Add(label & " is OUT of stock - needs restocking")
            Else
                result.Add(label & " is at its minimum stock (" & qty.ToString() & " left) - needs restocking")
            End If
        Next
        Return result
    End Function

    ''' <summary>Sets the alert level. allSizes = every size of the same product name in the same category.</summary>
    Public Function SetThreshold(productId As Integer, value As Integer, allSizes As Boolean) As Integer
        If allSizes Then
            Return Db.NonQuery(
                "UPDATE products t INNER JOIN products s ON s.category_id = t.category_id AND s.name = t.name " &
                "SET t.low_stock_threshold = @v WHERE s.id = @id",
                Db.Params("@v", value, "@id", productId))
        End If
        Return Db.NonQuery("UPDATE products SET low_stock_threshold = @v WHERE id = @id", Db.Params("@v", value, "@id", productId))
    End Function

End Module

Public Class InventoryTotals
    Public Property Items As Integer
    Public Property StockValue As Decimal
    Public Property Low As Integer
    Public Property OutOfStock As Integer
End Class

Public Module InventoryRepository

    Private Function EscapeLike(s As String) As String
        Return s.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
    End Function

    ''' <summary>Companies that have at least one delivered batch (for the supplier filter).</summary>
    Public Function GetSupplierCompanies() As DataTable
        Return Db.Table(
            "SELECT DISTINCT s.company_name FROM suppliers s " &
            "INNER JOIN deliveries d ON d.supplier_id = s.id AND d.status = 'sent' ORDER BY s.company_name")
    End Function

    Public Function GetSentBatches() As DataTable
        Return Db.Table(
            "SELECT d.id, d.batch_no, d.delivery_date, s.company_name FROM deliveries d " &
            "INNER JOIN suppliers s ON s.id = d.supplier_id WHERE d.status = 'sent' ORDER BY d.id DESC")
    End Function

    ''' <summary>
    ''' Stock per item. With a batch or a company chosen, it also returns how much of THOSE deliveries
    ''' was received and is still left.
    ''' </summary>
    Public Function GetInventory(search As String, categoryId As Integer, supplierCompany As String, batchId As Integer) As DataTable
        Dim scoped As Boolean = (batchId > 0 OrElse Not String.IsNullOrEmpty(supplierCompany))
        Dim prm As New Dictionary(Of String, Object)()
        Dim sql As String

        If scoped Then
            sql = "SELECT p.id, p.name, p.measurement, p.barcode, c.name AS category, p.qty_on_hand, p.cost_price, " &
                  "p.selling_price, p.low_stock_threshold, SUM(di.qty) AS received, SUM(di.qty_remaining) AS remaining, " &
                  "(SELECT MAX(d2.delivery_date) FROM delivery_items i2 INNER JOIN deliveries d2 ON d2.id = i2.delivery_id AND d2.status = 'sent' WHERE i2.product_id = p.id) AS last_delivery " &
                  "FROM products p INNER JOIN categories c ON c.id = p.category_id " &
                  "INNER JOIN delivery_items di ON di.product_id = p.id " &
                  "INNER JOIN deliveries d ON d.id = di.delivery_id AND d.status = 'sent' WHERE 1 = 1 "
            If batchId > 0 Then
                sql &= "AND d.id = @batch "
                prm("@batch") = batchId
            Else
                sql &= "AND d.supplier_id IN (SELECT id FROM suppliers WHERE company_name = @company) "
                prm("@company") = supplierCompany
            End If
        Else
            sql = "SELECT p.id, p.name, p.measurement, p.barcode, c.name AS category, p.qty_on_hand, p.cost_price, " &
                  "p.selling_price, p.low_stock_threshold, NULL AS received, NULL AS remaining, " &
                  "(SELECT MAX(d2.delivery_date) FROM delivery_items i2 INNER JOIN deliveries d2 ON d2.id = i2.delivery_id AND d2.status = 'sent' WHERE i2.product_id = p.id) AS last_delivery " &
                  "FROM products p INNER JOIN categories c ON c.id = p.category_id WHERE 1 = 1 "
        End If

        If categoryId > 0 Then
            sql &= "AND p.category_id = @cat "
            prm("@cat") = categoryId
        End If
        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= "AND (p.name LIKE @s OR p.measurement LIKE @s OR p.barcode LIKE @s OR c.name LIKE @s) "
            prm("@s") = "%" & EscapeLike(search.Trim()) & "%"
        End If

        If scoped Then
            sql &= "GROUP BY p.id, p.name, p.measurement, p.barcode, c.name, p.qty_on_hand, p.cost_price, p.selling_price, p.low_stock_threshold "
        End If
        sql &= "ORDER BY c.name, p.name, p.measurement"
        Return Db.Table(sql, prm)
    End Function

    ''' <summary>"In stock", "Low stock" or "Out of stock".</summary>
    Public Function StatusOf(qty As Integer, threshold As Integer) As String
        If qty <= 0 Then Return "Out of stock"
        If threshold > 0 AndAlso qty <= threshold Then Return "Low stock"
        Return "In stock"
    End Function

    Public Function Summarize(dt As DataTable) As InventoryTotals
        Dim t As New InventoryTotals()
        For Each r As DataRow In dt.Rows
            Dim qty As Integer = Convert.ToInt32(r("qty_on_hand"))
            Dim limit As Integer = Convert.ToInt32(r("low_stock_threshold"))
            t.Items += 1
            t.StockValue += qty * Convert.ToDecimal(r("cost_price"))
            Select Case StatusOf(qty, limit)
                Case "Out of stock"
                    t.OutOfStock += 1
                Case "Low stock"
                    t.Low += 1
            End Select
        Next
        Return t
    End Function

End Module
