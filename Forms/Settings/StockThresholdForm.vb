Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Settings > Stock Threshold. For each item: at what quantity should the red low-stock warning appear?
''' (0 = no warning). Superadmin only.
''' </summary>
Public Class StockThresholdForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly DetailsWidth As Integer = Theme.S(340)
    Private Shared ReadOnly ContentW As Integer = Theme.S(268)

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly BigNameFont As Font = Theme.MakeFont("Segoe UI Semibold", 14.0F)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()

    Private ReadOnly _bar As New Panel()
    Private ReadOnly _cat As New ComboBox()
    Private ReadOnly _search As New SearchBox("Search name, size or barcode...")

    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()

    Private ReadOnly _details As New FlowLayoutPanel()
    Private ReadOnly _dName As New Label()
    Private ReadOnly _dSub As New Label()
    Private ReadOnly _dStock As New Label()
    Private ReadOnly _value As New InputField("Warn me when stock is this low (0 = no warning)", InputKind.WholeNumber, ContentW, True)
    Private ReadOnly _allSizes As New CheckBox()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _msg As New Label()

    Private ReadOnly _catIds As New List(Of Integer)()
    Private _loading As Boolean
    Private _selectedId As Integer

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildBar()
        BuildGrid()
        BuildDetails()

        Controls.Add(_listHost)
        Controls.Add(_details)
        Controls.Add(_bar)
        Controls.Add(_header)
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"products", "categories"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadCategories()
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
        _title.Text = "Stock Threshold"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "When an item drops to its threshold, the red low-stock line appears at the top of the system"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)
    End Sub

    Private Sub BuildBar()
        _bar.Dock = DockStyle.Top
        _bar.Height = Theme.S(58)
        _bar.BackColor = Color.White

        _cat.DropDownStyle = ComboBoxStyle.DropDownList
        _cat.FlatStyle = FlatStyle.Flat
        _cat.Font = Theme.FontBase
        _cat.BackColor = Color.White
        _cat.Width = Theme.S(200)
        _cat.Location = New Point(Theme.S(32), Theme.S(14))
        AddHandler _cat.SelectedIndexChanged, AddressOf OnCategoryChanged

        _search.Size = New Size(Theme.S(300), Theme.S(40))
        _search.Location = New Point(Theme.S(32) + Theme.S(200) + Theme.S(12), Theme.S(8))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _bar.Controls.Add(_cat)
        _bar.Controls.Add(_search)
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
        _listHost.Padding = New Padding(Theme.S(32), 0, Theme.S(16), Theme.S(16))

        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 56)
        AddColumn("item", "ITEM", "name", 36, False)
        AddColumn("size", "SIZE", "measurement", 18, False)
        AddColumn("stock", "IN STOCK", "qty_on_hand", 14, True)
        AddColumn("alert", "WARN AT", "low_stock_threshold", 16, True)
        AddHandler _grid.CellPainting, AddressOf OnCellPainting
        AddHandler _grid.CellFormatting, AddressOf OnCellFormatting
        AddHandler _grid.SelectionChanged, AddressOf OnGridSelectionChanged

        _empty.Dock = DockStyle.Fill
        _empty.TextAlign = ContentAlignment.MiddleCenter
        _empty.Font = Theme.FontHeading
        _empty.ForeColor = Theme.TextMuted
        _empty.BackColor = Color.White
        _empty.Visible = False

        _listHost.Controls.Add(_grid)
        _listHost.Controls.Add(_empty)
    End Sub

    Private Sub BuildDetails()
        _details.Dock = DockStyle.Right
        _details.Width = DetailsWidth
        _details.BackColor = Color.FromArgb(251, 249, 249)
        _details.FlowDirection = FlowDirection.TopDown
        _details.WrapContents = False
        _details.AutoScroll = True
        _details.Padding = New Padding(Theme.S(28), Theme.S(26), Theme.S(12), Theme.S(16))
        AddHandler _details.Paint, AddressOf OnDetailsPaint

        _dName.AutoSize = False
        _dName.Size = New Size(ContentW, Theme.S(56))
        _dName.Font = BigNameFont
        _dName.ForeColor = Theme.TextDark
        _dName.Margin = New Padding(0)
        _details.Controls.Add(_dName)

        _dSub.AutoSize = False
        _dSub.Size = New Size(ContentW, Theme.S(22))
        _dSub.Font = Theme.FontBase
        _dSub.ForeColor = Theme.TextMuted
        _dSub.Margin = New Padding(0, 0, 0, Theme.S(4))
        _details.Controls.Add(_dSub)

        _dStock.AutoSize = False
        _dStock.Size = New Size(ContentW, Theme.S(26))
        _dStock.Font = NameFont
        _dStock.ForeColor = Theme.TextDark
        _dStock.Margin = New Padding(0, 0, 0, Theme.S(16))
        _details.Controls.Add(_dStock)

        _value.MinNumber = 0D
        _value.MaxNumber = 999999D
        _value.Margin = New Padding(0, 0, 0, Theme.S(6))
        _value.SwallowEnter = True
        AddHandler _value.EnterPressed, AddressOf OnSave
        _details.Controls.Add(_value)

        _allSizes.Text = "Use this for every size of this product"
        _allSizes.AutoSize = False
        _allSizes.Size = New Size(ContentW, Theme.S(26))
        _allSizes.Font = Theme.FontSmall
        _allSizes.ForeColor = Theme.TextMuted
        _allSizes.Margin = New Padding(0, Theme.S(4), 0, Theme.S(10))
        _details.Controls.Add(_allSizes)

        _save.Text = "Save"
        _save.Size = New Size(ContentW, Theme.S(42))
        _save.Margin = New Padding(0)
        AddHandler _save.Click, AddressOf OnSave
        _details.Controls.Add(_save)

        _msg.AutoSize = False
        _msg.Size = New Size(ContentW, Theme.S(40))
        _msg.Font = Theme.FontSmall
        _msg.Margin = New Padding(0, Theme.S(8), 0, 0)
        _details.Controls.Add(_msg)
    End Sub

    Private Sub OnDetailsPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _details.Height)
        End Using
    End Sub

    ' ------------------------------------------------------------------ data
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadCategories()
        LoadProducts()
    End Sub

    Private Function CurrentCategoryId() As Integer
        Dim i As Integer = _cat.SelectedIndex
        If i <= 0 OrElse i >= _catIds.Count Then Return 0
        Return _catIds(i)
    End Function

    Private Sub LoadCategories()
        Dim keep As Integer = CurrentCategoryId()
        _loading = True
        Try
            _cat.Items.Clear()
            _catIds.Clear()
            _cat.Items.Add("All categories")
            _catIds.Add(0)
            Dim idx As Integer = 0
            For Each r As DataRow In CategoryRepository.GetAll().Rows
                _cat.Items.Add(r("name").ToString())
                _catIds.Add(Convert.ToInt32(r("id")))
                If Convert.ToInt32(r("id")) = keep Then idx = _cat.Items.Count - 1
            Next
            _cat.SelectedIndex = idx
        Catch
            If _cat.Items.Count = 0 Then
                _cat.Items.Add("All categories")
                _catIds.Add(0)
                _cat.SelectedIndex = 0
            End If
        Finally
            _loading = False
        End Try
    End Sub

    Private Sub OnCategoryChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        LoadProducts()
    End Sub

    Private Sub OnSearchChanged()
        LoadProducts()
    End Sub

    Private Sub LoadProducts()
        If _loading Then Return
        _loading = True
        Try
            Dim keep As Integer = _selectedId
            Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex
            Dim dt As DataTable = ProductRepository.GetProducts(_search.Query, CurrentCategoryId())
            _grid.DataSource = dt

            _empty.Visible = (dt.Rows.Count = 0)
            If dt.Rows.Count = 0 Then
                _empty.Text = "No products to show."
                _empty.BringToFront()
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
            _empty.Text = "Could not load the products: " & ex.Message
            _empty.Visible = True
            _empty.BringToFront()
        Finally
            _loading = False
        End Try
        ShowDetails(False)
    End Sub

    ' ------------------------------------------------------------------ list
    Private Sub OnCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        If _grid.Columns(e.ColumnIndex).Name <> "item" Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using

        Dim c As Rectangle = e.CellBounds
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
        TextRenderer.DrawText(e.Graphics, row("name").ToString(), NameFont, New Rectangle(c.X + 10, c.Y + 10, c.Width - 14, 20), Theme.TextDark, flags)
        TextRenderer.DrawText(e.Graphics, row("category").ToString(), Theme.FontSmall, New Rectangle(c.X + 10, c.Y + 31, c.Width - 14, 18), Theme.TextMuted, flags)
        e.Handled = True
    End Sub

    Private Sub OnCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Select Case _grid.Columns(e.ColumnIndex).Name
            Case "size"
                If e.Value Is Nothing OrElse e.Value.ToString().Length = 0 Then
                    e.Value = "-"
                    e.FormattingApplied = True
                End If
            Case "alert"
                e.Value = If(Convert.ToInt32(e.Value) = 0, "not set", e.Value.ToString())
                e.FormattingApplied = True
        End Select
    End Sub

    Private Sub OnGridSelectionChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        ShowDetails(True)
    End Sub

    Private Function SelectedRow() As DataRowView
        If _grid.SelectedRows.Count = 0 Then Return Nothing
        Return TryCast(_grid.SelectedRows(0).DataBoundItem, DataRowView)
    End Function

    ''' <summary>resetEditor = True when the person picked another item (so the box shows its current level).</summary>
    Private Sub ShowDetails(resetEditor As Boolean)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then
            _selectedId = 0
            _details.Visible = False
            Return
        End If
        _details.Visible = True

        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim changed As Boolean = (id <> _selectedId)
        _selectedId = id

        _dName.Text = row("name").ToString()
        Dim m As String = row("measurement").ToString()
        _dSub.Text = row("category").ToString() & If(m.Length > 0, "   |   " & m, "")
        _dStock.Text = "In stock now: " & Convert.ToInt32(row("qty_on_hand")).ToString()

        If changed OrElse resetEditor Then
            _value.Value = Convert.ToInt32(row("low_stock_threshold")).ToString()
            _allSizes.Checked = False
            _msg.Text = ""
        End If
    End Sub

    ' ------------------------------------------------------------------ save
    Private Sub OnSave(sender As Object, e As EventArgs)
        OnSave()
    End Sub

    Private Sub OnSave()
        If _selectedId <= 0 Then Return
        _msg.Text = ""
        Dim problem As String = InputField.CheckAll(_value)
        If problem.Length > 0 Then
            _msg.ForeColor = Theme.AlertRed
            _msg.Text = problem
            Return
        End If

        Dim v As Integer = Integer.Parse(_value.Value.Trim())
        Try
            DeliveryHelpers.SaveThreshold(_selectedId, v, _allSizes.Checked, _dName.Text & " " & _dSub.Text)
            _msg.ForeColor = Color.FromArgb(22, 130, 60)
            _msg.Text = If(v = 0, "Warning turned off for this item.", "Saved. The warning appears at " & v.ToString() & " or less.")
            _allSizes.Checked = False
            LoadProducts()
        Catch ex As Exception
            _msg.ForeColor = Theme.AlertRed
            _msg.Text = "Could not save: " & ex.Message
        End Try
    End Sub

End Class

''' <summary>Small shared helper so the threshold write and its audit entry stay together.</summary>
Public Module DeliveryHelpers

    Public Sub SaveThreshold(productId As Integer, value As Integer, allSizes As Boolean, label As String)
        LowStockAlerts.SetThreshold(productId, value, allSizes)
        AuditLog.Write("EDIT", "Stock Threshold",
                       "Set the low-stock warning of " & label.Trim() & " to " & value.ToString() & If(allSizes, " (all sizes)", ""))
    End Sub

End Module
