Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Globalization

''' <summary>
''' All report queries. Every function returns a ReportDoc (figures + charts + table) for one period, so the screen,
''' the PDF, the Excel file and the printout all show exactly the same numbers. Runs on a background thread.
''' </summary>
Public Module ReportData

    Private ReadOnly Inv As CultureInfo = CultureInfo.InvariantCulture
    Private ReadOnly PaleBar As Color = Color.FromArgb(205, 165, 165)
    Private Const MaxRows As Integer = 3000

    ' ------------------------------------------------------------------ small helpers
    Private Function NewDoc(title As String, r As DateRange) As ReportDoc
        Dim d As New ReportDoc()
        d.ShopName = PosRepository.ShopSetting("shop_name", "DARWIN TRADING")
        d.Title = title
        d.PeriodLabel = r.Label
        d.GeneratedBy = AppSession.FullName
        d.GeneratedAt = DateTime.Now
        Return d
    End Function

    Private Function Dec(o As Object) As Decimal
        If o Is Nothing OrElse o Is DBNull.Value Then Return 0D
        Return Convert.ToDecimal(o)
    End Function

    Private Function Txt(o As Object) As String
        If o Is Nothing OrElse o Is DBNull.Value Then Return ""
        Return Convert.ToString(o, Inv)
    End Function

    Private Function Pct(part As Decimal, whole As Decimal) As Decimal
        If whole = 0D Then Return 0D
        Return Math.Round(part / whole * 100D, 1)
    End Function

    Private Function SizedName(name As String, size As String) As String
        Return If(String.IsNullOrWhiteSpace(size) OrElse size = "-", name, name & " " & size)
    End Function

    ''' <summary>"Sales are up 12% compared with the day before."</summary>
    Private Function CompareNote(what As String, current As Decimal, previous As Decimal, p As ReportPeriod) As String
        Dim prev As String = ReportPeriods.PreviousName(p)
        If current = 0D AndAlso previous = 0D Then Return ""
        If previous = 0D Then Return what & " had nothing " & prev & "."
        Dim change As Decimal = Math.Round((current - previous) / previous * 100D, 0)
        If change = 0D Then Return what & " are the same as " & prev & "."
        Return what & " are " & If(change > 0D, "up ", "down ") & Math.Abs(change).ToString("0", Inv) & "% compared with " & prev & "."
    End Function

    ' ---- time buckets for the trend charts: hours of one day, or the days of a week / month
    Private Class Buckets
        Public ReadOnly Labels As New List(Of String)()
        Public ReadOnly Keys As New List(Of String)()
    End Class

    Private Function TrendExpr(p As ReportPeriod, column As String) As String
        If p = ReportPeriod.Daily Then Return "HOUR(" & column & ")"
        Return "DATE_FORMAT(" & column & ", '%Y-%m-%d')"
    End Function

    Private Function HourLabel(h As Integer) As String
        Dim h12 As Integer = h Mod 12
        If h12 = 0 Then h12 = 12
        Return h12.ToString(Inv) & If(h < 12, "a", "p")
    End Function

    Private Function MakeBuckets(p As ReportPeriod, r As DateRange, dataKeys As IEnumerable(Of String)) As Buckets
        Dim b As New Buckets()
        If p = ReportPeriod.Daily Then
            Dim lo As Integer = 8
            Dim hi As Integer = 18
            For Each k As String In dataKeys
                Dim h As Integer
                If Integer.TryParse(k, h) Then
                    If h < lo Then lo = h
                    If h > hi Then hi = h
                End If
            Next
            For h As Integer = lo To hi
                b.Keys.Add(h.ToString(Inv))
                b.Labels.Add(HourLabel(h))
            Next
        Else
            Dim d As DateTime = r.From
            While d < r.ToExclusive
                b.Keys.Add(d.ToString("yyyy-MM-dd", Inv))
                b.Labels.Add(If(p = ReportPeriod.Weekly, d.ToString("ddd d", Inv), d.Day.ToString(Inv)))
                d = d.AddDays(1)
            End While
        End If
        Return b
    End Function

    Private Function ToLookup(t As DataTable, keyCol As String, valCol As String) As Dictionary(Of String, Decimal)
        Dim d As New Dictionary(Of String, Decimal)()
        For Each row As DataRow In t.Rows
            d(Txt(row(keyCol))) = Dec(row(valCol))
        Next
        Return d
    End Function

    Private Function Keys(t As DataTable, keyCol As String) As List(Of String)
        Dim l As New List(Of String)()
        For Each row As DataRow In t.Rows
            l.Add(Txt(row(keyCol)))
        Next
        Return l
    End Function

    Private Function TopChart(title As String, t As DataTable, labelCol As String, sizeCol As String, valueCol As String, isMoney As Boolean) As ReportChart
        Dim ch As New ReportChart()
        ch.Title = title
        ch.Horizontal = True
        ch.IsMoney = isMoney
        Dim s As New ChartSeries(title, Theme.Maroon)
        Dim count As Integer = 0
        For Each row As DataRow In t.Rows
            If count >= 10 Then Exit For
            ch.Labels.Add(SizedName(Txt(row(labelCol)), If(sizeCol.Length > 0, Txt(row(sizeCol)), "")))
            s.Values.Add(Dec(row(valueCol)))
            count += 1
        Next
        ch.Series.Add(s)
        Return ch
    End Function

    ' =====================================================================================
    '  SALES
    ' =====================================================================================
    Private Const SalesWhere As String = "s.created_at >= @f AND s.created_at < @t AND (@c = 0 OR s.cashier_id = @c)"

    Private Function SalesParams(r As DateRange, cashierId As Integer) As Dictionary(Of String, Object)
        Return Db.Params("@f", r.From, "@t", r.ToExclusive, "@c", cashierId)
    End Function

    Private Function SalesTotal(r As DateRange, cashierId As Integer) As Decimal
        Return Dec(Db.Scalar("SELECT COALESCE(SUM(s.total), 0) FROM sales s WHERE " & SalesWhere, SalesParams(r, cashierId)))
    End Function

    Public Function SalesSummary(p As ReportPeriod, r As DateRange, cashierId As Integer) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Sales Report", r)
        Dim prm As Dictionary(Of String, Object) = SalesParams(r, cashierId)

        Dim t As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(s.total), 0) AS sales, " &
                                      "COALESCE(SUM(s.discount_amount + s.special_discount_amount), 0) AS disc " &
                                      "FROM sales s WHERE " & SalesWhere, prm)
        Dim it As DataTable = Db.Table("SELECT COALESCE(SUM(si.qty), 0) AS units, COALESCE(SUM(si.qty * si.cost_price), 0) AS cost, " &
                                       "COALESCE(SUM(si.line_total), 0) AS rev " &
                                       "FROM sale_items si JOIN sales s ON s.id = si.sale_id WHERE " & SalesWhere, prm)
        Dim n As Decimal = Dec(t.Rows(0)("n"))
        Dim sales As Decimal = Dec(t.Rows(0)("sales"))
        Dim disc As Decimal = Dec(t.Rows(0)("disc"))
        Dim units As Decimal = Dec(it.Rows(0)("units"))
        Dim cost As Decimal = Dec(it.Rows(0)("cost"))
        Dim rev As Decimal = Dec(it.Rows(0)("rev"))

        doc.Kpis.Add(New ReportKpi("Total sales", ReportFormat.Peso(sales)))
        doc.Kpis.Add(New ReportKpi("Transactions", ReportFormat.Whole(n)))
        doc.Kpis.Add(New ReportKpi("Items sold", ReportFormat.Whole(units)))
        doc.Kpis.Add(New ReportKpi("Average sale", ReportFormat.Peso(If(n > 0D, Math.Round(sales / n, 2), 0D))))
        doc.Kpis.Add(New ReportKpi("Profit (sales - cost)", ReportFormat.Peso(sales - cost)))
        doc.Kpis.Add(New ReportKpi("Discounts given", ReportFormat.Peso(disc)))

        Dim prevRange As DateRange = ReportPeriods.RangeFor(p, ReportPeriods.Shift(p, r.From, -1))
        doc.Note = CompareNote("Sales", sales, SalesTotal(prevRange, cashierId), p)
        If cashierId > 0 Then doc.Note = ("Your sales only. " & doc.Note).Trim()

        ' trend chart
        Dim tr As DataTable = Db.Table("SELECT " & TrendExpr(p, "s.created_at") & " AS k, SUM(s.total) AS v FROM sales s WHERE " & SalesWhere & " GROUP BY 1", prm)
        Dim lookup As Dictionary(Of String, Decimal) = ToLookup(tr, "k", "v")
        Dim b As Buckets = MakeBuckets(p, r, Keys(tr, "k"))
        Dim trend As New ReportChart()
        trend.Title = If(p = ReportPeriod.Daily, "Sales per hour", "Sales per day")
        trend.IsMoney = True
        Dim ts As New ChartSeries("Sales", Theme.Maroon)
        For i As Integer = 0 To b.Keys.Count - 1
            trend.Labels.Add(b.Labels(i))
            Dim v As Decimal = 0D
            lookup.TryGetValue(b.Keys(i), v)
            ts.Values.Add(v)
        Next
        trend.Series.Add(ts)
        doc.Charts.Add(trend)

        ' top 10 products
        Dim top As DataTable = Db.Table("SELECT si.product_id, MAX(si.product_name) AS product, MAX(si.measurement) AS size, " &
                                        "SUM(si.qty) AS qty, SUM(si.line_total) AS sales, SUM(si.line_total - si.qty * si.cost_price) AS profit " &
                                        "FROM sale_items si JOIN sales s ON s.id = si.sale_id WHERE " & SalesWhere &
                                        " GROUP BY si.product_id ORDER BY sales DESC, qty DESC LIMIT 10", prm)
        top.Columns.Add("rank", GetType(Integer))
        top.Columns.Add("share", GetType(Decimal))
        For i As Integer = 0 To top.Rows.Count - 1
            top.Rows(i)("rank") = i + 1
            top.Rows(i)("share") = Pct(Dec(top.Rows(i)("sales")), rev)
        Next
        doc.Charts.Add(TopChart("Top 10 products by sales", top, "product", "size", "sales", True))

        doc.TableTitle = "Top 10 products"
        doc.Columns.Add(New ReportColumn("#", "rank", ColKind.Number, 4, 50))
        doc.Columns.Add(New ReportColumn("PRODUCT", "product", ColKind.Text, 26, 180))
        doc.Columns.Add(New ReportColumn("SIZE", "size", ColKind.Text, 12, 90))
        doc.Columns.Add(New ReportColumn("UNITS SOLD", "qty", ColKind.Number, 11, 100))
        doc.Columns.Add(New ReportColumn("SALES", "sales", ColKind.Money, 13, 120))
        doc.Columns.Add(New ReportColumn("PROFIT", "profit", ColKind.Money, 13, 110))
        doc.Columns.Add(New ReportColumn("SHARE", "share", ColKind.Percent, 9, 80))
        doc.Data = top
        doc.EmptyText = "No sales were made in this period."
        Return doc
    End Function

    Public Function SalesTransactions(p As ReportPeriod, r As DateRange, cashierId As Integer) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Sales Report - Transactions", r)
        Dim prm As Dictionary(Of String, Object) = SalesParams(r, cashierId)
        If cashierId > 0 Then doc.Note = "Your sales only"

        Dim agg As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(s.total), 0) AS sales, COALESCE(MAX(s.total), 0) AS biggest, " &
                                        "COALESCE(SUM(s.discount_amount + s.special_discount_amount), 0) AS disc FROM sales s WHERE " & SalesWhere, prm)
        Dim n As Decimal = Dec(agg.Rows(0)("n"))
        Dim sales As Decimal = Dec(agg.Rows(0)("sales"))
        doc.Kpis.Add(New ReportKpi("Transactions", ReportFormat.Whole(n)))
        doc.Kpis.Add(New ReportKpi("Total sales", ReportFormat.Peso(sales)))
        doc.Kpis.Add(New ReportKpi("Average sale", ReportFormat.Peso(If(n > 0D, Math.Round(sales / n, 2), 0D))))
        doc.Kpis.Add(New ReportKpi("Largest sale", ReportFormat.Peso(Dec(agg.Rows(0)("biggest")))))
        doc.Kpis.Add(New ReportKpi("Discounts given", ReportFormat.Peso(Dec(agg.Rows(0)("disc")))))

        Dim dt As DataTable = Db.Table("SELECT s.id, s.receipt_no, s.created_at, COALESCE(s.cashier_name, '') AS cashier, COALESCE(s.customer_name, '') AS customer, " &
                                       "(SELECT COALESCE(SUM(si.qty), 0) FROM sale_items si WHERE si.sale_id = s.id) AS items, " &
                                       "(s.discount_amount + s.special_discount_amount) AS discount, s.total, s.amount_paid " &
                                       "FROM sales s WHERE " & SalesWhere & " ORDER BY s.created_at DESC, s.id DESC LIMIT " & (MaxRows + 1), prm)
        If dt.Rows.Count > MaxRows Then
            dt.Rows.RemoveAt(dt.Rows.Count - 1)
            doc.TruncatedNote = "Showing the latest " & MaxRows.ToString("N0", Inv) & " sales of this period. Choose a shorter period to see the rest."
        End If

        doc.ScreenHint = "Select a sale, then choose View receipt or Print copy (or double-click it)."
        doc.TableTitle = "All sales (double-click a row to see the receipt)"
        doc.Columns.Add(New ReportColumn("RECEIPT NO", "receipt_no", ColKind.Text, 14, 130))
        doc.Columns.Add(New ReportColumn("DATE & TIME", "created_at", ColKind.Stamp, 17, 170))
        doc.Columns.Add(New ReportColumn("CASHIER", "cashier", ColKind.Text, 14, 120))
        doc.Columns.Add(New ReportColumn("CUSTOMER", "customer", ColKind.Text, 15, 130))
        doc.Columns.Add(New ReportColumn("ITEMS", "items", ColKind.Number, 7, 70))
        doc.Columns.Add(New ReportColumn("DISCOUNT", "discount", ColKind.Money, 10, 100))
        doc.Columns.Add(New ReportColumn("TOTAL", "total", ColKind.Money, 11, 110))
        doc.Columns.Add(New ReportColumn("PAID", "amount_paid", ColKind.Money, 11, 110))
        doc.Data = dt
        doc.EmptyText = "No sales were made in this period."
        Return doc
    End Function

    Public Function SalesCategories(p As ReportPeriod, r As DateRange, cashierId As Integer) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Sales Report - Categories", r)
        Dim prm As Dictionary(Of String, Object) = SalesParams(r, cashierId)
        If cashierId > 0 Then doc.Note = "Your sales only"

        Dim dt As DataTable = Db.Table("SELECT COALESCE(c.name, 'Uncategorized') AS category, SUM(si.qty) AS qty, SUM(si.line_total) AS sales, " &
                                       "SUM(si.line_total - si.qty * si.cost_price) AS profit " &
                                       "FROM sale_items si JOIN sales s ON s.id = si.sale_id " &
                                       "LEFT JOIN products p ON p.id = si.product_id LEFT JOIN categories c ON c.id = p.category_id " &
                                       "WHERE " & SalesWhere & " GROUP BY c.id, c.name ORDER BY sales DESC", prm)
        dt.Columns.Add("share", GetType(Decimal))
        Dim total As Decimal = 0D
        Dim units As Decimal = 0D
        For Each row As DataRow In dt.Rows
            total += Dec(row("sales"))
            units += Dec(row("qty"))
        Next
        For Each row As DataRow In dt.Rows
            row("share") = Pct(Dec(row("sales")), total)
        Next

        doc.Kpis.Add(New ReportKpi("Sales in these categories", ReportFormat.Peso(total)))
        doc.Kpis.Add(New ReportKpi("Categories sold", ReportFormat.Whole(dt.Rows.Count)))
        doc.Kpis.Add(New ReportKpi("Units sold", ReportFormat.Whole(units)))
        If dt.Rows.Count > 0 Then
            doc.Kpis.Add(New ReportKpi("Best category", Txt(dt.Rows(0)("category"))))
            doc.Kpis.Add(New ReportKpi("Best category share", Dec(dt.Rows(0)("share")).ToString("0.0", Inv) & "%"))
        End If

        doc.Charts.Add(TopChart("Sales by category", dt, "category", "", "sales", True))
        doc.TableTitle = "Sales by category"
        doc.Columns.Add(New ReportColumn("CATEGORY", "category", ColKind.Text, 30, 180))
        doc.Columns.Add(New ReportColumn("UNITS SOLD", "qty", ColKind.Number, 14, 100))
        doc.Columns.Add(New ReportColumn("SALES", "sales", ColKind.Money, 18, 120))
        doc.Columns.Add(New ReportColumn("PROFIT", "profit", ColKind.Money, 18, 120))
        doc.Columns.Add(New ReportColumn("SHARE", "share", ColKind.Percent, 12, 80))
        doc.Data = dt
        doc.EmptyText = "No sales were made in this period."
        Return doc
    End Function

    ' =====================================================================================
    '  INVENTORY (stock movements)
    ' =====================================================================================
    Private Const MoveWhere As String = "m.created_at >= @f AND m.created_at < @t"

    Private Function MoveParams(r As DateRange) As Dictionary(Of String, Object)
        Return Db.Params("@f", r.From, "@t", r.ToExclusive)
    End Function

    Private Function MoveTypeText(t As String) As String
        Select Case t
            Case "delivery_in"
                Return "Delivery received"
            Case "sale_out"
                Return "Sale"
            Case "loss_out"
                Return "Loss"
            Case "adjustment"
                Return "Adjustment"
            Case Else
                Return t
        End Select
    End Function

    Private Sub AddMovementKpis(doc As ReportDoc, r As DateRange)
        Dim t As DataTable = Db.Table("SELECT m.movement_type AS t, COALESCE(SUM(m.qty_change), 0) AS net FROM inventory_movements m WHERE " & MoveWhere & " GROUP BY m.movement_type", MoveParams(r))
        Dim received As Decimal = 0D, sold As Decimal = 0D, lost As Decimal = 0D, adj As Decimal = 0D
        For Each row As DataRow In t.Rows
            Select Case Txt(row("t"))
                Case "delivery_in"
                    received = Dec(row("net"))
                Case "sale_out"
                    sold = -Dec(row("net"))
                Case "loss_out"
                    lost = -Dec(row("net"))
                Case "adjustment"
                    adj = Dec(row("net"))
            End Select
        Next
        Dim moved As Decimal = Dec(Db.Scalar("SELECT COUNT(DISTINCT m.product_id) FROM inventory_movements m WHERE " & MoveWhere, MoveParams(r)))
        doc.Kpis.Add(New ReportKpi("Stock received", ReportFormat.Whole(received)))
        doc.Kpis.Add(New ReportKpi("Units sold", ReportFormat.Whole(sold)))
        doc.Kpis.Add(New ReportKpi("Lost / written off", ReportFormat.Whole(lost), lost > 0D))
        doc.Kpis.Add(New ReportKpi("Adjustments (net)", ReportFormat.Whole(adj)))
        doc.Kpis.Add(New ReportKpi("Net stock change", ReportFormat.Whole(received - sold - lost + adj)))
        doc.Kpis.Add(New ReportKpi("Products moved", ReportFormat.Whole(moved)))
    End Sub

    Public Function InventorySummary(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Inventory Report", r)
        Dim prm As Dictionary(Of String, Object) = MoveParams(r)
        AddMovementKpis(doc, r)

        ' stock in vs stock out, per hour / per day
        Dim tr As DataTable = Db.Table("SELECT " & TrendExpr(p, "m.created_at") & " AS k, " &
                                       "SUM(CASE WHEN m.qty_change > 0 THEN m.qty_change ELSE 0 END) AS i, " &
                                       "SUM(CASE WHEN m.qty_change < 0 THEN -m.qty_change ELSE 0 END) AS o " &
                                       "FROM inventory_movements m WHERE " & MoveWhere & " GROUP BY 1", prm)
        Dim lin As Dictionary(Of String, Decimal) = ToLookup(tr, "k", "i")
        Dim lout As Dictionary(Of String, Decimal) = ToLookup(tr, "k", "o")
        Dim b As Buckets = MakeBuckets(p, r, Keys(tr, "k"))
        Dim ch As New ReportChart()
        ch.Title = If(p = ReportPeriod.Daily, "Stock in and out per hour (units)", "Stock in and out per day (units)")
        Dim sIn As New ChartSeries("Stock in", PaleBar)
        Dim sOut As New ChartSeries("Stock out", Theme.Maroon)
        For i As Integer = 0 To b.Keys.Count - 1
            ch.Labels.Add(b.Labels(i))
            Dim a As Decimal = 0D
            Dim c As Decimal = 0D
            lin.TryGetValue(b.Keys(i), a)
            lout.TryGetValue(b.Keys(i), c)
            sIn.Values.Add(a)
            sOut.Values.Add(c)
        Next
        ch.Series.Add(sIn)
        ch.Series.Add(sOut)
        doc.Charts.Add(ch)

        Dim dt As DataTable = Db.Table("SELECT p.id, p.name AS product, p.measurement AS size, " &
                                       "SUM(CASE WHEN m.qty_change > 0 THEN m.qty_change ELSE 0 END) AS qty_in, " &
                                       "SUM(CASE WHEN m.qty_change < 0 THEN -m.qty_change ELSE 0 END) AS qty_out, " &
                                       "SUM(m.qty_change) AS net, p.qty_on_hand AS on_hand " &
                                       "FROM inventory_movements m JOIN products p ON p.id = m.product_id WHERE " & MoveWhere &
                                       " GROUP BY p.id, p.name, p.measurement, p.qty_on_hand ORDER BY qty_out DESC, qty_in DESC LIMIT 500", prm)
        Dim topOut As New ReportChart()
        topOut.Title = "Top 10 products moved out (units)"
        topOut.Horizontal = True
        Dim so As New ChartSeries("Units out", Theme.Maroon)
        Dim shown As Integer = 0
        For Each row As DataRow In dt.Rows
            If shown >= 10 OrElse Dec(row("qty_out")) <= 0D Then Exit For
            topOut.Labels.Add(SizedName(Txt(row("product")), Txt(row("size"))))
            so.Values.Add(Dec(row("qty_out")))
            shown += 1
        Next
        topOut.Series.Add(so)
        doc.Charts.Add(topOut)

        doc.TableTitle = "Stock movement by product"
        doc.Columns.Add(New ReportColumn("PRODUCT", "product", ColKind.Text, 28, 190))
        doc.Columns.Add(New ReportColumn("SIZE", "size", ColKind.Text, 12, 90))
        doc.Columns.Add(New ReportColumn("STOCK IN", "qty_in", ColKind.Number, 11, 90))
        doc.Columns.Add(New ReportColumn("STOCK OUT", "qty_out", ColKind.Number, 11, 90))
        doc.Columns.Add(New ReportColumn("NET CHANGE", "net", ColKind.Number, 11, 100))
        doc.Columns.Add(New ReportColumn("ON HAND NOW", "on_hand", ColKind.Number, 12, 110))
        doc.Data = dt
        doc.EmptyText = "No stock moved in this period."
        Return doc
    End Function

    Public Function InventoryMovements(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Inventory Report - Movements", r)
        AddMovementKpis(doc, r)

        Dim dt As DataTable = Db.Table("SELECT m.id, m.created_at, p.name AS product, p.measurement AS size, m.movement_type AS mtype, " &
                                       "m.qty_change, m.balance_after, " &
                                       "CASE WHEN m.reference_type = 'sale' THEN s.receipt_no WHEN m.reference_type = 'delivery' THEN d.batch_no ELSE '' END AS ref, " &
                                       "COALESCE(u.full_name, '') AS by_user " &
                                       "FROM inventory_movements m JOIN products p ON p.id = m.product_id " &
                                       "LEFT JOIN sales s ON m.reference_type = 'sale' AND s.id = m.reference_id " &
                                       "LEFT JOIN deliveries d ON m.reference_type = 'delivery' AND d.id = m.reference_id " &
                                       "LEFT JOIN users u ON u.id = m.user_id " &
                                       "WHERE " & MoveWhere & " ORDER BY m.created_at DESC, m.id DESC LIMIT " & (MaxRows + 1), MoveParams(r))
        If dt.Rows.Count > MaxRows Then
            dt.Rows.RemoveAt(dt.Rows.Count - 1)
            doc.TruncatedNote = "Showing the latest " & MaxRows.ToString("N0", Inv) & " movements of this period. Choose a shorter period to see the rest."
        End If
        dt.Columns.Add("type", GetType(String))
        For Each row As DataRow In dt.Rows
            row("type") = MoveTypeText(Txt(row("mtype")))
        Next

        doc.TableTitle = "Every stock in and stock out"
        doc.Columns.Add(New ReportColumn("DATE & TIME", "created_at", ColKind.Stamp, 16, 170))
        doc.Columns.Add(New ReportColumn("PRODUCT", "product", ColKind.Text, 22, 170))
        doc.Columns.Add(New ReportColumn("SIZE", "size", ColKind.Text, 10, 80))
        doc.Columns.Add(New ReportColumn("TYPE", "type", ColKind.Text, 14, 130))
        doc.Columns.Add(New ReportColumn("CHANGE", "qty_change", ColKind.Number, 8, 80))
        doc.Columns.Add(New ReportColumn("BALANCE AFTER", "balance_after", ColKind.Number, 11, 110))
        doc.Columns.Add(New ReportColumn("REFERENCE", "ref", ColKind.Text, 13, 120))
        doc.Columns.Add(New ReportColumn("BY", "by_user", ColKind.Text, 13, 120))
        doc.Data = dt
        doc.EmptyText = "No stock moved in this period."
        Return doc
    End Function

    ' =====================================================================================
    '  DELIVERIES
    ' =====================================================================================
    Private Const DelWhere As String = "d.delivery_date >= @f AND d.delivery_date < @t"

    Private Function DelParams(r As DateRange) As Dictionary(Of String, Object)
        Return Db.Params("@f", r.From.Date, "@t", r.ToExclusive.Date)
    End Function

    Private Sub AddDeliveryKpis(doc As ReportDoc, r As DateRange)
        Dim prm As Dictionary(Of String, Object) = DelParams(r)
        Dim t As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(d.total_cost), 0) AS cost, COALESCE(SUM(d.expected_profit), 0) AS prof, " &
                                      "COUNT(DISTINCT d.supplier_id) AS sup FROM deliveries d WHERE d.status = 'sent' AND " & DelWhere, prm)
        Dim units As Decimal = Dec(Db.Scalar("SELECT COALESCE(SUM(di.qty), 0) FROM delivery_items di JOIN deliveries d ON d.id = di.delivery_id " &
                                             "WHERE d.status = 'sent' AND " & DelWhere, prm))
        Dim drafts As Decimal = Dec(Db.Scalar("SELECT COUNT(*) FROM deliveries WHERE status = 'draft'"))
        doc.Kpis.Add(New ReportKpi("Deliveries received", ReportFormat.Whole(Dec(t.Rows(0)("n")))))
        doc.Kpis.Add(New ReportKpi("Units received", ReportFormat.Whole(units)))
        doc.Kpis.Add(New ReportKpi("Total cost", ReportFormat.Peso(Dec(t.Rows(0)("cost")))))
        doc.Kpis.Add(New ReportKpi("Expected profit", ReportFormat.Peso(Dec(t.Rows(0)("prof")))))
        doc.Kpis.Add(New ReportKpi("Suppliers", ReportFormat.Whole(Dec(t.Rows(0)("sup")))))
        doc.Kpis.Add(New ReportKpi("Drafts not yet sent", ReportFormat.Whole(drafts), drafts > 0D))
    End Sub

    Public Function DeliverySummary(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Delivery Report", r)
        Dim prm As Dictionary(Of String, Object) = DelParams(r)
        AddDeliveryKpis(doc, r)

        Dim dt As DataTable = Db.Table("SELECT sp.id, sp.company_name AS supplier, COUNT(*) AS deliveries, COALESCE(SUM(x.q), 0) AS units, " &
                                       "COALESCE(SUM(d.total_cost), 0) AS cost, COALESCE(SUM(d.expected_profit), 0) AS profit " &
                                       "FROM deliveries d JOIN suppliers sp ON sp.id = d.supplier_id " &
                                       "LEFT JOIN (SELECT delivery_id, SUM(qty) AS q FROM delivery_items GROUP BY delivery_id) x ON x.delivery_id = d.id " &
                                       "WHERE d.status = 'sent' AND " & DelWhere & " GROUP BY sp.id, sp.company_name ORDER BY cost DESC", prm)
        dt.Columns.Add("share", GetType(Decimal))
        Dim total As Decimal = 0D
        For Each row As DataRow In dt.Rows
            total += Dec(row("cost"))
        Next
        For Each row As DataRow In dt.Rows
            row("share") = Pct(Dec(row("cost")), total)
        Next

        Dim bySupplier As ReportChart = TopChart("Cost by supplier", dt, "supplier", "", "cost", True)
        doc.Charts.Add(bySupplier)

        If p <> ReportPeriod.Daily Then
            Dim tr As DataTable = Db.Table("SELECT DATE_FORMAT(d.delivery_date, '%Y-%m-%d') AS k, SUM(d.total_cost) AS v FROM deliveries d " &
                                           "WHERE d.status = 'sent' AND " & DelWhere & " GROUP BY 1", prm)
            Dim lookup As Dictionary(Of String, Decimal) = ToLookup(tr, "k", "v")
            Dim b As Buckets = MakeBuckets(p, r, Keys(tr, "k"))
            Dim trend As New ReportChart()
            trend.Title = "Delivery cost per day"
            trend.IsMoney = True
            Dim s As New ChartSeries("Cost", Theme.Maroon)
            For i As Integer = 0 To b.Keys.Count - 1
                trend.Labels.Add(b.Labels(i))
                Dim v As Decimal = 0D
                lookup.TryGetValue(b.Keys(i), v)
                s.Values.Add(v)
            Next
            trend.Series.Add(s)
            doc.Charts.Add(trend)
        End If

        doc.TableTitle = "Deliveries by supplier"
        doc.Columns.Add(New ReportColumn("SUPPLIER", "supplier", ColKind.Text, 30, 200))
        doc.Columns.Add(New ReportColumn("DELIVERIES", "deliveries", ColKind.Number, 11, 100))
        doc.Columns.Add(New ReportColumn("UNITS", "units", ColKind.Number, 11, 90))
        doc.Columns.Add(New ReportColumn("TOTAL COST", "cost", ColKind.Money, 15, 120))
        doc.Columns.Add(New ReportColumn("EXPECTED PROFIT", "profit", ColKind.Money, 15, 130))
        doc.Columns.Add(New ReportColumn("SHARE OF COST", "share", ColKind.Percent, 12, 110))
        doc.Data = dt
        doc.EmptyText = "No deliveries were received in this period."
        Return doc
    End Function

    Public Function DeliveryList(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Delivery Report - Deliveries", r)
        AddDeliveryKpis(doc, r)

        Dim dt As DataTable = Db.Table("SELECT d.id, d.batch_no, sp.company_name AS supplier, d.delivery_date, d.status, " &
                                       "(SELECT COUNT(*) FROM delivery_items di WHERE di.delivery_id = d.id) AS item_count, " &
                                       "(SELECT COALESCE(SUM(di.qty), 0) FROM delivery_items di WHERE di.delivery_id = d.id) AS units, " &
                                       "(SELECT COALESCE(SUM(di.qty_remaining), 0) FROM delivery_items di WHERE di.delivery_id = d.id) AS remaining, " &
                                       "d.total_cost, d.expected_profit " &
                                       "FROM deliveries d JOIN suppliers sp ON sp.id = d.supplier_id WHERE " & DelWhere &
                                       " ORDER BY d.delivery_date DESC, d.id DESC LIMIT " & (MaxRows + 1), DelParams(r))
        If dt.Rows.Count > MaxRows Then
            dt.Rows.RemoveAt(dt.Rows.Count - 1)
            doc.TruncatedNote = "Showing the latest " & MaxRows.ToString("N0", Inv) & " deliveries of this period."
        End If
        dt.Columns.Add("state", GetType(String))
        For Each row As DataRow In dt.Rows
            row("state") = If(Txt(row("status")) = "sent", "Received", "Draft")
        Next

        doc.ScreenHint = "Select a delivery, then choose View items (or double-click it)."
        doc.TableTitle = "All deliveries (double-click a row to see its items)"
        doc.Columns.Add(New ReportColumn("BATCH NO", "batch_no", ColKind.Text, 12, 120))
        doc.Columns.Add(New ReportColumn("SUPPLIER", "supplier", ColKind.Text, 22, 170))
        doc.Columns.Add(New ReportColumn("DELIVERY DATE", "delivery_date", ColKind.DayOnly, 13, 130))
        doc.Columns.Add(New ReportColumn("STATUS", "state", ColKind.Text, 9, 90))
        doc.Columns.Add(New ReportColumn("ITEMS", "item_count", ColKind.Number, 7, 70))
        doc.Columns.Add(New ReportColumn("UNITS", "units", ColKind.Number, 8, 80))
        doc.Columns.Add(New ReportColumn("STILL LEFT", "remaining", ColKind.Number, 9, 90))
        doc.Columns.Add(New ReportColumn("TOTAL COST", "total_cost", ColKind.Money, 12, 120))
        doc.Columns.Add(New ReportColumn("EXPECTED PROFIT", "expected_profit", ColKind.Money, 13, 130))
        doc.Data = dt
        doc.EmptyText = "No deliveries in this period."
        Return doc
    End Function

    ''' <summary>The items of one delivery (for the details window).</summary>
    Public Function DeliveryItems(deliveryId As Integer) As DataTable
        Return Db.Table("SELECT p.name AS product, p.measurement AS size, di.qty, di.qty_remaining AS remaining, di.cost_price, " &
                        "di.selling_price, di.line_cost, di.expected_profit " &
                        "FROM delivery_items di JOIN products p ON p.id = di.product_id WHERE di.delivery_id = @id ORDER BY p.name",
                        Db.Params("@id", deliveryId))
    End Function

