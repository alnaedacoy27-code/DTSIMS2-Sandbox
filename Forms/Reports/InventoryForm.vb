Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Reports > Inventory. Total stock by default; filter by category, by supplier company or by one delivery batch
''' (then you also see how much of that delivery is still left). Low stock is flagged.
''' </summary>
Public Class InventoryForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()

    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cardItems As New StatCard()
    Private ReadOnly _cardValue As New StatCard()
    Private ReadOnly _cardLow As New StatCard()
    Private ReadOnly _cardOut As New StatCard()
    Private ReadOnly _cardVat As New StatCard()
    Private _vat As Decimal

    Private ReadOnly _bar As New Panel()
    Private ReadOnly _cat As New ComboBox()
    Private ReadOnly _sup As New ComboBox()
    Private ReadOnly _batch As New ComboBox()
    Private ReadOnly _search As New SearchBox("Search name, size or barcode...")

    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()

    Private ReadOnly _catIds As New List(Of Integer)()
    Private ReadOnly _batchIds As New List(Of Integer)()
    Private _loading As Boolean

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildStats()
        BuildBar()
        BuildGrid()

        Controls.Add(_listHost)
        Controls.Add(_bar)
        Controls.Add(_stats)
        Controls.Add(_header)
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"products", "categories", "deliveries", "delivery_items", "suppliers", "vat_rates"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadFilters()
        LoadInventory()
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Inventory"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "What you have in stock right now. Filter by category, supplier or one delivery"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)
    End Sub

    Private Sub BuildStats()
        _stats.Dock = DockStyle.Top
        _stats.Height = Theme.S(84)
        _stats.BackColor = Color.White
        _stats.FlowDirection = FlowDirection.LeftToRight
        _stats.WrapContents = False
        _stats.Padding = New Padding(Theme.S(32), Theme.S(4), 0, 0)

        For Each c As StatCard In New StatCard() {_cardItems, _cardValue, _cardLow, _cardOut, _cardVat}
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            _stats.Controls.Add(c)
        Next
        _cardItems.Caption = "Items listed"
        _cardValue.Caption = "Stock value (at cost)"
        _cardLow.Caption = "Low stock"
        _cardOut.Caption = "Out of stock"
        _cardVat.Caption = "VAT in the prices"
    End Sub

    Private Sub StyleCombo(box As ComboBox, width As Integer)
        box.DropDownStyle = ComboBoxStyle.DropDownList
        box.FlatStyle = FlatStyle.Flat
        box.Font = Theme.FontBase
        box.BackColor = Color.White
        box.Width = width
        _bar.Controls.Add(box)
    End Sub

    Private Sub BuildBar()
        _bar.Dock = DockStyle.Top
        _bar.Height = Theme.S(58)
        _bar.BackColor = Color.White

        StyleCombo(_cat, 180)
        StyleCombo(_sup, 210)
        StyleCombo(_batch, 250)
        _cat.Location = New Point(Theme.S(32), Theme.S(14))
        _sup.Location = New Point(Theme.S(32) + Theme.S(180) + Theme.S(12), Theme.S(14))
        _batch.Location = New Point(Theme.S(32) + Theme.S(180) + Theme.S(12) + Theme.S(210) + Theme.S(12), Theme.S(14))
        AddHandler _cat.SelectedIndexChanged, AddressOf OnCategoryChanged
        AddHandler _sup.SelectedIndexChanged, AddressOf OnSupplierChanged
        AddHandler _batch.SelectedIndexChanged, AddressOf OnBatchChanged

        _search.Size = New Size(Theme.S(280), Theme.S(40))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged
        _bar.Controls.Add(_search)
        AddHandler _bar.Resize, AddressOf OnBarResize
        OnBarResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnBarResize(sender As Object, e As EventArgs)
        _search.Location = New Point(Math.Max(_batch.Right + Theme.S(12), _bar.ClientSize.Width - Theme.S(32) - _search.Width), Theme.S(8))
    End Sub

    Private Sub AddColumn(name As String, headerText As String, dataProp As String, weight As Single, rightAlign As Boolean)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = name
        c.HeaderText = headerText
        c.DataPropertyName = dataProp
        c.FillWeight = weight
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        If rightAlign Then
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
        End If
        _grid.Columns.Add(c)
    End Sub

    Private Sub BuildGrid()
        _listHost.Dock = DockStyle.Fill
        _listHost.BackColor = Color.White
        _listHost.Padding = New Padding(Theme.S(32), 0, Theme.S(32), Theme.S(16))

        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 60)
        AddColumn("item", "ITEM", "name", 24, False)
        AddColumn("size", "SIZE", "measurement", 10, False)
        AddColumn("barcode", "BARCODE", "barcode", 12, False)
        AddColumn("stock", "IN STOCK", "qty_on_hand", 8, True)
        AddColumn("alert", "WARN AT", "low_stock_threshold", 7, True)
        AddColumn("scope", "FROM THIS SELECTION", "remaining", 12, True)
        AddColumn("cost", "COST", "cost_price", 9, True)
        AddColumn("price", "PRICE (VAT incl.)", "selling_price", 10, True)
        AddColumn("vat", "VAT", "selling_price", 9, True)
        AddColumn("exvat", "PRICE w/o VAT", "selling_price", 10, True)
        AddColumn("profit", "PROFIT / UNIT", "selling_price", 9, True)
        AddColumn("value", "STOCK VALUE", "qty_on_hand", 11, True)
        AddColumn("last", "LAST DELIVERY", "last_delivery", 10, False)
        AddColumn("status", "STATUS", "low_stock_threshold", 11, False)

        ' keep every column readable: when the window is narrow the list scrolls sideways
        Dim mins As Integer() = {230, 100, 130, 80, 80, 150, 100, 130, 100, 130, 110, 120, 120, 120}
        For i As Integer = 0 To _grid.Columns.Count - 1
            _grid.Columns(i).MinimumWidth = mins(i)
        Next
        AddHandler _grid.CellPainting, AddressOf OnCellPainting
        AddHandler _grid.CellFormatting, AddressOf OnCellFormatting

        _empty.Dock = DockStyle.Fill
        _empty.TextAlign = ContentAlignment.MiddleCenter
        _empty.Font = Theme.FontHeading
        _empty.ForeColor = Theme.TextMuted
        _empty.BackColor = Color.White
        _empty.Visible = False

        _listHost.Controls.Add(_grid)
        _listHost.Controls.Add(_empty)
    End Sub

    ' ------------------------------------------------------------------ filters
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadFilters()
        LoadInventory()
    End Sub

    Private Function CurrentCategoryId() As Integer
        Dim i As Integer = _cat.SelectedIndex
        If i <= 0 OrElse i >= _catIds.Count Then Return 0
        Return _catIds(i)
    End Function

    Private Function CurrentCompany() As String
        If _sup.SelectedIndex <= 0 Then Return ""
        Return _sup.SelectedItem.ToString()
    End Function

    Private Function CurrentBatchId() As Integer
        Dim i As Integer = _batch.SelectedIndex
        If i <= 0 OrElse i >= _batchIds.Count Then Return 0
        Return _batchIds(i)
    End Function

    Private Sub LoadFilters()
        Dim keepCat As Integer = CurrentCategoryId()
        Dim keepCompany As String = CurrentCompany()
        Dim keepBatch As Integer = CurrentBatchId()
        _loading = True
        Try
            _cat.Items.Clear()
            _catIds.Clear()
            _cat.Items.Add("All categories")
            _catIds.Add(0)
            Dim ci As Integer = 0
            For Each r As DataRow In CategoryRepository.GetAll().Rows
                _cat.Items.Add(r("name").ToString())
                _catIds.Add(Convert.ToInt32(r("id")))
                If Convert.ToInt32(r("id")) = keepCat Then ci = _cat.Items.Count - 1
            Next
            _cat.SelectedIndex = ci

            _sup.Items.Clear()
            _sup.Items.Add("All suppliers")
            Dim si As Integer = 0
            For Each r As DataRow In InventoryRepository.GetSupplierCompanies().Rows
                _sup.Items.Add(r("company_name").ToString())
                If r("company_name").ToString() = keepCompany Then si = _sup.Items.Count - 1
            Next
            _sup.SelectedIndex = si

            _batch.Items.Clear()
            _batchIds.Clear()
            _batch.Items.Add("All deliveries")
            _batchIds.Add(0)
            Dim bi As Integer = 0
            For Each r As DataRow In InventoryRepository.GetSentBatches().Rows
                _batch.Items.Add(r("batch_no").ToString() & "  -  " & r("company_name").ToString())
                _batchIds.Add(Convert.ToInt32(r("id")))
                If Convert.ToInt32(r("id")) = keepBatch Then bi = _batch.Items.Count - 1
            Next
            _batch.SelectedIndex = bi
        Catch
            ' database offline: keep what is there
        Finally
            _loading = False
        End Try
    End Sub

    Private Sub OnCategoryChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        LoadInventory()
    End Sub

    Private Sub OnSupplierChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        If _sup.SelectedIndex > 0 Then
            _loading = True
            _batch.SelectedIndex = 0
            _loading = False
        End If
        LoadInventory()
    End Sub

    Private Sub OnBatchChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        If _batch.SelectedIndex > 0 Then
            _loading = True
            _sup.SelectedIndex = 0
            _loading = False
        End If
        LoadInventory()
    End Sub

    Private Sub OnSearchChanged()
        LoadInventory()
    End Sub

    ' ------------------------------------------------------------------ data
    Private Sub LoadInventory()
        If _loading Then Return
        _loading = True
        Try
            Dim scoped As Boolean = (CurrentBatchId() > 0 OrElse CurrentCompany().Length > 0)
            Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex

            Dim dt As DataTable = InventoryRepository.GetInventory(_search.Query, CurrentCategoryId(), CurrentCompany(), CurrentBatchId())
            _grid.DataSource = dt
            _grid.ClearSelection()
            _grid.Columns("scope").Visible = scoped
            If scroll >= 0 AndAlso scroll < _grid.Rows.Count Then _grid.FirstDisplayedScrollingRowIndex = scroll

            Try
                Dim v As DataRow = VatRepository.GetActive()
                _vat = If(v Is Nothing, 0D, Convert.ToDecimal(v("percentage")))
                _cardVat.Value = If(v Is Nothing, "not set", Percent.Format(_vat))
                _grid.Columns("vat").HeaderText = If(v Is Nothing, "VAT", "VAT (" & Percent.Format(_vat) & ")")
            Catch
                _vat = 0D
            End Try

            Dim t As InventoryTotals = InventoryRepository.Summarize(dt)
            _cardItems.Value = t.Items.ToString()
            _cardValue.Value = Currency.Format(t.StockValue)
            _cardLow.Value = t.Low.ToString()
            _cardLow.Warn = (t.Low > 0)
            _cardOut.Value = t.OutOfStock.ToString()
            _cardOut.Warn = (t.OutOfStock > 0)

            If dt.Rows.Count = 0 Then
                _empty.Text = If(scoped OrElse _search.Query.Length > 0 OrElse CurrentCategoryId() > 0,
                                 "Nothing matches these filters.", "No products yet. Add products, then receive a delivery.")
                _empty.Visible = True
                _empty.BringToFront()
            Else
                _empty.Visible = False
            End If
        Catch ex As Exception
            _empty.Text = "Could not load the inventory: " & ex.Message
            _empty.Visible = True
            _empty.BringToFront()
        Finally
            _loading = False
        End Try
    End Sub

    ' ------------------------------------------------------------------ painting
    Private Sub OnCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        If colName <> "item" AndAlso colName <> "status" AndAlso colName <> "stock" Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim c As Rectangle = e.CellBounds
        Dim qty As Integer = Convert.ToInt32(row("qty_on_hand"))
        Dim limit As Integer = Convert.ToInt32(row("low_stock_threshold"))
        Dim status As String = InventoryRepository.StatusOf(qty, limit)

        Select Case colName
            Case "item"
                Dim nm As String = row("name").ToString()
                Dim circle As New Rectangle(c.X + Theme.S(12), c.Y + (c.Height - Theme.S(38)) \ 2, Theme.S(38), Theme.S(38))
                Avatar.PaintInitials(e.Graphics, circle, nm, 10.0F)
                Dim textX As Integer = circle.Right + Theme.S(12)
                Dim textW As Integer = Math.Max(Theme.S(10), c.Right - textX - Theme.S(8))
                TextRenderer.DrawText(e.Graphics, nm, NameFont, New Rectangle(textX, c.Y + 11, textW, 20), Theme.TextDark,
                                      TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)
                TextRenderer.DrawText(e.Graphics, row("category").ToString(), Theme.FontSmall, New Rectangle(textX, c.Y + 32, textW, 18),
                                      Theme.TextMuted, TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

            Case "stock"
                TextRenderer.DrawText(e.Graphics, qty.ToString(), NameFont, New Rectangle(c.X, c.Y, c.Width - 12, c.Height),
                                      If(status = "In stock", Theme.TextDark, Theme.AlertRed),
                                      TextFormatFlags.Right Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)

            Case "status"
                Dim back As Color
                Dim fore As Color
                Select Case status
                    Case "Out of stock"
                        back = Color.FromArgb(253, 230, 230)
                        fore = Theme.AlertRed
                    Case "Low stock"
                        back = Color.FromArgb(255, 243, 219)
                        fore = Color.FromArgb(160, 100, 10)
                    Case Else
                        back = Color.FromArgb(226, 246, 234)
                        fore = Color.FromArgb(22, 130, 60)
                End Select
                Pills.Draw(e.Graphics, c, status, back, fore)
        End Select
        e.Handled = True
    End Sub

    Private Sub OnCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        Select Case _grid.Columns(e.ColumnIndex).Name
            Case "cost"
                e.Value = Currency.FormatCost(e.Value)
                e.FormattingApplied = True
            Case "price"
                e.Value = Currency.Format(e.Value)
                e.FormattingApplied = True
            Case "vat"
                ' selling prices already include VAT: the VAT inside a price = price x rate / (100 + rate)
                Dim price As Decimal = Convert.ToDecimal(row("selling_price"))
                e.Value = Currency.Format(Math.Round(price * _vat / (100D + _vat), 2))
                e.FormattingApplied = True
            Case "exvat"
                Dim price As Decimal = Convert.ToDecimal(row("selling_price"))
                e.Value = Currency.Format(price - Math.Round(price * _vat / (100D + _vat), 2))
                e.FormattingApplied = True
            Case "profit"
                e.Value = Currency.FormatCost(Convert.ToDecimal(row("selling_price")) - Convert.ToDecimal(row("cost_price")))
                e.FormattingApplied = True
            Case "value"
                e.Value = Currency.Format(Convert.ToInt32(row("qty_on_hand")) * Convert.ToDecimal(row("cost_price")))
                e.FormattingApplied = True
            Case "alert"
                e.Value = If(Convert.ToInt32(row("low_stock_threshold")) = 0, "not set", Convert.ToInt32(row("low_stock_threshold")).ToString())
                e.FormattingApplied = True
            Case "last"
                e.Value = If(row("last_delivery") Is DBNull.Value, "-", Convert.ToDateTime(row("last_delivery")).ToString("MMM d, yyyy"))
                e.FormattingApplied = True
            Case "size"
                If e.Value Is Nothing OrElse e.Value.ToString().Length = 0 Then
                    e.Value = "-"
                    e.FormattingApplied = True
                End If
            Case "scope"
                If row("remaining") Is DBNull.Value Then
                    e.Value = "-"
                Else
                    e.Value = Convert.ToInt32(row("remaining")).ToString() & " left of " & Convert.ToInt32(row("received")).ToString()
                End If
                e.FormattingApplied = True
        End Select
    End Sub

End Class
