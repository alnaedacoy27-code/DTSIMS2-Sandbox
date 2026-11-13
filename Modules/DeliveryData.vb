Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports MySql.Data.MySqlClient

Public Enum BatchResult
    Ok
    NotFound
    ''' <summary>The batch was already sent (or changed by someone else).</summary>
    NotDraft
    ''' <summary>Nothing in the batch yet.</summary>
    Empty
    Failed
    ' delivery date in the future
    BadDate
    ' quantity or price missing / zero
    Invalid
    ' this supplier's cost price for the item is locked (see GetLastSupplierPrice)
    PriceLocked
End Enum

''' <summary>Small lookups used when receiving a delivery.</summary>
Public Module ProductLookup

    ''' <summary>The product id with exactly this barcode, or 0.</summary>
    Public Function FindByBarcode(code As String) As Integer
        If String.IsNullOrWhiteSpace(code) Then Return 0
        Dim o As Object = Db.Scalar("SELECT id FROM products WHERE barcode = @b", Db.Params("@b", code.Trim().ToUpperInvariant()))
        If o Is Nothing OrElse o Is DBNull.Value Then Return 0
        Return Convert.ToInt32(o)
    End Function

    ''' <summary>name, measurement, category, barcode, qty_on_hand, cost_price, selling_price (or Nothing).</summary>
    Public Function GetBrief(productId As Integer) As DataRow
        Dim dt As DataTable = Db.Table(
            "SELECT p.id, p.name, p.measurement, p.barcode, c.name AS category, p.qty_on_hand, p.cost_price, p.selling_price " &
            "FROM products p INNER JOIN categories c ON c.id = p.category_id WHERE p.id = @id", Db.Params("@id", productId))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

End Module

''' <summary>
''' Delivery batches. A batch is a 'draft' (editable) until it is sent to inventory ('sent', locked).
''' Sending adds the stock, updates the latest prices and writes the stock movements in ONE transaction.
''' </summary>
Public Module DeliveryRepository

    ' ------------------------------------------------------------------ small transaction helpers
    Private Function Run(conn As MySqlConnection, tx As MySqlTransaction, sql As String, ParamArray kv As Object()) As Integer
        Using cmd As New MySqlCommand(sql, conn, tx)
            For i As Integer = 0 To kv.Length - 2 Step 2
                cmd.Parameters.AddWithValue(CStr(kv(i)), If(kv(i + 1), DBNull.Value))
            Next
            Return cmd.ExecuteNonQuery()
        End Using
    End Function

    Private Function Scalar(conn As MySqlConnection, tx As MySqlTransaction, sql As String, ParamArray kv As Object()) As Object
        Using cmd As New MySqlCommand(sql, conn, tx)
            For i As Integer = 0 To kv.Length - 2 Step 2
                cmd.Parameters.AddWithValue(CStr(kv(i)), If(kv(i + 1), DBNull.Value))
            Next
            Return cmd.ExecuteScalar()
        End Using
    End Function

    Private Function Rows(conn As MySqlConnection, tx As MySqlTransaction, sql As String, ParamArray kv As Object()) As DataTable
        Dim dt As New DataTable()
        Using cmd As New MySqlCommand(sql, conn, tx)
            For i As Integer = 0 To kv.Length - 2 Step 2
                cmd.Parameters.AddWithValue(CStr(kv(i)), If(kv(i + 1), DBNull.Value))
            Next
            Using adapter As New MySqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using
        End Using
        Return dt
    End Function

    Private Sub RecalcTotals(conn As MySqlConnection, tx As MySqlTransaction, batchId As Integer)
        Run(conn, tx,
            "UPDATE deliveries SET " &
            "total_cost = (SELECT COALESCE(SUM(line_cost), 0) FROM delivery_items WHERE delivery_id = @b), " &
            "expected_profit = (SELECT COALESCE(SUM(expected_profit), 0) FROM delivery_items WHERE delivery_id = @b) " &
            "WHERE id = @b", "@b", batchId)
    End Sub

    Private Function IsDraft(conn As MySqlConnection, tx As MySqlTransaction, batchId As Integer) As Boolean
        Dim o As Object = Scalar(conn, tx, "SELECT status FROM deliveries WHERE id = @b FOR UPDATE", "@b", batchId)
        Return o IsNot Nothing AndAlso o IsNot DBNull.Value AndAlso o.ToString() = "draft"
    End Function

    ' ------------------------------------------------------------------ reading
    ''' <summary>filter: "all", "draft" or "sent". Drafts first.</summary>
    Public Function GetBatches(filter As String) As DataTable
        Dim sql As String =
            "SELECT d.id, d.batch_no, d.status, d.delivery_date, d.total_cost, d.expected_profit, " &
            "s.company_name, s.company_address, s.name AS contact, " &
            "(SELECT COUNT(*) FROM delivery_items i WHERE i.delivery_id = d.id) AS item_count " &
            "FROM deliveries d INNER JOIN suppliers s ON s.id = d.supplier_id WHERE 1 = 1 "
        If filter = "draft" Then sql &= "AND d.status = 'draft' "
        If filter = "sent" Then sql &= "AND d.status = 'sent' "
        sql &= "ORDER BY (d.status = 'draft') DESC, d.id DESC LIMIT 300"
        Return Db.Table(sql)
    End Function

    Public Function GetBatch(batchId As Integer) As DataRow
        Dim dt As DataTable = Db.Table(
            "SELECT d.id, d.batch_no, d.status, d.delivery_date, d.supplier_id, d.total_cost, d.expected_profit, d.sent_at, " &
            "s.company_name, s.company_address, s.name AS contact, " &
            "(SELECT u.full_name FROM users u WHERE u.id = d.created_by) AS created_by_name, " &
            "(SELECT u.full_name FROM users u WHERE u.id = d.sent_by) AS sent_by_name " &
            "FROM deliveries d INNER JOIN suppliers s ON s.id = d.supplier_id WHERE d.id = @id", Db.Params("@id", batchId))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    Public Function GetItems(batchId As Integer) As DataTable
        Return Db.Table(
            "SELECT i.id, i.product_id, p.name, p.measurement, p.barcode, c.name AS category, i.qty, i.qty_remaining, " &
            "i.cost_price, i.cost_mode, i.pack_size, i.entered_cost, i.selling_price, i.line_cost, i.expected_profit " &
            "FROM delivery_items i INNER JOIN products p ON p.id = i.product_id " &
            "INNER JOIN categories c ON c.id = p.category_id WHERE i.delivery_id = @b ORDER BY i.id", Db.Params("@b", batchId))
    End Function

    ''' <summary>All suppliers as "Company - address (person)" for the pickers.</summary>
    Public Function GetSupplierChoices() As DataTable
        Return Db.Table("SELECT id, company_name, company_address, name FROM suppliers ORDER BY company_name, company_address")
    End Function

    ' ------------------------------------------------------------------ batches
    Public Function CreateBatch(supplierId As Integer, deliveryDate As Date, userId As Integer, ByRef newId As Integer) As BatchResult
        newId = 0
        If deliveryDate.Date > Date.Today Then Return BatchResult.BadDate
        For attempt As Integer = 1 To 5
            Using conn As MySqlConnection = Db.OpenConnection()
                Dim prefix As String = "DEL-" & deliveryDate.Year.ToString() & "-"
                Dim last As Object = Scalar(conn, Nothing,
                    "SELECT COALESCE(MAX(CAST(SUBSTRING(batch_no, 10) AS UNSIGNED)), 0) FROM deliveries WHERE batch_no LIKE @p",
                    "@p", prefix & "%")
                Dim nextNo As Integer = Convert.ToInt32(last) + 1
                Dim batchNo As String = prefix & nextNo.ToString("0000")

                Try
                    Using cmd As New MySqlCommand(
                        "INSERT INTO deliveries (batch_no, supplier_id, status, delivery_date, created_by) VALUES (@n, @s, 'draft', @d, @u)", conn)
                        cmd.Parameters.AddWithValue("@n", batchNo)
                        cmd.Parameters.AddWithValue("@s", supplierId)
                        cmd.Parameters.AddWithValue("@d", deliveryDate.Date)
                        cmd.Parameters.AddWithValue("@u", If(userId > 0, CObj(userId), DBNull.Value))
                        cmd.ExecuteNonQuery()
                        newId = Convert.ToInt32(cmd.LastInsertedId)
                    End Using
                    Return BatchResult.Ok
                Catch ex As MySqlException When ex.Number = 1062
                    ' someone took this number a moment ago: try the next one
                Catch ex As MySqlException When ex.Number = 1452
                    Return BatchResult.NotFound
                End Try
            End Using
        Next
        Return BatchResult.Failed
    End Function

    Public Function UpdateBatch(batchId As Integer, supplierId As Integer, deliveryDate As Date) As BatchResult
        If deliveryDate.Date > Date.Today Then Return BatchResult.BadDate
        Try
            Dim rows As Integer = Db.NonQuery(
                "UPDATE deliveries SET supplier_id = @s, delivery_date = @d WHERE id = @id AND status = 'draft'",
                Db.Params("@s", supplierId, "@d", deliveryDate.Date, "@id", batchId))
            If rows = 1 Then Return BatchResult.Ok
            Return If(GetBatch(batchId) Is Nothing, BatchResult.NotFound, BatchResult.NotDraft)
        Catch ex As MySqlException When ex.Number = 1452
            Return BatchResult.NotFound
        End Try
    End Function

    Public Function DeleteBatch(batchId As Integer) As BatchResult
        Dim rows As Integer = Db.NonQuery("DELETE FROM deliveries WHERE id = @id AND status = 'draft'", Db.Params("@id", batchId))
        If rows = 1 Then Return BatchResult.Ok
        Return If(GetBatch(batchId) Is Nothing, BatchResult.NotFound, BatchResult.NotDraft)
    End Function

    ' ------------------------------------------------------------------ supplier price memory
    ' What this supplier company charged the last time it delivered this item (a batch that was sent).
    Private Const LastPriceSql As String =
        "SELECT di.cost_price, di.cost_mode, di.pack_size, di.entered_cost, di.selling_price, d.batch_no, d.delivery_date, s.company_name " &
        "FROM delivery_items di " &
        "INNER JOIN deliveries d ON d.id = di.delivery_id AND d.status = 'sent' " &
        "INNER JOIN suppliers s ON s.id = d.supplier_id " &
        "WHERE di.product_id = @p AND s.company_name = " &
        "(SELECT s2.company_name FROM deliveries d2 INNER JOIN suppliers s2 ON s2.id = d2.supplier_id WHERE d2.id = @b) " &
        "ORDER BY d.sent_at DESC, d.id DESC LIMIT 1"

    ' The last cost / selling price this batch's supplier company used for the item, or Nothing (first time).
    Public Function GetLastSupplierPrice(batchId As Integer, productId As Integer) As DataRow
        Dim dt As DataTable = Db.Table(LastPriceSql, Db.Params("@p", productId, "@b", batchId))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    ' ------------------------------------------------------------------ lines
    ''' <summary>Adds the item to the batch, or updates its line when it is already there.</summary>
    Public Function SaveItem(batchId As Integer, productId As Integer, quantity As Integer, costMode As String,
                             packSize As Integer, enteredCost As Decimal, price As Decimal,
                             costChangeConfirmed As Boolean) As BatchResult
        ' costMode "piece": quantity is pieces and enteredCost is the cost of ONE piece.
        ' costMode "pack":  quantity is packs, packSize is the pieces in one pack and enteredCost is the cost of ONE pack.
        ' The stock always counts pieces; the cost kept per piece can have up to 4 decimals.
        Dim byPack As Boolean = (costMode = "pack")
        If Not byPack Then packSize = 1
        If quantity <= 0 OrElse enteredCost <= 0D OrElse price <= 0D OrElse (byPack AndAlso packSize < 2) Then Return BatchResult.Invalid
        If CLng(quantity) * CLng(packSize) > 2000000000L Then Return BatchResult.Invalid

        Dim pieces As Integer = quantity * packSize
        Dim unitCost As Decimal = If(byPack, Math.Round(enteredCost / packSize, 4, MidpointRounding.AwayFromZero), enteredCost)
        If unitCost < 0.0001D Then Return BatchResult.Invalid
        Dim lineCost As Decimal = Math.Round(If(byPack, quantity * enteredCost, pieces * enteredCost), 2, MidpointRounding.AwayFromZero)
        Dim profit As Decimal = pieces * price - lineCost
        Dim modeText As String = If(byPack, "pack", "piece")

        Using conn As MySqlConnection = Db.OpenConnection()
            Using tx As MySqlTransaction = conn.BeginTransaction()
                Try
                    If Not IsDraft(conn, tx, batchId) Then
                        tx.Rollback()
                        Return If(GetBatch(batchId) Is Nothing, BatchResult.NotFound, BatchResult.NotDraft)
                    End If

                    ' the cost is locked to what this supplier charged last time (per piece), unless the change was confirmed
                    If Not costChangeConfirmed Then
                        Dim last As DataTable = Rows(conn, tx, LastPriceSql, "@p", productId, "@b", batchId)
                        If last.Rows.Count > 0 Then
                            If Math.Abs(unitCost - Convert.ToDecimal(last.Rows(0)("cost_price"))) > 0.00005D Then
                                tx.Rollback()
                                Return BatchResult.PriceLocked
                            End If
                        End If
                    End If

                    Dim existing As Object = Scalar(conn, tx,
                        "SELECT id FROM delivery_items WHERE delivery_id = @b AND product_id = @p LIMIT 1", "@b", batchId, "@p", productId)
                    If existing IsNot Nothing AndAlso existing IsNot DBNull.Value Then
                        Run(conn, tx,
                            "UPDATE delivery_items SET qty = @q, cost_price = @c, cost_mode = @m, pack_size = @k, entered_cost = @e, " &
                            "selling_price = @s, line_cost = @lc, expected_profit = @ep WHERE id = @id",
                            "@q", pieces, "@c", unitCost, "@m", modeText, "@k", packSize, "@e", enteredCost, "@s", price,
                            "@lc", lineCost, "@ep", profit, "@id", Convert.ToInt32(existing))
                    Else
                        Run(conn, tx,
                            "INSERT INTO delivery_items (delivery_id, product_id, qty, qty_remaining, cost_price, cost_mode, pack_size, entered_cost, " &
                            "selling_price, line_cost, expected_profit) VALUES (@b, @p, @q, 0, @c, @m, @k, @e, @s, @lc, @ep)",
                            "@b", batchId, "@p", productId, "@q", pieces, "@c", unitCost, "@m", modeText, "@k", packSize,
                            "@e", enteredCost, "@s", price, "@lc", lineCost, "@ep", profit)
                    End If

                    RecalcTotals(conn, tx, batchId)
                    tx.Commit()
                    Return BatchResult.Ok
                Catch ex As MySqlException
                    tx.Rollback()
                    Return BatchResult.Failed
                End Try
            End Using
        End Using
    End Function

    Public Function RemoveItem(batchId As Integer, itemId As Integer) As BatchResult
        Using conn As MySqlConnection = Db.OpenConnection()
            Using tx As MySqlTransaction = conn.BeginTransaction()
                Try
                    If Not IsDraft(conn, tx, batchId) Then
                        tx.Rollback()
                        Return BatchResult.NotDraft
                    End If
                    Run(conn, tx, "DELETE FROM delivery_items WHERE id = @i AND delivery_id = @b", "@i", itemId, "@b", batchId)
                    RecalcTotals(conn, tx, batchId)
                    tx.Commit()
                    Return BatchResult.Ok
                Catch ex As MySqlException
                    tx.Rollback()
                    Return BatchResult.Failed
                End Try
            End Using
        End Using
    End Function

    ' ------------------------------------------------------------------ the big one
    ''' <summary>
    ''' Puts the batch into inventory: stock goes up, the latest cost and selling price are saved on each product,
    ''' every item gets a stock-in movement, and the batch is locked. All or nothing.
    ''' </summary>
    Public Function SendToInventory(batchId As Integer, userId As Integer) As BatchResult
        Using conn As MySqlConnection = Db.OpenConnection()
            Using tx As MySqlTransaction = conn.BeginTransaction()
                Try
                    If Not IsDraft(conn, tx, batchId) Then
                        tx.Rollback()
                        Return If(GetBatch(batchId) Is Nothing, BatchResult.NotFound, BatchResult.NotDraft)
                    End If

                    Dim items As DataTable = Rows(conn, tx,
                        "SELECT id, product_id, qty, cost_price, selling_price FROM delivery_items WHERE delivery_id = @b", "@b", batchId)
                    If items.Rows.Count = 0 Then
                        tx.Rollback()
                        Return BatchResult.Empty
                    End If

                    For Each r As DataRow In items.Rows
                        Dim productId As Integer = Convert.ToInt32(r("product_id"))
                        Dim qty As Integer = Convert.ToInt32(r("qty"))

                        Run(conn, tx,
                            "UPDATE products SET qty_on_hand = qty_on_hand + @q, cost_price = @c, selling_price = @s WHERE id = @p",
                            "@q", qty, "@c", Convert.ToDecimal(r("cost_price")), "@s", Convert.ToDecimal(r("selling_price")), "@p", productId)

                        Dim balance As Integer = Convert.ToInt32(Scalar(conn, tx, "SELECT qty_on_hand FROM products WHERE id = @p", "@p", productId))

                        Run(conn, tx,
                            "INSERT INTO inventory_movements (product_id, movement_type, qty_change, balance_after, reference_type, reference_id, user_id) " &
                            "VALUES (@p, 'delivery_in', @q, @bal, 'delivery', @b, @u)",
                            "@p", productId, "@q", qty, "@bal", balance, "@b", batchId, "@u", If(userId > 0, CObj(userId), Nothing))

                        Run(conn, tx, "UPDATE delivery_items SET qty_remaining = qty WHERE id = @i", "@i", Convert.ToInt32(r("id")))
                    Next

                    RecalcTotals(conn, tx, batchId)
                    Run(conn, tx,
                        "UPDATE deliveries SET status = 'sent', sent_by = @u, sent_at = NOW() WHERE id = @b",
                        "@u", If(userId > 0, CObj(userId), Nothing), "@b", batchId)

                    tx.Commit()
                    Return BatchResult.Ok
                Catch ex As MySqlException
                    tx.Rollback()
                    Return BatchResult.Failed
                End Try
            End Using
        End Using
    End Function

End Module
