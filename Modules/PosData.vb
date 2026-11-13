Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports MySql.Data.MySqlClient

Public Class SaleTotals
    Public Property Subtotal As Decimal
    Public Property CostTotal As Decimal
    Public Property DiscountPercent As Decimal
    Public Property DiscountAmount As Decimal
    Public Property AfterDiscount As Decimal
    Public Property SpecialAmount As Decimal
    Public Property Total As Decimal
    Public Property VatPercent As Decimal
    Public Property VatAmount As Decimal
    Public Property VatableSales As Decimal
End Class

''' <summary>The money maths of a sale in one place (used by the screen AND by the database save).</summary>
Public Module PosMath

    Public Function Round2(x As Decimal) As Decimal
        Return Math.Round(x, 2, MidpointRounding.AwayFromZero)
    End Function

    ''' <summary>
    ''' Prices already include VAT. Order: percentage discount on the subtotal, then the special price
    ''' (an amount off), then the VAT that is INSIDE the final total.
    ''' </summary>
    Public Function Compute(subtotal As Decimal, costTotal As Decimal, discountPct As Decimal,
                            specialAmount As Decimal, vatPct As Decimal) As SaleTotals
        Dim t As New SaleTotals()
        t.Subtotal = subtotal
        t.CostTotal = costTotal
        t.DiscountPercent = discountPct
        t.DiscountAmount = Round2(subtotal * discountPct / 100D)
        t.AfterDiscount = subtotal - t.DiscountAmount

        ' the special price can never bring the total down to zero or below
        Dim maxSpecial As Decimal = Math.Max(0D, t.AfterDiscount - 0.01D)
        t.SpecialAmount = Math.Max(0D, Math.Min(specialAmount, maxSpecial))

        t.Total = t.AfterDiscount - t.SpecialAmount
        t.VatPercent = vatPct
        t.VatAmount = If(vatPct > 0D, Round2(t.Total * vatPct / (100D + vatPct)), 0D)
        t.VatableSales = t.Total - t.VatAmount
        Return t
    End Function

End Module

Public Enum SaleResult
    Ok
    OutOfStock
    PriceChanged
    Underpaid
    Invalid
    Failed
End Enum

Public Class SaleLine
    Public Property ProductId As Integer
    Public Property Qty As Integer
    Public Property ExpectedPrice As Decimal
End Class

Public Class SaleRequest
    Public Property Lines As New List(Of SaleLine)()
    Public Property DiscountId As Integer
    Public Property SpecialAmount As Decimal
    Public Property ApproverId As Integer
    Public Property AmountPaid As Decimal
    Public Property CashierId As Integer
    Public Property CashierName As String = ""
    ' loyalty: CustomerId 0 = no card. PointsMode "earn" adds points, "use" pays with PointsToUse points.
    Public Property CustomerId As Integer
    Public Property PointsMode As String = ""
    Public Property PointsToUse As Decimal
End Class

Public Class SaleOutcome
    Public Property Result As SaleResult
    Public Property Message As String = ""
    Public Property SaleId As Integer
    Public Property ReceiptNo As String = ""
    Public Property Total As Decimal
    Public Property CustomerName As String = ""
    Public Property CustomerPhone As String = ""
    Public Property PointsEarned As Decimal
    Public Property PointsUsed As Decimal
    Public Property PointsBalance As Decimal
End Class

Public Class MyStats
    Public Property Count As Integer
    Public Property Amount As Decimal
End Class

''' <summary>Everything the POS reads and writes.</summary>
Public Module PosRepository

    Private Function EscapeLike(s As String) As String
        Return s.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
    End Function

    ' ------------------------------------------------------------------ reading
    ''' <summary>Items that can be sold right now: in stock and with a price. categoryId 0 = all.</summary>
    Public Function GetSellable(search As String, categoryId As Integer) As DataTable
        Dim sql As String =
            "SELECT p.id, p.name, p.measurement, p.barcode, c.name AS category, p.qty_on_hand, p.selling_price, p.cost_price " &
            "FROM products p INNER JOIN categories c ON c.id = p.category_id " &
            "WHERE p.qty_on_hand > 0 AND p.selling_price > 0 "
        Dim prm As New Dictionary(Of String, Object)()
        If categoryId > 0 Then
            sql &= "AND p.category_id = @cat "
            prm("@cat") = categoryId
        End If
        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= "AND (p.name LIKE @s OR p.measurement LIKE @s OR p.barcode LIKE @s OR c.name LIKE @s) "
            prm("@s") = "%" & EscapeLike(search.Trim()) & "%"
        End If
        sql &= "ORDER BY p.name, p.measurement LIMIT 400"
        Return Db.Table(sql, prm)
    End Function

    ''' <summary>One item if it can be sold right now, otherwise Nothing.</summary>
    Public Function GetSellableOne(productId As Integer) As DataRow
        Dim dt As DataTable = Db.Table(
            "SELECT p.id, p.name, p.measurement, p.barcode, c.name AS category, p.qty_on_hand, p.selling_price, p.cost_price " &
            "FROM products p INNER JOIN categories c ON c.id = p.category_id " &
            "WHERE p.id = @id AND p.qty_on_hand > 0 AND p.selling_price > 0", Db.Params("@id", productId))
        If dt.Rows.Count = 0 Then Return Nothing
        Return dt.Rows(0)
    End Function

    Public Function GetActiveDiscounts() As DataTable
        Return Db.Table("SELECT id, name, percentage FROM discounts WHERE status = 'active' ORDER BY name")
    End Function

    ''' <summary>This person's sales in the last 24 hours.</summary>
    Public Function GetMyStats(userId As Integer) As MyStats
        Dim dt As DataTable = Db.Table(
            "SELECT COUNT(*) AS n, COALESCE(SUM(total), 0) AS amount FROM sales " &
            "WHERE cashier_id = @u AND created_at >= DATE_SUB(NOW(), INTERVAL 24 HOUR)", Db.Params("@u", userId))
        Dim s As New MyStats()
        If dt.Rows.Count > 0 Then
            s.Count = Convert.ToInt32(dt.Rows(0)("n"))
            s.Amount = Convert.ToDecimal(dt.Rows(0)("amount"))
        End If
        Return s
    End Function

    Public Function ShopSetting(key As String, defaultValue As String) As String
        Dim o As Object = Db.Scalar("SELECT setting_value FROM settings WHERE setting_key = @k", Db.Params("@k", key))
        If o Is Nothing OrElse o Is DBNull.Value OrElse o.ToString().Trim().Length = 0 Then Return defaultValue
        Return o.ToString().Trim()
    End Function

    ''' <summary>A saved sale, ready to show or print.</summary>
    Public Function LoadReceipt(saleId As Integer) As ReceiptModel
        Dim sale As DataTable = Db.Table(
            "SELECT s.id, s.receipt_no, s.created_at, s.cashier_name, s.subtotal, s.discount_name, s.discount_amount, " &
            "s.special_discount_amount, s.special_approved_name, s.vat_name, s.vat_percent, s.vat_amount, s.total, " &
            "s.amount_paid, s.change_amount, s.customer_name, s.points_used, s.points_value, s.points_earned, " &
            "COALESCE((SELECT l.balance_after FROM points_ledger l WHERE l.sale_id = s.id ORDER BY l.id DESC LIMIT 1), " &
            "(SELECT c.points_balance FROM customers c WHERE c.id = s.customer_id)) AS points_balance " &
            "FROM sales s WHERE s.id = @id", Db.Params("@id", saleId))
        If sale.Rows.Count = 0 Then Return Nothing

        Dim r As DataRow = sale.Rows(0)
        Dim m As New ReceiptModel()
        m.ShopName = ShopSetting("shop_name", "DARWIN TRADING")
        m.ShopAddress = ShopSetting("shop_address", "")
        m.ShopContact = ShopSetting("shop_contact", "")
        m.ShopTin = ShopSetting("shop_tin", "")
        m.ReceiptNo = r("receipt_no").ToString()
        m.SoldAt = Convert.ToDateTime(r("created_at"))
        m.CashierName = If(r("cashier_name") Is DBNull.Value, "", r("cashier_name").ToString())
        m.Subtotal = Convert.ToDecimal(r("subtotal"))
        m.DiscountName = If(r("discount_name") Is DBNull.Value, "", r("discount_name").ToString())
        m.DiscountAmount = Convert.ToDecimal(r("discount_amount"))
        m.SpecialAmount = Convert.ToDecimal(r("special_discount_amount"))
        m.ApprovedBy = If(r("special_approved_name") Is DBNull.Value, "", r("special_approved_name").ToString())
        m.VatName = If(r("vat_name") Is DBNull.Value, "VAT", r("vat_name").ToString())
        m.VatPercent = Convert.ToDecimal(r("vat_percent"))
        m.VatAmount = Convert.ToDecimal(r("vat_amount"))
        m.Total = Convert.ToDecimal(r("total"))
        m.Paid = Convert.ToDecimal(r("amount_paid"))
        m.Change = Convert.ToDecimal(r("change_amount"))
        m.CustomerName = If(r("customer_name") Is DBNull.Value, "", r("customer_name").ToString())
        m.PointsUsed = Convert.ToDecimal(r("points_used"))
        m.PointsValue = Convert.ToDecimal(r("points_value"))
        m.PointsEarned = Convert.ToDecimal(r("points_earned"))
        m.HasPointsBalance = (r("points_balance") IsNot DBNull.Value)
        m.PointsBalance = If(m.HasPointsBalance, Convert.ToDecimal(r("points_balance")), 0D)

        Dim items As DataTable = Db.Table(
            "SELECT product_name, measurement, qty, unit_price, line_total FROM sale_items WHERE sale_id = @id ORDER BY id",
            Db.Params("@id", saleId))
        For Each i As DataRow In items.Rows
            Dim l As New ReceiptLine()
            l.Name = i("product_name").ToString()
            l.Size = i("measurement").ToString()
            l.Qty = Convert.ToInt32(i("qty"))
            l.UnitPrice = Convert.ToDecimal(i("unit_price"))
            l.LineTotal = Convert.ToDecimal(i("line_total"))
            m.Lines.Add(l)
        Next
        m.VatableSales = m.Total - m.VatAmount
        Return m
    End Function

    ' ------------------------------------------------------------------ transaction helpers
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

    Private Function ReadDecimal(conn As MySqlConnection, tx As MySqlTransaction, key As String, fallback As Decimal) As Decimal
        Dim o As Object = Scalar(conn, tx, "SELECT setting_value FROM settings WHERE setting_key = @k", "@k", key)
        Dim v As Decimal
        If o IsNot Nothing AndAlso o IsNot DBNull.Value AndAlso
           Decimal.TryParse(o.ToString(), Globalization.NumberStyles.AllowDecimalPoint, Globalization.CultureInfo.InvariantCulture, v) AndAlso v > 0D Then Return v
        Return fallback
    End Function

    Private Function Reject(tx As MySqlTransaction, result As SaleResult, message As String) As SaleOutcome
        tx.Rollback()
        Dim o As New SaleOutcome()
        o.Result = result
        o.Message = message
        Return o
    End Function

    Private Class Priced
        Public ProductId As Integer
        Public Name As String = ""
        Public Size As String = ""
        Public Qty As Integer
        Public Price As Decimal
        Public Cost As Decimal
    End Class

    ' ------------------------------------------------------------------ the sale
    ''' <summary>
    ''' Saves the sale, takes the stock out (oldest delivery first) and writes the stock movements,
    ''' all in one transaction. Prices, stock, VAT and discount are read from the database again here,
    ''' so the screen can never save a wrong number.
    ''' </summary>
    Public Function CompleteSale(req As SaleRequest) As SaleOutcome
        If req.Lines.Count = 0 Then
            Dim empty As New SaleOutcome()
            empty.Result = SaleResult.Invalid
            empty.Message = "The cart is empty."
            Return empty
        End If

        Using conn As MySqlConnection = Db.OpenConnection()
            Using tx As MySqlTransaction = conn.BeginTransaction()
                Try
                    ' ---- VAT and discount as they are right now
                    Dim vatName As String = "VAT"
                    Dim vatPct As Decimal = 0D
                    Dim vat As DataTable = Rows(conn, tx, "SELECT name, percentage FROM vat_rates WHERE is_active = 1 ORDER BY id LIMIT 1")
                    If vat.Rows.Count > 0 Then
                        vatName = vat.Rows(0)("name").ToString()
                        vatPct = Convert.ToDecimal(vat.Rows(0)("percentage"))
                    End If

                    Dim discountName As String = Nothing
                    Dim discountPct As Decimal = 0D
                    If req.DiscountId > 0 Then
                        Dim d As DataTable = Rows(conn, tx, "SELECT name, percentage FROM discounts WHERE id = @i AND status = 'active'", "@i", req.DiscountId)
                        If d.Rows.Count = 0 Then Return Reject(tx, SaleResult.Invalid, "That discount is no longer active. Pick it again.")
                        discountName = d.Rows(0)("name").ToString()
                        discountPct = Convert.ToDecimal(d.Rows(0)("percentage"))
                    End If

                    ' ---- lock the items (always in id order) and price the sale from the database
                    Dim ordered As New List(Of SaleLine)(req.Lines)
                    ordered.Sort(Function(a As SaleLine, b As SaleLine) a.ProductId.CompareTo(b.ProductId))

                    Dim priced As New List(Of Priced)()
                    Dim subtotal As Decimal = 0D
                    Dim costTotal As Decimal = 0D
                    For Each line As SaleLine In ordered
                        If line.Qty <= 0 Then Return Reject(tx, SaleResult.Invalid, "A quantity in the cart is not valid.")

                        Dim p As DataTable = Rows(conn, tx,
                            "SELECT id, name, measurement, selling_price, cost_price, qty_on_hand FROM products WHERE id = @i FOR UPDATE", "@i", line.ProductId)
                        If p.Rows.Count = 0 Then Return Reject(tx, SaleResult.Invalid, "An item in the cart no longer exists.")

                        Dim row As DataRow = p.Rows(0)
                        Dim label As String = row("name").ToString() & " " & row("measurement").ToString()
                        Dim stock As Integer = Convert.ToInt32(row("qty_on_hand"))
                        If stock < line.Qty Then
                            Return Reject(tx, SaleResult.OutOfStock, label.Trim() & ": only " & stock.ToString() & " left in stock.")
                        End If

                        Dim price As Decimal = Convert.ToDecimal(row("selling_price"))
                        If price <= 0D Then Return Reject(tx, SaleResult.Invalid, label.Trim() & " has no selling price.")
                        If price <> line.ExpectedPrice Then
                            Return Reject(tx, SaleResult.PriceChanged, "The price of " & label.Trim() & " just changed to " & Currency.Format(price) & ".")
                        End If

                        Dim pl As New Priced()
                        pl.ProductId = line.ProductId
                        pl.Name = row("name").ToString()
                        pl.Size = row("measurement").ToString()
                        pl.Qty = line.Qty
                        pl.Price = price
                        pl.Cost = Convert.ToDecimal(row("cost_price"))
                        priced.Add(pl)
                        subtotal += pl.Qty * pl.Price
                        costTotal += pl.Qty * pl.Cost
                    Next

                    ' ---- totals
                    Dim discountAmount As Decimal = PosMath.Round2(subtotal * discountPct / 100D)
                    Dim afterDiscount As Decimal = subtotal - discountAmount
                    If req.SpecialAmount < 0D OrElse (req.SpecialAmount > 0D AndAlso req.SpecialAmount >= afterDiscount) Then
                        Return Reject(tx, SaleResult.Invalid, "The special price is not valid for this total.")
                    End If

                    Dim approverName As String = Nothing
                    If req.SpecialAmount > 0D Then
                        Dim a As DataTable = Rows(conn, tx, "SELECT full_name, role FROM users WHERE id = @i AND is_active = 1", "@i", req.ApproverId)
                        Dim role As String = If(a.Rows.Count = 0, "", Permissions.NormalizeRole(a.Rows(0)("role").ToString()))
                        If role <> "admin" AndAlso role <> "superadmin" Then
                            Return Reject(tx, SaleResult.Invalid, "A special price needs an Admin's or the Superadmin's approval.")
                        End If
                        approverName = a.Rows(0)("full_name").ToString()
                    End If

                    Dim total As Decimal = afterDiscount - req.SpecialAmount
                    Dim vatAmount As Decimal = If(vatPct > 0D, PosMath.Round2(total * vatPct / (100D + vatPct)), 0D)

                    ' ---- loyalty card: earn points on this sale, OR pay part / all of it with points
                    Dim custName As String = Nothing
                    Dim custPhone As String = ""
                    Dim custBalance As Decimal = 0D
                    Dim pointsUsed As Decimal = 0D
                    Dim pointsValue As Decimal = 0D
                    Dim pointsEarned As Decimal = 0D
                    If req.CustomerId > 0 Then
                        Dim c As DataTable = Rows(conn, tx, "SELECT name, contact_number, points_balance FROM customers WHERE id = @i FOR UPDATE", "@i", req.CustomerId)
                        If c.Rows.Count = 0 Then Return Reject(tx, SaleResult.Invalid, "This loyalty account no longer exists.")
                        custName = c.Rows(0)("name").ToString()
                        custPhone = If(c.Rows(0)("contact_number") Is DBNull.Value, "", c.Rows(0)("contact_number").ToString())
                        custBalance = Convert.ToDecimal(c.Rows(0)("points_balance"))

                        If req.PointsMode = "use" Then
                            ' using points turns off the discount and the special price
                            If discountPct > 0D OrElse req.SpecialAmount > 0D Then
                                Return Reject(tx, SaleResult.Invalid, "Points cannot be used together with a discount or a special price.")
                            End If
                            Dim peso As Decimal = ReadDecimal(conn, tx, "point_peso_value", 1D)
                            pointsUsed = req.PointsToUse
                            If pointsUsed <= 0D Then Return Reject(tx, SaleResult.Invalid, "Enter how many points to use.")
                            If pointsUsed > custBalance Then Return Reject(tx, SaleResult.Invalid, "The customer only has " & Loyalty.Fmt(custBalance) & " points.")
                            pointsValue = PosMath.Round2(pointsUsed * peso)
                            If pointsValue > total Then Return Reject(tx, SaleResult.Invalid, "The points are worth more than the total.")
                        ElseIf req.PointsMode = "earn" Then
                            pointsEarned = Loyalty.PointsFor(total, ReadDecimal(conn, tx, "points_spend_per_point", 200D))
                        End If
                    End If

                    ' cash only has to cover what the points do not
                    Dim cashDue As Decimal = total - pointsValue
                    If req.AmountPaid < cashDue Then
                        Return Reject(tx, SaleResult.Underpaid, "The amount received is less than the total.")
                    End If
                    Dim change As Decimal = req.AmountPaid - cashDue

                    ' ---- the sale row (receipt numbers are per year: OR-2026-000001)
                    Dim saleId As Integer = 0
                    Dim receiptNo As String = ""
                    Dim saved As Boolean = False
                    For attempt As Integer = 1 To 5
                        Dim prefix As String = "OR-" & Date.Now.Year.ToString() & "-"
                        Dim last As Object = Scalar(conn, tx,
                            "SELECT COALESCE(MAX(CAST(SUBSTRING(receipt_no, 9) AS UNSIGNED)), 0) FROM sales WHERE receipt_no LIKE @p", "@p", prefix & "%")
                        receiptNo = prefix & (Convert.ToInt32(last) + attempt).ToString("000000")
                        Try
                            Using cmd As New MySqlCommand(
                                "INSERT INTO sales (receipt_no, cashier_id, cashier_name, subtotal, discount_id, discount_name, discount_amount, " &
                                "special_discount_amount, special_approved_by, special_approved_name, vat_name, vat_percent, vat_amount, total, amount_paid, change_amount, " &
                                "customer_id, customer_name, points_used, points_value, points_earned) " &
                                "VALUES (@r, @ci, @cn, @st, @di, @dn, @da, @sa, @ab, @an, @vn, @vp, @va, @t, @ap, @ch, @cu, @cun, @pu, @pv, @pe)", conn, tx)
                                cmd.Parameters.AddWithValue("@r", receiptNo)
                                cmd.Parameters.AddWithValue("@ci", If(req.CashierId > 0, CObj(req.CashierId), DBNull.Value))
                                cmd.Parameters.AddWithValue("@cn", req.CashierName)
                                cmd.Parameters.AddWithValue("@st", subtotal)
                                cmd.Parameters.AddWithValue("@di", If(req.DiscountId > 0, CObj(req.DiscountId), DBNull.Value))
                                cmd.Parameters.AddWithValue("@dn", If(discountName, CObj(DBNull.Value)))
                                cmd.Parameters.AddWithValue("@da", discountAmount)
                                cmd.Parameters.AddWithValue("@sa", req.SpecialAmount)
                                cmd.Parameters.AddWithValue("@ab", If(req.SpecialAmount > 0D, CObj(req.ApproverId), DBNull.Value))
                                cmd.Parameters.AddWithValue("@an", If(approverName, CObj(DBNull.Value)))
                                cmd.Parameters.AddWithValue("@vn", vatName)
                                cmd.Parameters.AddWithValue("@vp", vatPct)
                                cmd.Parameters.AddWithValue("@va", vatAmount)
                                cmd.Parameters.AddWithValue("@t", total)
                                cmd.Parameters.AddWithValue("@ap", req.AmountPaid)
                                cmd.Parameters.AddWithValue("@ch", change)
                                cmd.Parameters.AddWithValue("@cu", If(req.CustomerId > 0, CObj(req.CustomerId), DBNull.Value))
                                cmd.Parameters.AddWithValue("@cun", If(custName, CObj(DBNull.Value)))
                                cmd.Parameters.AddWithValue("@pu", pointsUsed)
                                cmd.Parameters.AddWithValue("@pv", pointsValue)
                                cmd.Parameters.AddWithValue("@pe", pointsEarned)
                                cmd.ExecuteNonQuery()
                                saleId = Convert.ToInt32(cmd.LastInsertedId)
                            End Using
                            saved = True
                            Exit For
                        Catch ex As MySqlException When ex.Number = 1062
                            ' another register took this number a moment ago: try the next one
                        End Try
                    Next
                    If Not saved Then Return Reject(tx, SaleResult.Failed, "Could not number the receipt. Try again.")

                    ' ---- items, stock out, movements, oldest delivery first
                    For Each pl As Priced In priced
                        Run(conn, tx,
                            "INSERT INTO sale_items (sale_id, product_id, product_name, measurement, qty, unit_price, cost_price, line_total) " &
                            "VALUES (@s, @p, @n, @m, @q, @u, @c, @l)",
                            "@s", saleId, "@p", pl.ProductId, "@n", pl.Name, "@m", pl.Size, "@q", pl.Qty, "@u", pl.Price, "@c", pl.Cost, "@l", pl.Qty * pl.Price)

                        Dim took As Integer = Run(conn, tx,
                            "UPDATE products SET qty_on_hand = qty_on_hand - @q WHERE id = @p AND qty_on_hand >= @q", "@q", pl.Qty, "@p", pl.ProductId)
                        If took <> 1 Then Return Reject(tx, SaleResult.OutOfStock, pl.Name & " " & pl.Size & ": the stock just changed. Try again.")

                        Dim balance As Integer = Convert.ToInt32(Scalar(conn, tx, "SELECT qty_on_hand FROM products WHERE id = @p", "@p", pl.ProductId))
                        Run(conn, tx,
                            "INSERT INTO inventory_movements (product_id, movement_type, qty_change, balance_after, reference_type, reference_id, user_id) " &
                            "VALUES (@p, 'sale_out', @q, @b, 'sale', @s, @u)",
                            "@p", pl.ProductId, "@q", -pl.Qty, "@b", balance, "@s", saleId, "@u", If(req.CashierId > 0, CObj(req.CashierId), Nothing))

                        Dim remaining As Integer = pl.Qty
                        Dim batches As DataTable = Rows(conn, tx,
                            "SELECT di.id, di.qty_remaining FROM delivery_items di " &
                            "INNER JOIN deliveries d ON d.id = di.delivery_id AND d.status = 'sent' " &
                            "WHERE di.product_id = @p AND di.qty_remaining > 0 ORDER BY d.sent_at, di.id FOR UPDATE", "@p", pl.ProductId)
                        For Each b As DataRow In batches.Rows
                            If remaining <= 0 Then Exit For
                            Dim take As Integer = Math.Min(remaining, Convert.ToInt32(b("qty_remaining")))
                            Run(conn, tx, "UPDATE delivery_items SET qty_remaining = qty_remaining - @t WHERE id = @i", "@t", take, "@i", Convert.ToInt32(b("id")))
                            remaining -= take
                        Next
                    Next

                    ' ---- the customer's points (and a line in the points history)
                    Dim newBalance As Decimal = custBalance
                    If req.CustomerId > 0 Then
                        If pointsUsed > 0D Then
                            newBalance = custBalance - pointsUsed
                            Run(conn, tx, "UPDATE customers SET points_balance = @b, last_purchase_at = NOW() WHERE id = @c", "@b", newBalance, "@c", req.CustomerId)
                            Run(conn, tx, "INSERT INTO points_ledger (customer_id, sale_id, entry_type, points, balance_after, note) VALUES (@c, @s, 'redeemed', @p, @b, @n)",
                                "@c", req.CustomerId, "@s", saleId, "@p", -pointsUsed, "@b", newBalance, "@n", "Used on " & receiptNo)
                        Else
                            newBalance = custBalance + pointsEarned
                            Run(conn, tx, "UPDATE customers SET points_balance = @b, last_purchase_at = NOW() WHERE id = @c", "@b", newBalance, "@c", req.CustomerId)
                            If pointsEarned > 0D Then
                                Run(conn, tx, "INSERT INTO points_ledger (customer_id, sale_id, entry_type, points, balance_after, note) VALUES (@c, @s, 'earned', @p, @b, @n)",
                                    "@c", req.CustomerId, "@s", saleId, "@p", pointsEarned, "@b", newBalance, "@n", "Earned on " & receiptNo)
                            End If
                        End If
                    End If

                    tx.Commit()
                    Dim ok As New SaleOutcome()
                    ok.Result = SaleResult.Ok
                    ok.SaleId = saleId
                    ok.ReceiptNo = receiptNo
                    ok.Total = total
                    ok.CustomerName = If(custName, "")
                    ok.CustomerPhone = custPhone
                    ok.PointsEarned = pointsEarned
                    ok.PointsUsed = pointsUsed
                    ok.PointsBalance = newBalance
                    Return ok

                Catch ex As MySqlException
                    Try
                        tx.Rollback()
                    Catch
                        ' already rolled back
                    End Try
                    Dim bad As New SaleOutcome()
                    bad.Result = SaleResult.Failed
                    bad.Message = "The sale could not be saved: " & ex.Message
                    Return bad
                End Try
            End Using
        End Using
    End Function

End Module

''' <summary>Checks an Admin's or the Superadmin's login to approve something (it does NOT log them in).</summary>
Public Module SupervisorAuth

    Public Function Verify(username As String, password As String, ByRef approverId As Integer,
                           ByRef approverName As String, ByRef message As String) As Boolean
        approverId = 0
        approverName = ""
        message = ""
        username = If(username, "").Trim()
        If username.Length = 0 OrElse String.IsNullOrEmpty(password) Then
            message = "Enter the supervisor's username and password."
            Return False
        End If

        Dim dt As DataTable = Db.Table(
            "SELECT id, username, password_hash, full_name, role, is_active, is_locked, failed_attempts, " &
            "COALESCE(locked_until > NOW(), 0) AS temp_locked FROM users WHERE username = @u", Db.Params("@u", username))
        If dt.Rows.Count = 0 Then
            AuditLog.Write("OVERRIDE_FAILED", "POS", "Unknown supervisor username: " & username)
            message = "Invalid username or password."
            Return False
        End If

        Dim row As DataRow = dt.Rows(0)
        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim isSuper As Boolean = (Permissions.NormalizeRole(row("role").ToString()) = "superadmin")
        If Not isSuper AndAlso (Convert.ToInt32(row("temp_locked")) = 1 OrElse Convert.ToInt32(row("is_locked")) = 1) Then
            message = "This account is locked."
            Return False
        End If
        If Convert.ToInt32(row("is_active")) = 0 Then
            message = "This account is disabled."
            Return False
        End If

        If Not PasswordHasher.Verify(password, row("password_hash").ToString()) Then
            If isSuper Then
                AuditLog.Write("OVERRIDE_FAILED", "POS", "Wrong password for the Superadmin '" & username & "'")
                System.Threading.Thread.Sleep(1200)
                message = "Invalid username or password."
                Return False
            End If
            Dim previous As Integer = Convert.ToInt32(row("failed_attempts"))
            Try
                Db.NonQuery(
                    "UPDATE users SET " &
                    "locked_until = IF(failed_attempts + 1 >= " & AuthService.MaxFailedAttempts & ", DATE_ADD(NOW(), INTERVAL " & AuthService.LockMinutes & " MINUTE), locked_until), " &
                    "failed_attempts = IF(failed_attempts + 1 >= " & AuthService.MaxFailedAttempts & ", 0, failed_attempts + 1) WHERE id = @id",
                    Db.Params("@id", id))
            Catch
                ' ignore
            End Try
            AuditLog.Write("OVERRIDE_FAILED", "POS", "Wrong password while approving a special price for '" & username & "'")
            Dim attemptsLeft As Integer = AuthService.MaxFailedAttempts - (previous + 1)
            message = If(attemptsLeft <= 0, "Too many failed attempts. This account is locked for " & AuthService.LockMinutes.ToString() & " minutes.",
                         "Invalid username or password. " & attemptsLeft.ToString() & " attempt(s) left.")
            Return False
        End If

        Dim role As String = Permissions.NormalizeRole(row("role").ToString())
        If role <> "admin" AndAlso role <> "superadmin" Then
            message = "Only an Admin or the Superadmin can approve a special price."
            Return False
        End If

        Try
            Db.NonQuery("UPDATE users SET failed_attempts = 0 WHERE id = @id", Db.Params("@id", id))
        Catch
            ' ignore
        End Try
        approverId = id
        approverName = row("full_name").ToString()
        Return True
    End Function

End Module