End Module

' =====================================================================================
'  LOSS REPORT - what the shop gave up: discounts, special prices and points redeemed
' =====================================================================================
Public Module LossData

    Private ReadOnly Inv As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
    Private ReadOnly PaleBar As Color = Color.FromArgb(205, 165, 165)
    Private ReadOnly GreyBar As Color = Color.FromArgb(110, 120, 130)
    Private Const MaxRows As Integer = 3000

    ' a sale "gave something up" when it has a percentage discount, an approved special price, or points
    Private Const AnyConcession As String = "(s.discount_amount > 0 OR s.special_discount_amount > 0 OR s.points_value > 0)"
    Private Const InRange As String = "s.created_at >= @f AND s.created_at < @t"
    Private Const GivenUp As String = "(s.discount_amount + s.special_discount_amount + s.points_value)"
    Private Const ReasonSql As String = "CONCAT_WS(' + ', IF(s.discount_amount > 0, COALESCE(s.discount_name, 'Discount'), NULL), " &
                                        "IF(s.special_discount_amount > 0, 'Special price', NULL), IF(s.points_value > 0, 'Points', NULL))"

    Private Function Prm(r As DateRange) As Dictionary(Of String, Object)
        Return Db.Params("@f", r.From, "@t", r.ToExclusive)
    End Function

    Private Function Dec(o As Object) As Decimal
        If o Is Nothing OrElse o Is DBNull.Value Then Return 0D
        Return Convert.ToDecimal(o)
    End Function

    Private Function Txt(o As Object) As String
        If o Is Nothing OrElse o Is DBNull.Value Then Return ""
        Return Convert.ToString(o, Inv)
    End Function

    Private Function Pct(part As Decimal, whole As Decimal) As Decimal
        If whole = 0D Then Return 0D
        Return Math.Round(part / whole * 100D, 1)
    End Function

    Private Function NewDoc(title As String, r As DateRange) As ReportDoc
        Dim d As New ReportDoc()
        d.ShopName = PosRepository.ShopSetting("shop_name", "DARWIN TRADING")
        d.Title = title
        d.PeriodLabel = r.Label
        d.GeneratedBy = AppSession.FullName
        d.GeneratedAt = DateTime.Now
        Return d
    End Function

    ''' <summary>The six status figures shared by the Summary, Sales and Cashiers views, plus a one-line note.</summary>
    Private Sub AddKpis(doc As ReportDoc, p As ReportPeriod, r As DateRange)
        Dim q As Dictionary(Of String, Object) = Prm(r)
        Dim t As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(s.subtotal), 0) AS full_price, COALESCE(SUM(s.discount_amount), 0) AS disc, " &
                                      "COALESCE(SUM(s.special_discount_amount), 0) AS spec, COALESCE(SUM(s.points_value), 0) AS pts, " &
                                      "COALESCE(SUM(CASE WHEN " & AnyConcession & " THEN 1 ELSE 0 END), 0) AS affected " &
                                      "FROM sales s WHERE " & InRange, q)
        Dim below As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(x.cost - x.collected), 0) AS loss FROM " &
                                          "(SELECT (s.total - s.points_value) AS collected, " &
                                          "(SELECT COALESCE(SUM(si.qty * si.cost_price), 0) FROM sale_items si WHERE si.sale_id = s.id) AS cost " &
                                          "FROM sales s WHERE " & InRange & ") x WHERE x.collected < x.cost", q)
        Dim n As Decimal = Dec(t.Rows(0)("n"))
        Dim fullPrice As Decimal = Dec(t.Rows(0)("full_price"))
        Dim disc As Decimal = Dec(t.Rows(0)("disc"))
        Dim spec As Decimal = Dec(t.Rows(0)("spec"))
        Dim pts As Decimal = Dec(t.Rows(0)("pts"))
        Dim given As Decimal = disc + spec + pts
        Dim belowN As Decimal = Dec(below.Rows(0)("n"))
        Dim belowLoss As Decimal = Dec(below.Rows(0)("loss"))

        doc.Kpis.Add(New ReportKpi("Total given up", ReportFormat.Peso(given)))
        doc.Kpis.Add(New ReportKpi("Discounts", ReportFormat.Peso(disc)))
        doc.Kpis.Add(New ReportKpi("Special prices", ReportFormat.Peso(spec)))
        doc.Kpis.Add(New ReportKpi("Points redeemed", ReportFormat.Peso(pts)))
        doc.Kpis.Add(New ReportKpi("Sales with a concession", ReportFormat.Whole(Dec(t.Rows(0)("affected"))) & " of " & ReportFormat.Whole(n)))
        doc.Kpis.Add(New ReportKpi("Sold below cost (" & ReportFormat.Whole(belowN) & ")", ReportFormat.Peso(belowLoss), belowN > 0D))

        Dim prevRange As DateRange = ReportPeriods.RangeFor(p, ReportPeriods.Shift(p, r.From, -1))
        Dim prev As Decimal = Dec(Db.Scalar("SELECT COALESCE(SUM" & GivenUp & ", 0) FROM sales s WHERE " & InRange, Prm(prevRange)))
        Dim note As String = ""
        If fullPrice > 0D Then note = Pct(given, fullPrice).ToString("0.0", Inv) & "% of full-price sales was given up."
        Dim cmp As String = ""
        If given = 0D AndAlso prev = 0D Then
            cmp = ""
        ElseIf prev = 0D Then
            cmp = "Nothing was given up " & ReportPeriods.PreviousName(p) & "."
        Else
            Dim ch As Decimal = Math.Round((given - prev) / prev * 100D, 0)
            If ch = 0D Then
                cmp = "Same as " & ReportPeriods.PreviousName(p) & "."
            Else
                cmp = "That is " & If(ch > 0D, "up ", "down ") & Math.Abs(ch).ToString("0", Inv) & "% compared with " & ReportPeriods.PreviousName(p) & "."
            End If
        End If
        doc.Note = (note & " " & cmp).Trim()
    End Sub

    Private Function TypeChart(title As String, t As DataTable, labelCol As String, valueCol As String) As ReportChart
        Dim ch As New ReportChart()
        ch.Title = title
        ch.Horizontal = True
        ch.IsMoney = True
        Dim s As New ChartSeries(title, Theme.Maroon)
        Dim count As Integer = 0
        For Each row As DataRow In t.Rows
            If count >= 10 Then Exit For
            ch.Labels.Add(Txt(row(labelCol)))
            s.Values.Add(Dec(row(valueCol)))
            count += 1
        Next
        ch.Series.Add(s)
        Return ch
    End Function

    ' ------------------------------------------------------------------ Summary
    Public Function Summary(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Loss Report", r)
        Dim q As Dictionary(Of String, Object) = Prm(r)
        AddKpis(doc, p, r)

        ' given up per hour / day, by kind
        Dim expr As String = If(p = ReportPeriod.Daily, "HOUR(s.created_at)", "DATE_FORMAT(s.created_at, '%Y-%m-%d')")
        Dim tr As DataTable = Db.Table("SELECT " & expr & " AS k, SUM(s.discount_amount) AS d, SUM(s.special_discount_amount) AS sp, SUM(s.points_value) AS pv " &
                                       "FROM sales s WHERE " & InRange & " AND " & AnyConcession & " GROUP BY 1", q)
        Dim ld As New Dictionary(Of String, Decimal)()
        Dim ls As New Dictionary(Of String, Decimal)()
        Dim lp As New Dictionary(Of String, Decimal)()
        Dim keys As New List(Of String)()
        For Each row As DataRow In tr.Rows
            Dim k As String = Txt(row("k"))
            keys.Add(k)
            ld(k) = Dec(row("d"))
            ls(k) = Dec(row("sp"))
            lp(k) = Dec(row("pv"))
        Next
        Dim labels As New List(Of String)()
        Dim bucketKeys As New List(Of String)()
        If p = ReportPeriod.Daily Then
            Dim lo As Integer = 8
            Dim hi As Integer = 18
            For Each k As String In keys
                Dim h As Integer
                If Integer.TryParse(k, h) Then
                    If h < lo Then lo = h
                    If h > hi Then hi = h
                End If
            Next
            For h As Integer = lo To hi
                bucketKeys.Add(h.ToString(Inv))
                Dim h12 As Integer = h Mod 12
                If h12 = 0 Then h12 = 12
                labels.Add(h12.ToString(Inv) & If(h < 12, "a", "p"))
            Next
        Else
            Dim d As DateTime = r.From
            While d < r.ToExclusive
                bucketKeys.Add(d.ToString("yyyy-MM-dd", Inv))
                labels.Add(If(p = ReportPeriod.Weekly, d.ToString("ddd d", Inv), d.Day.ToString(Inv)))
                d = d.AddDays(1)
            End While
        End If
        Dim trend As New ReportChart()
        trend.Title = If(p = ReportPeriod.Daily, "Given up per hour", "Given up per day")
        trend.IsMoney = True
        Dim sd As New ChartSeries("Discounts", Theme.Maroon)
        Dim ss As New ChartSeries("Special prices", PaleBar)
        Dim sp As New ChartSeries("Points", GreyBar)
        For i As Integer = 0 To bucketKeys.Count - 1
            trend.Labels.Add(labels(i))
            Dim a As Decimal = 0D
            Dim b As Decimal = 0D
            Dim c As Decimal = 0D
            ld.TryGetValue(bucketKeys(i), a)
            ls.TryGetValue(bucketKeys(i), b)
            lp.TryGetValue(bucketKeys(i), c)
            sd.Values.Add(a)
            ss.Values.Add(b)
            sp.Values.Add(c)
        Next
        trend.Series.Add(sd)
        trend.Series.Add(ss)
        trend.Series.Add(sp)
        doc.Charts.Add(trend)

        ' breakdown: every named discount, special prices, points
        Dim dt As New DataTable()
        dt.Columns.Add("type", GetType(String))
        dt.Columns.Add("kind", GetType(String))
        dt.Columns.Add("times", GetType(Integer))
        dt.Columns.Add("amount", GetType(Decimal))
        dt.Columns.Add("share", GetType(Decimal))

        Dim byDisc As DataTable = Db.Table("SELECT COALESCE(s.discount_name, 'Discount') AS name, COUNT(*) AS n, SUM(s.discount_amount) AS amt " &
                                           "FROM sales s WHERE " & InRange & " AND s.discount_amount > 0 GROUP BY s.discount_name", q)
        For Each row As DataRow In byDisc.Rows
            dt.Rows.Add(Txt(row("name")), "Discount", Convert.ToInt32(row("n")), Dec(row("amt")), 0D)
        Next
        Dim sk As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(s.special_discount_amount), 0) AS amt FROM sales s WHERE " & InRange & " AND s.special_discount_amount > 0", q)
        If Convert.ToInt32(sk.Rows(0)("n")) > 0 Then dt.Rows.Add("Special price (admin approved)", "Special price", Convert.ToInt32(sk.Rows(0)("n")), Dec(sk.Rows(0)("amt")), 0D)
        Dim pk As DataTable = Db.Table("SELECT COUNT(*) AS n, COALESCE(SUM(s.points_value), 0) AS amt, COALESCE(SUM(s.points_used), 0) AS used FROM sales s WHERE " & InRange & " AND s.points_value > 0", q)
        If Convert.ToInt32(pk.Rows(0)("n")) > 0 Then dt.Rows.Add("Points redeemed (" & ReportFormat.Whole(Dec(pk.Rows(0)("used"))) & " pts)", "Points", Convert.ToInt32(pk.Rows(0)("n")), Dec(pk.Rows(0)("amt")), 0D)

        Dim total As Decimal = 0D
        For Each row As DataRow In dt.Rows
            total += Dec(row("amount"))
        Next
        For Each row As DataRow In dt.Rows
            row("share") = Pct(Dec(row("amount")), total)
        Next
        dt.DefaultView.Sort = "amount DESC"
        Dim sorted As DataTable = dt.DefaultView.ToTable()

        doc.Charts.Add(TypeChart("Given up by type", sorted, "type", "amount"))
        doc.TableTitle = "Where it came from"
        doc.Columns.Add(New ReportColumn("TYPE", "type", ColKind.Text, 32, 200))
        doc.Columns.Add(New ReportColumn("KIND", "kind", ColKind.Text, 14, 110))
        doc.Columns.Add(New ReportColumn("TIMES USED", "times", ColKind.Number, 12, 100))
        doc.Columns.Add(New ReportColumn("AMOUNT GIVEN UP", "amount", ColKind.Money, 18, 140))
        doc.Columns.Add(New ReportColumn("SHARE", "share", ColKind.Percent, 12, 90))
        doc.Data = sorted
        doc.EmptyText = "No discount, special price or points were used in this period."
        Return doc
    End Function

    ' ------------------------------------------------------------------ Sales (one row per sale that gave something up)
    Public Function SalesList(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Loss Report - Sales", r)
        AddKpis(doc, p, r)

        Dim dt As DataTable = Db.Table("SELECT s.id, s.receipt_no, s.created_at, COALESCE(s.cashier_name, '') AS cashier, COALESCE(s.customer_name, '') AS customer, " &
                                       ReasonSql & " AS reason, COALESCE(s.special_approved_name, '') AS approver, " &
                                       "s.subtotal AS full_price, " & GivenUp & " AS given_up, s.total " &
                                       "FROM sales s WHERE " & InRange & " AND " & AnyConcession &
                                       " ORDER BY s.created_at DESC, s.id DESC LIMIT " & (MaxRows + 1), Prm(r))
        If dt.Rows.Count > MaxRows Then
            dt.Rows.RemoveAt(dt.Rows.Count - 1)
            doc.TruncatedNote = "Showing the latest " & MaxRows.ToString("N0", Inv) & " sales of this period. Choose a shorter period to see the rest."
        End If

        doc.ScreenHint = "Select a sale, then choose View receipt (or double-click it)."
        doc.TableTitle = "Sales that gave something up"
        doc.Columns.Add(New ReportColumn("RECEIPT NO", "receipt_no", ColKind.Text, 13, 120))
        doc.Columns.Add(New ReportColumn("DATE & TIME", "created_at", ColKind.Stamp, 16, 170))
        doc.Columns.Add(New ReportColumn("CASHIER", "cashier", ColKind.Text, 12, 110))
        doc.Columns.Add(New ReportColumn("CUSTOMER", "customer", ColKind.Text, 13, 120))
        doc.Columns.Add(New ReportColumn("REASON", "reason", ColKind.Text, 16, 150))
        doc.Columns.Add(New ReportColumn("APPROVED BY", "approver", ColKind.Text, 12, 110))
        doc.Columns.Add(New ReportColumn("FULL PRICE", "full_price", ColKind.Money, 11, 110))
        doc.Columns.Add(New ReportColumn("GIVEN UP", "given_up", ColKind.Money, 11, 110))
        doc.Columns.Add(New ReportColumn("SALE TOTAL", "total", ColKind.Money, 11, 110))
        doc.Data = dt
        doc.EmptyText = "No discount, special price or points were used in this period."
        Return doc
    End Function

    ' ------------------------------------------------------------------ Cashiers (who gives what away)
    Public Function Cashiers(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Loss Report - Cashiers", r)
        Dim q As Dictionary(Of String, Object) = Prm(r)
        AddKpis(doc, p, r)

        Dim dt As DataTable = Db.Table("SELECT COALESCE(s.cashier_name, '(unknown)') AS cashier, COUNT(*) AS sales_count, " &
                                       "COALESCE(SUM(CASE WHEN " & AnyConcession & " THEN 1 ELSE 0 END), 0) AS with_concession, " &
                                       "COALESCE(SUM(s.discount_amount), 0) AS disc, COALESCE(SUM(s.special_discount_amount), 0) AS spec, " &
                                       "COALESCE(SUM(s.points_value), 0) AS pts, COALESCE(SUM(s.subtotal), 0) AS full_price, " &
                                       "COALESCE(SUM" & GivenUp & ", 0) AS given_up " &
                                       "FROM sales s WHERE " & InRange & " GROUP BY s.cashier_id, s.cashier_name ORDER BY given_up DESC", q)
        dt.Columns.Add("rate", GetType(Decimal))
        For Each row As DataRow In dt.Rows
            row("rate") = Pct(Dec(row("given_up")), Dec(row("full_price")))
        Next

        doc.Charts.Add(TypeChart("Given up by cashier", dt, "cashier", "given_up"))

        Dim ap As DataTable = Db.Table("SELECT COALESCE(s.special_approved_name, '(unknown)') AS approver, SUM(s.special_discount_amount) AS amt " &
                                       "FROM sales s WHERE " & InRange & " AND s.special_discount_amount > 0 " &
                                       "GROUP BY s.special_approved_by, s.special_approved_name ORDER BY amt DESC", q)
        If ap.Rows.Count > 0 Then doc.Charts.Add(TypeChart("Special prices approved by", ap, "approver", "amt"))

        doc.TableTitle = "Given up by cashier"
        doc.Columns.Add(New ReportColumn("CASHIER", "cashier", ColKind.Text, 20, 140))
        doc.Columns.Add(New ReportColumn("SALES", "sales_count", ColKind.Number, 8, 80))
        doc.Columns.Add(New ReportColumn("WITH A CONCESSION", "with_concession", ColKind.Number, 12, 120))
        doc.Columns.Add(New ReportColumn("DISCOUNTS", "disc", ColKind.Money, 11, 110))
        doc.Columns.Add(New ReportColumn("SPECIAL PRICES", "spec", ColKind.Money, 12, 120))
        doc.Columns.Add(New ReportColumn("POINTS", "pts", ColKind.Money, 10, 100))
        doc.Columns.Add(New ReportColumn("TOTAL GIVEN UP", "given_up", ColKind.Money, 12, 120))
        doc.Columns.Add(New ReportColumn("% OF FULL PRICE", "rate", ColKind.Percent, 11, 110))
        doc.Data = dt
        doc.EmptyText = "There were no sales in this period."
        Return doc
    End Function

    ' ------------------------------------------------------------------ Below cost (the only real loss)
    Public Function BelowCost(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Loss Report - Below cost", r)

        Dim dt As DataTable = Db.Table("SELECT x.id, x.receipt_no, x.created_at, x.cashier, x.approver, x.reason, x.cost, x.collected, (x.cost - x.collected) AS loss FROM " &
                                       "(SELECT s.id, s.receipt_no, s.created_at, COALESCE(s.cashier_name, '') AS cashier, " &
                                       "COALESCE(s.special_approved_name, '') AS approver, " & ReasonSql & " AS reason, " &
                                       "(SELECT COALESCE(SUM(si.qty * si.cost_price), 0) FROM sale_items si WHERE si.sale_id = s.id) AS cost, " &
                                       "(s.total - s.points_value) AS collected FROM sales s WHERE " & InRange & ") x " &
                                       "WHERE x.collected < x.cost ORDER BY loss DESC LIMIT " & (MaxRows + 1), Prm(r))
        If dt.Rows.Count > MaxRows Then
            dt.Rows.RemoveAt(dt.Rows.Count - 1)
            doc.TruncatedNote = "Showing the " & MaxRows.ToString("N0", Inv) & " biggest losses of this period."
        End If

        Dim totalLoss As Decimal = 0D
        Dim biggest As Decimal = 0D
        Dim costSum As Decimal = 0D
        For Each row As DataRow In dt.Rows
            Dim l As Decimal = Dec(row("loss"))
            totalLoss += l
            costSum += Dec(row("cost"))
            If l > biggest Then biggest = l
        Next
        doc.Kpis.Add(New ReportKpi("Sales below cost", ReportFormat.Whole(dt.Rows.Count), dt.Rows.Count > 0))
        doc.Kpis.Add(New ReportKpi("Total loss", ReportFormat.Peso(totalLoss), totalLoss > 0D))
        doc.Kpis.Add(New ReportKpi("Biggest single loss", ReportFormat.Peso(biggest)))
        doc.Kpis.Add(New ReportKpi("Cost of those items", ReportFormat.Peso(costSum)))
        doc.Note = "Sales where the customer paid less than the items cost the shop."

        doc.ScreenHint = "Select a sale, then choose View receipt (or double-click it)."
        doc.TableTitle = "Sales made below cost"
        doc.Columns.Add(New ReportColumn("RECEIPT NO", "receipt_no", ColKind.Text, 14, 120))
        doc.Columns.Add(New ReportColumn("DATE & TIME", "created_at", ColKind.Stamp, 17, 170))
        doc.Columns.Add(New ReportColumn("CASHIER", "cashier", ColKind.Text, 13, 110))
        doc.Columns.Add(New ReportColumn("REASON", "reason", ColKind.Text, 16, 150))
        doc.Columns.Add(New ReportColumn("APPROVED BY", "approver", ColKind.Text, 13, 110))
        doc.Columns.Add(New ReportColumn("COST OF ITEMS", "cost", ColKind.Money, 12, 120))
        doc.Columns.Add(New ReportColumn("PAID", "collected", ColKind.Money, 11, 110))
        doc.Columns.Add(New ReportColumn("LOSS", "loss", ColKind.Money, 11, 110))
        doc.Data = dt
        doc.EmptyText = "No sale went below cost in this period."
        Return doc
    End Function

End Module
