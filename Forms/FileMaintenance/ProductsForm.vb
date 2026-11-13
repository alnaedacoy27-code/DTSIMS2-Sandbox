Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' File Maintenance > Products. Every size of a product is its own item with its own barcode.
''' Categories are managed from here (Categories button). Refreshes by itself (ILiveForm).
''' </summary>
Public Class ProductsForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly DetailsWidth As Integer = Theme.S(340)
    Private Shared ReadOnly ContentW As Integer = Theme.S(268)

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly BigNameFont As Font = Theme.MakeFont("Segoe UI Semibold", 14.0F)
    Private Shared ReadOnly CodeFont As New Font("Consolas", 10.5F)
    Private Shared ReadOnly BigCodeFont As New Font("Consolas", 14.0F, FontStyle.Bold)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _filter As New ComboBox()
    Private ReadOnly _search As New SearchBox("Search name, size or barcode...")
    Private ReadOnly _categories As New MaroonButton()
    Private ReadOnly _add As New MaroonButton()

    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cardNames As New StatCard()
    Private ReadOnly _cardItems As New StatCard()
    Private ReadOnly _cardCats As New StatCard()

    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()

    Private ReadOnly _details As New Panel()
    Private ReadOnly _scroll As New FlowLayoutPanel()
    Private ReadOnly _bar As New FlowLayoutPanel()
    Private ReadOnly _avatar As New Avatar()
    Private ReadOnly _dName As New Label()
    Private ReadOnly _dCategory As New Label()
    Private ReadOnly _vSize As Label
    Private ReadOnly _vBarcode As Label
    Private ReadOnly _vStock As Label
    Private ReadOnly _vThreshold As Label
    Private ReadOnly _vCreated As Label
    Private ReadOnly _btnEdit As New MaroonButton()
    Private ReadOnly _btnPrintCode As New MaroonButton()
    Private ReadOnly _btnSize As New MaroonButton()
    Private ReadOnly _btnDelete As New MaroonButton()

    Private ReadOnly _filterIds As New List(Of Integer)()
    Private _loading As Boolean
    Private _selectedId As Integer

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildStats()
        BuildGrid()
        BuildDetailsTop()
        _vSize = BuildRow("SIZE / MEASUREMENT", 24)
        _vBarcode = BuildRow("BARCODE", 30)
        _vBarcode.Font = BigCodeFont
        _vBarcode.ForeColor = Theme.Maroon
        _vStock = BuildRow("IN STOCK", 24)
        _vThreshold = BuildRow("LOW-STOCK ALERT AT", 24)
        _vCreated = BuildRow("ADDED", 24)
        BuildActions()

        Controls.Add(_listHost)
        Controls.Add(_details)
        Controls.Add(_stats)
        Controls.Add(_header)
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"products", "categories"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadFilter()
        LoadProducts()
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Products"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "Everything you sell. Each size is its own item with its own barcode"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _filter.DropDownStyle = ComboBoxStyle.DropDownList
        _filter.FlatStyle = FlatStyle.Flat
        _filter.Font = Theme.FontBase
        _filter.BackColor = Color.White
        _filter.Width = Theme.S(190)
        AddHandler _filter.SelectedIndexChanged, AddressOf OnFilterChanged

        _search.Size = New Size(Theme.S(260), Theme.S(40))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _categories.Text = "Categories"
        _categories.MakeSecondary()
        _categories.Size = New Size(Theme.S(120), Theme.S(40))
        AddHandler _categories.Click, AddressOf OnCategories

        _add.Text = "+  Add product"
        _add.Size = New Size(Theme.S(150), Theme.S(40))
        AddHandler _add.Click, AddressOf OnAdd

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)
        _header.Controls.Add(_filter)
        _header.Controls.Add(_search)
        _header.Controls.Add(_categories)
        _header.Controls.Add(_add)
        AddHandler _header.Resize, AddressOf OnHeaderResize
        OnHeaderResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnHeaderResize(sender As Object, e As EventArgs)
        _add.Location = New Point(Math.Max(Theme.S(300), _header.ClientSize.Width - Theme.S(32) - _add.Width), Theme.S(30))
        _categories.Location = New Point(Math.Max(0, _add.Left - Theme.S(12) - _categories.Width), Theme.S(30))
        _search.Location = New Point(Math.Max(0, _categories.Left - Theme.S(12) - _search.Width), Theme.S(30))
        _filter.Location = New Point(Math.Max(0, _search.Left - Theme.S(12) - _filter.Width), Theme.S(30) + (Theme.S(40) - _filter.Height) \ 2)
    End Sub

    Private Sub BuildStats()
        _stats.Dock = DockStyle.Top
        _stats.Height = Theme.S(84)
        _stats.BackColor = Color.White
        _stats.FlowDirection = FlowDirection.LeftToRight
        _stats.WrapContents = False
        _stats.Padding = New Padding(Theme.S(32), Theme.S(4), 0, 0)

        For Each c As StatCard In New StatCard() {_cardNames, _cardItems, _cardCats}
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            _stats.Controls.Add(c)
        Next
        _cardNames.Caption = "Products"
        _cardItems.Caption = "Sizes in total"
        _cardCats.Caption = "Categories"
    End Sub

    Private Sub AddColumn(name As String, headerText As String, dataProp As String, weight As Single)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = name
        c.HeaderText = headerText
        c.DataPropertyName = dataProp
        c.FillWeight = weight
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        _grid.Columns.Add(c)
    End Sub

    Private Sub BuildGrid()
        _listHost.Dock = DockStyle.Fill
        _listHost.BackColor = Color.White
        _listHost.Padding = New Padding(Theme.S(32), Theme.S(4), Theme.S(16), Theme.S(16))

        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 60)
        AddColumn("product", "PRODUCT", "name", 36)
        AddColumn("size", "SIZE", "measurement", 20)
        AddColumn("barcode", "BARCODE", "barcode", 26)
        AddColumn("stock", "IN STOCK", "qty_on_hand", 18)

        AddHandler _grid.CellPainting, AddressOf OnCellPainting
        AddHandler _grid.SelectionChanged, AddressOf OnGridSelectionChanged
        AddHandler _grid.CellDoubleClick, AddressOf OnGridDoubleClick

        _empty.Dock = DockStyle.Fill
        _empty.TextAlign = ContentAlignment.MiddleCenter
        _empty.Font = Theme.FontHeading
        _empty.ForeColor = Theme.TextMuted
        _empty.BackColor = Color.White
        _empty.Visible = False

        _listHost.Controls.Add(_grid)
        _listHost.Controls.Add(_empty)
    End Sub

    Private Sub BuildDetailsTop()
        _details.Dock = DockStyle.Right
        _details.Width = DetailsWidth
        _details.BackColor = Color.FromArgb(251, 249, 249)
        DetailsLayout.Setup(_details, _scroll, _bar, New Padding(Theme.S(28), Theme.S(22), Theme.S(12), Theme.S(16)))
        AddHandler _scroll.Paint, AddressOf OnDetailsPaint

        _avatar.Size = New Size(Theme.S(96), Theme.S(96))
        _avatar.Margin = New Padding((ContentW - Theme.S(96)) \ 2, 0, 0, Theme.S(10))
        _scroll.Controls.Add(_avatar)

        _dName.AutoSize = False
        _dName.Size = New Size(ContentW, Theme.S(56))
        _dName.TextAlign = ContentAlignment.TopCenter
        _dName.Font = BigNameFont
        _dName.ForeColor = Theme.TextDark
        _dName.Margin = New Padding(0)
        _scroll.Controls.Add(_dName)

        _dCategory.AutoSize = False
        _dCategory.Size = New Size(ContentW, Theme.S(22))
        _dCategory.TextAlign = ContentAlignment.MiddleCenter
        _dCategory.Font = Theme.FontBase
        _dCategory.ForeColor = Theme.TextMuted
        _dCategory.Margin = New Padding(0, 0, 0, Theme.S(6))
        _scroll.Controls.Add(_dCategory)
    End Sub

    Private Function BuildRow(caption As String, valueHeight As Integer) As Label
        Dim cap As New Label()
        cap.AutoSize = False
        cap.Size = New Size(ContentW, Theme.S(18))
        cap.Font = Theme.FontCaption
        cap.ForeColor = Theme.TextMuted
        cap.Text = caption
        cap.Margin = New Padding(0, Theme.S(12), 0, 0)
        _scroll.Controls.Add(cap)

        Dim v As New Label()
        v.AutoSize = False
        v.Size = New Size(ContentW, valueHeight)
        v.Font = Theme.FontBase
        v.ForeColor = Theme.TextDark
        v.Margin = New Padding(0)
        _scroll.Controls.Add(v)
        Return v
    End Function

    Private Sub BuildActions()
        _btnEdit.Text = "Edit product"
        _btnSize.Text = "Add another size"
        _btnSize.MakeSecondary()
        _btnPrintCode.Text = "Print barcode"
        _btnPrintCode.MakeSecondary()
        _btnDelete.Text = "Delete product"
        _btnDelete.MakeDanger()

        AddHandler _btnEdit.Click, AddressOf OnEdit
        AddHandler _btnSize.Click, AddressOf OnAddSize
        AddHandler _btnPrintCode.Click, AddressOf OnPrintBarcode
        AddHandler _btnDelete.Click, AddressOf OnDelete

        Dim first As Boolean = True
        For Each b As MaroonButton In New MaroonButton() {_btnEdit, _btnPrintCode, _btnSize, _btnDelete}
            b.Size = New Size(ContentW, Theme.S(40))
            b.Margin = New Padding(0, If(first, 0, Theme.S(8)), 0, 0)
            first = False
            _bar.Controls.Add(b)
        Next
    End Sub

    ' ------------------------------------------------------------------ data
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadFilter()
        LoadProducts()
    End Sub

    Private Sub OnSearchChanged()
        LoadProducts()
    End Sub

    Private Sub OnFilterChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        LoadProducts()
    End Sub

    Private Function CurrentCategoryId() As Integer
        Dim i As Integer = _filter.SelectedIndex
        If i <= 0 OrElse i >= _filterIds.Count Then Return 0
        Return _filterIds(i)
    End Function

    Private Sub LoadFilter()
        Dim keep As Integer = CurrentCategoryId()
        _loading = True
        Try
            _filter.Items.Clear()
            _filterIds.Clear()
            _filter.Items.Add("All categories")
            _filterIds.Add(0)
            Dim selectIndex As Integer = 0
            For Each r As DataRow In CategoryRepository.GetAll().Rows
                _filter.Items.Add(r("name").ToString())
                _filterIds.Add(Convert.ToInt32(r("id")))
                If Convert.ToInt32(r("id")) = keep Then selectIndex = _filter.Items.Count - 1
            Next
            _filter.SelectedIndex = selectIndex
        Catch
            If _filter.Items.Count = 0 Then
                _filter.Items.Add("All categories")
                _filterIds.Add(0)
                _filter.SelectedIndex = 0
            End If
        Finally
            _loading = False
        End Try
    End Sub

    Private Sub LoadProducts()
        If _loading Then Return
        _loading = True
        Try
            Dim keep As Integer = _selectedId
            Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex

            Dim dt As DataTable = ProductRepository.GetProducts(_search.Query, CurrentCategoryId())
            _grid.DataSource = dt

            Dim counts As ProductCounts = ProductRepository.GetCounts()
            _cardNames.Value = counts.Names.ToString()
            _cardItems.Value = counts.Items.ToString()
            _cardCats.Value = counts.Categories.ToString()

            If dt.Rows.Count = 0 Then
                If _search.Query.Length > 0 OrElse CurrentCategoryId() > 0 Then
                    _empty.Text = "No products match your search."
                ElseIf counts.Categories = 0 Then
                    _empty.Text = "Start with the Categories button, then Add product."
                Else
                    _empty.Text = "No products yet. Click Add product."
                End If
                _empty.Visible = True
                _empty.BringToFront()
            Else
                _empty.Visible = False
            End If

            _grid.ClearSelection()
            Dim target As DataGridViewRow = Nothing
            For Each r As DataGridViewRow In _grid.Rows
                Dim drv As DataRowView = TryCast(r.DataBoundItem, DataRowView)
                If drv IsNot Nothing AndAlso Convert.ToInt32(drv("id")) = keep Then
                    target = r
                    Exit For
                End If
            Next
            If target Is Nothing AndAlso _grid.Rows.Count > 0 Then target = _grid.Rows(0)
            If target IsNot Nothing Then
                target.Selected = True
                _grid.CurrentCell = target.Cells(0)
            End If
            If scroll >= 0 AndAlso scroll < _grid.Rows.Count Then _grid.FirstDisplayedScrollingRowIndex = scroll

        Catch ex As Exception
            _empty.Text = "Could not load products: " & ex.Message
            _empty.Visible = True
            _empty.BringToFront()
        Finally
            _loading = False
        End Try
        ShowDetails()
    End Sub

    ' ------------------------------------------------------------------ grid painting
    Private Sub OnCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        If colName <> "product" AndAlso colName <> "size" AndAlso colName <> "barcode" AndAlso colName <> "stock" Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim c As Rectangle = e.CellBounds
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding

        Select Case colName
            Case "product"
                Dim nm As String = row("name").ToString()
                Dim circle As New Rectangle(c.X + Theme.S(12), c.Y + (c.Height - Theme.S(38)) \ 2, Theme.S(38), Theme.S(38))
                Avatar.PaintInitials(e.Graphics, circle, nm, 10.0F)
                Dim textX As Integer = circle.Right + Theme.S(12)
                Dim textW As Integer = Math.Max(Theme.S(10), c.Right - textX - Theme.S(8))
                TextRenderer.DrawText(e.Graphics, nm, NameFont, New Rectangle(textX, c.Y + 11, textW, 20), Theme.TextDark,
                                      TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)
                TextRenderer.DrawText(e.Graphics, row("category").ToString(), Theme.FontSmall, New Rectangle(textX, c.Y + 32, textW, 18),
                                      Theme.TextMuted, TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)

            Case "size"
                Dim m As String = row("measurement").ToString()
                TextRenderer.DrawText(e.Graphics, If(m.Length = 0, "-", m), NameFont,
                                      New Rectangle(c.X + Theme.S(10), c.Y, c.Width - Theme.S(14), c.Height), Theme.TextDark, flags)

            Case "barcode"
                TextRenderer.DrawText(e.Graphics, row("barcode").ToString(), CodeFont,
                                      New Rectangle(c.X + Theme.S(10), c.Y, c.Width - Theme.S(14), c.Height), Theme.TextMuted, flags)

            Case "stock"
                Dim qty As Integer = Convert.ToInt32(row("qty_on_hand"))
                Dim limit As Integer = Convert.ToInt32(row("low_stock_threshold"))
                Dim low As Boolean = (limit > 0 AndAlso qty <= limit)
                TextRenderer.DrawText(e.Graphics, qty.ToString() & If(low, "  (low)", ""), NameFont,
                                      New Rectangle(c.X + Theme.S(10), c.Y, c.Width - Theme.S(14), c.Height), If(low, Theme.AlertRed, Theme.TextDark), flags)
        End Select
        e.Handled = True
    End Sub

    Private Sub OnGridSelectionChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        ShowDetails()
    End Sub

    Private Sub OnGridDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then OnEdit(sender, EventArgs.Empty)
    End Sub

    Private Function SelectedRow() As DataRowView
        If _grid.SelectedRows.Count = 0 Then Return Nothing
        Return TryCast(_grid.SelectedRows(0).DataBoundItem, DataRowView)
    End Function

    Private Sub ShowDetails()
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then
            _selectedId = 0
            _details.Visible = False
            Return
        End If
        _details.Visible = True

        _selectedId = Convert.ToInt32(row("id"))
        Dim nm As String = row("name").ToString()
        _dName.Text = nm
        _avatar.DisplayName = nm
        _dCategory.Text = row("category").ToString()

        Dim m As String = row("measurement").ToString()
        _vSize.Text = If(m.Length = 0, "-", m)
        _vBarcode.Text = row("barcode").ToString()
        _vStock.Text = Convert.ToInt32(row("qty_on_hand")).ToString()
        Dim limit As Integer = Convert.ToInt32(row("low_stock_threshold"))
        _vThreshold.Text = If(limit > 0, limit.ToString(), "Not set (Settings > Stock Threshold)")
        _vCreated.Text = Convert.ToDateTime(row("created_at")).ToString("MMM d, yyyy")
    End Sub

    Private Sub OnDetailsPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _details.Height)
        End Using
    End Sub

    ' ------------------------------------------------------------------ actions
    Private Function HasCategories() As Boolean
        Try
            If CategoryRepository.GetAll().Rows.Count > 0 Then Return True
        Catch ex As Exception
            Notify(Me, "Products", "Could not reach the database: " & ex.Message, MessageBoxIcon.Error)
            Return False
        End Try
        Notify(Me, "Products", "Create a category first." & Environment.NewLine & "Click the Categories button.", MessageBoxIcon.Information)
        Return False
    End Function

    Private Sub OnCategories(sender As Object, e As EventArgs)
        Using dlg As New CategoriesDialog()
            dlg.ShowDialog(OwnerOf(Me))
        End Using
        LoadFilter()
        LoadProducts()
    End Sub

    Private Sub OnAdd(sender As Object, e As EventArgs)
        If Not HasCategories() Then Return
        Using dlg As New ProductEditForm()
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadProducts()
        End Using
    End Sub

    Private Sub OnAddSize(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Using dlg As New ProductEditForm(row("name").ToString(), Convert.ToInt32(row("category_id")))
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadProducts()
        End Using
    End Sub

    Private Sub OnPrintBarcode(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Using dlg As New PrintBarcodeDialog(row("name").ToString(), row("measurement").ToString(), row("barcode").ToString())
            dlg.ShowDialog(OwnerOf(Me))
        End Using
    End Sub

    Private Sub OnEdit(sender As Object, e As EventArgs)
        If _selectedId <= 0 Then Return
        Using dlg As New ProductEditForm(_selectedId)
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadProducts()
        End Using
    End Sub

    Private Sub OnDelete(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim label As String = row("name").ToString() & If(row("measurement").ToString().Length > 0, " " & row("measurement").ToString(), "")

        If Not Confirm(Me, "Products", "Delete " & label & "?" & Environment.NewLine & Environment.NewLine &
                       "This cannot be undone.", MessageBoxIcon.Warning) Then Return

        Try
            Select Case ProductRepository.DeleteProduct(id)
                Case SaveResult.Ok
                    AuditLog.Write("DELETE", "Products", "Deleted product '" & label & "'")
                Case SaveResult.InUse
                    Notify(Me, "Products", "This product already has deliveries or sales, so it cannot be deleted." & Environment.NewLine &
                           "Its history must stay in the reports.", MessageBoxIcon.Information)
                Case Else
                    ' already gone
            End Select
        Catch ex As Exception
            Notify(Me, "Products", "Could not delete the product: " & ex.Message, MessageBoxIcon.Error)
        End Try
        _selectedId = 0
        LoadProducts()
    End Sub

End Class
