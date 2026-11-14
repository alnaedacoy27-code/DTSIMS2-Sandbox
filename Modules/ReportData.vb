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
                                       "(SELECT COUNT(*) FROM delivery_items di WHERE di.delivery_id = d.id) AS lines, " &
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

        doc.TableTitle = "All deliveries (double-click a row to see its items)"
        doc.Columns.Add(New ReportColumn("BATCH NO", "batch_no", ColKind.Text, 12, 120))
        doc.Columns.Add(New ReportColumn("SUPPLIER", "supplier", ColKind.Text, 22, 170))
        doc.Columns.Add(New ReportColumn("DELIVERY DATE", "delivery_date", ColKind.DayOnly, 13, 130))
        doc.Columns.Add(New ReportColumn("STATUS", "state", ColKind.Text, 9, 90))
        doc.Columns.Add(New ReportColumn("ITEMS", "lines", ColKind.Number, 7, 70))
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
