Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>
''' Transactions > Delivery Management. Pick a supplier for a new batch, scan or pick each product,
''' enter quantity and prices, check the batch (totals and expected profit), then send it to inventory.
''' </summary>
Public Class DeliveryForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly LeftWidth As Integer = Theme.S(340)

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly BigFont As Font = Theme.MakeFont("Segoe UI Semibold", 18.0F)
    Private Shared ReadOnly MidFont As Font = Theme.MakeFont("Segoe UI Semibold", 12.0F)

    ' ---- header
    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _newBatch As New MaroonButton()

    ' ---- left: batches
    Private ReadOnly _body As New Panel()
    Private ReadOnly _left As New Panel()
    Private ReadOnly _leftTop As New Panel()
    Private ReadOnly _filter As New ComboBox()
    Private ReadOnly _batchGrid As New DataGridView()
    Private ReadOnly _batchEmpty As New Label()

    ' ---- right: the open batch
    Private ReadOnly _main As New Panel()
    Private ReadOnly _placeholder As New Label()
    Private ReadOnly _batchPanel As New Panel()

    Private ReadOnly _info As New Panel()
    Private ReadOnly _lblBatchNo As New Label()
    Private ReadOnly _pillStatus As New PillLabel()
    Private ReadOnly _lblSupplier As New Label()
    Private ReadOnly _lblMeta As New Label()
    Private ReadOnly _btnEditBatch As New MaroonButton()
    Private ReadOnly _btnDeleteBatch As New MaroonButton()

    Private ReadOnly _entry As New Panel()
    Private ReadOnly _scan As New InputField("Scan a barcode or type a product name", InputKind.Text, 430, False)
    Private ReadOnly _browse As New MaroonButton()
    Private ReadOnly _lblItem As New Label()
    Private ReadOnly _lblItemSub As New Label()
    Private ReadOnly _qty As New InputField("Quantity (pieces)", InputKind.WholeNumber, 130, True)
    Private ReadOnly _costMode As New ChoiceField("Cost price is", 170)
    Private ReadOnly _packSize As New InputField("Pieces in 1 pack", InputKind.WholeNumber, 140, True)
    Private ReadOnly _cost As New InputField("Cost price (per piece)", InputKind.Money, 170, True)
    Private ReadOnly _price As New InputField("Selling price per piece (VAT incl.)", InputKind.Money, 250, True)
    Private ReadOnly _lblPreview As New Label()
    Private ReadOnly _btnAdd As New MaroonButton()

    Private ReadOnly _grid As New DataGridView()

    Private ReadOnly _totals As New Panel()
    Private ReadOnly _lblSummary As New Label()
    Private ReadOnly _btnRemove As New MaroonButton()
    Private ReadOnly _btnSend As New MaroonButton()

    Private _items As DataTable
    Private _loading As Boolean
    Private _batchId As Integer
    Private _isDraft As Boolean
    Private _productId As Integer
    Private _productName As String = ""
    Private _company As String = ""
    Private _hasRef As Boolean
    Private _refCost As Decimal
    Private _refInfo As String = ""
    Private _refMode As String = "piece"
    Private _refPack As Integer = 1
    Private _refEntered As Decimal
    Private _layingOut As Boolean
    Private _costUnlocked As Boolean
    Private ReadOnly _lblLock As New Label()
    Private ReadOnly _linkUnlock As New LinkLabel()

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildLeft()
        BuildBatchPanel()

        _main.Dock = DockStyle.Fill
        _main.BackColor = Color.White
        _placeholder.Dock = DockStyle.Fill
        _placeholder.TextAlign = ContentAlignment.MiddleCenter
        _placeholder.Font = Theme.FontHeading
        _placeholder.ForeColor = Theme.TextMuted
        _placeholder.Text = "Select a batch on the left, or start a new one."
        _batchPanel.Dock = DockStyle.Fill
        _main.Controls.Add(_batchPanel)
        _main.Controls.Add(_placeholder)

        _body.Dock = DockStyle.Fill
        _body.Controls.Add(_main)
        _body.Controls.Add(_left)

        Controls.Add(_body)
        Controls.Add(_header)

        ClearEntry()
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"deliveries", "delivery_items", "suppliers"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadBatches()
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Delivery Management"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "Receive a supplier's delivery: build a batch, check it, then send it to inventory"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _newBatch.Text = "+  New batch"
        _newBatch.Size = New Size(Theme.S(150), Theme.S(40))
        AddHandler _newBatch.Click, AddressOf OnNewBatch

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)
        _header.Controls.Add(_newBatch)
        AddHandler _header.Resize, AddressOf OnHeaderResize
        OnHeaderResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnHeaderResize(sender As Object, e As EventArgs)
        _newBatch.Location = New Point(Math.Max(Theme.S(300), _header.ClientSize.Width - Theme.S(32) - _newBatch.Width), Theme.S(30))
    End Sub

    Private Sub BuildLeft()
        _left.Dock = DockStyle.Left
        _left.Width = LeftWidth
        _left.BackColor = Color.FromArgb(251, 249, 249)
        _left.Padding = New Padding(Theme.S(24), 0, 0, Theme.S(12))
        AddHandler _left.Paint, AddressOf OnLeftPaint

        _leftTop.Dock = DockStyle.Top
        _leftTop.Height = Theme.S(52)
        _leftTop.BackColor = _left.BackColor

        _filter.DropDownStyle = ComboBoxStyle.DropDownList
        _filter.FlatStyle = FlatStyle.Flat
        _filter.Font = Theme.FontBase
        _filter.Width = Theme.S(200)
        _filter.Location = New Point(0, Theme.S(10))
        _filter.Items.AddRange(New Object() {"All batches", "Drafts only", "Delivered only"})
        _filter.SelectedIndex = 0
        AddHandler _filter.SelectedIndexChanged, AddressOf OnFilterChanged
        _leftTop.Controls.Add(_filter)

        _batchGrid.Dock = DockStyle.Fill
        GridStyle.Apply(_batchGrid, 82)
        _batchGrid.ColumnHeadersVisible = False
        _batchGrid.BackgroundColor = _left.BackColor
        _batchGrid.DefaultCellStyle.BackColor = _left.BackColor
        _batchGrid.DefaultCellStyle.SelectionBackColor = Theme.MaroonTint
        _batchGrid.DefaultCellStyle.Padding = New Padding(0)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = "batch"
        c.DataPropertyName = "batch_no"
        c.FillWeight = 100
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        _batchGrid.Columns.Add(c)
        AddHandler _batchGrid.CellPainting, AddressOf OnBatchCellPainting
        AddHandler _batchGrid.SelectionChanged, AddressOf OnBatchSelectionChanged

        _batchEmpty.Dock = DockStyle.Fill
        _batchEmpty.TextAlign = ContentAlignment.MiddleCenter
        _batchEmpty.Font = Theme.FontBase
        _batchEmpty.ForeColor = Theme.TextMuted
        _batchEmpty.BackColor = _left.BackColor
        _batchEmpty.Text = "No batches yet." & Environment.NewLine & "Click New batch."
        _batchEmpty.Visible = False

        _left.Controls.Add(_batchGrid)
        _left.Controls.Add(_batchEmpty)
        _left.Controls.Add(_leftTop)
    End Sub

    Private Sub OnLeftPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, _left.Width - 1, 0, _left.Width - 1, _left.Height)
        End Using
    End Sub

    Private Sub AddGridColumn(name As String, headerText As String, dataProp As String, weight As Single, rightAlign As Boolean)
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

    Private Sub BuildBatchPanel()
        ' ---- info strip
        _info.Dock = DockStyle.Top
        _info.Height = Theme.S(104)
        _info.BackColor = Color.White

        _lblBatchNo.AutoSize = True
        _lblBatchNo.Font = BigFont
        _lblBatchNo.ForeColor = Theme.Maroon
        _lblBatchNo.Location = New Point(Theme.S(24), Theme.S(12))

        _lblSupplier.AutoSize = True
        _lblSupplier.Font = MidFont
        _lblSupplier.ForeColor = Theme.TextDark
        _lblSupplier.Location = New Point(Theme.S(26), Theme.S(50))

        _lblMeta.AutoSize = True
        _lblMeta.Font = Theme.FontSmall
        _lblMeta.ForeColor = Theme.TextMuted
        _lblMeta.Location = New Point(Theme.S(26), Theme.S(76))

        _btnEditBatch.Text = "Edit details"
        _btnEditBatch.MakeSecondary()
        _btnEditBatch.Size = New Size(Theme.S(120), Theme.S(36))
        AddHandler _btnEditBatch.Click, AddressOf OnEditBatch

        _btnDeleteBatch.Text = "Delete batch"
        _btnDeleteBatch.MakeDanger()
        _btnDeleteBatch.Size = New Size(Theme.S(120), Theme.S(36))
        AddHandler _btnDeleteBatch.Click, AddressOf OnDeleteBatch

        _info.Controls.Add(_lblBatchNo)
        _info.Controls.Add(_pillStatus)
        _info.Controls.Add(_lblSupplier)
        _info.Controls.Add(_lblMeta)
        _info.Controls.Add(_btnEditBatch)
        _info.Controls.Add(_btnDeleteBatch)
        AddHandler _info.Resize, AddressOf OnInfoResize

        ' ---- entry bar
        _entry.Dock = DockStyle.Top
        _entry.Height = Theme.S(340)
        _entry.BackColor = Color.FromArgb(251, 249, 249)
        AddHandler _entry.Resize, AddressOf OnEntryResize
        AddHandler _entry.Paint, AddressOf OnEntryPaint

        _scan.Location = New Point(Theme.S(24), Theme.S(8))
        _scan.SwallowEnter = True
        AddHandler _scan.EnterPressed, AddressOf OnScanEnter

        _browse.Text = "Browse products"
        _browse.MakeSecondary()
        _browse.Size = New Size(Theme.S(160), Theme.S(42))
        _browse.Location = New Point(Theme.S(24) + Theme.S(430) + Theme.S(12), Theme.S(30))
        AddHandler _browse.Click, AddressOf OnBrowse

        _lblItem.AutoSize = False
        _lblItem.Size = New Size(Theme.S(700), Theme.S(26))
        _lblItem.Font = MidFont
        _lblItem.ForeColor = Theme.TextDark
        _lblItem.Location = New Point(Theme.S(24), Theme.S(88))

        _lblItemSub.AutoSize = False
        _lblItemSub.Size = New Size(Theme.S(900), Theme.S(20))
        _lblItemSub.Font = Theme.FontSmall
        _lblItemSub.ForeColor = Theme.TextMuted
        _lblItemSub.Location = New Point(Theme.S(24), Theme.S(116))

        _costMode.SetChoices(New String() {"Per piece", "Per pack / box"})
        _costMode.Value = "Per piece"
        AddHandler _costMode.ValueChanged, AddressOf OnCostModeChanged
        _packSize.MinNumber = 2D
        _packSize.MaxNumber = 100000D
        _packSize.Visible = False
        AddHandler _packSize.ValueChanged, AddressOf OnEntryChanged
        _packSize.SwallowEnter = True
        AddHandler _packSize.EnterPressed, AddressOf OnFieldEnter
        _qty.MinNumber = 1D
        _qty.MaxNumber = 999999D
        _cost.MinNumber = 0.01D
        _cost.MaxNumber = 9999999.99D
        _price.MinNumber = 0.01D
        _price.MaxNumber = 9999999.99D
        AddHandler _qty.ValueChanged, AddressOf OnEntryChanged
        AddHandler _cost.ValueChanged, AddressOf OnEntryChanged
        AddHandler _price.ValueChanged, AddressOf OnEntryChanged
        _qty.SwallowEnter = True
        _cost.SwallowEnter = True
        _price.SwallowEnter = True
        AddHandler _qty.EnterPressed, AddressOf OnFieldEnter
        AddHandler _cost.EnterPressed, AddressOf OnFieldEnter
        AddHandler _price.EnterPressed, AddressOf OnAdd

        _lblPreview.AutoSize = False
        _lblPreview.Font = Theme.FontBase
        _lblPreview.ForeColor = Theme.TextMuted
        _lblPreview.Size = New Size(Theme.S(360), Theme.S(44))

        _btnAdd.Text = "Add to batch"
        _btnAdd.Size = New Size(Theme.S(170), Theme.S(44))
        AddHandler _btnAdd.Click, AddressOf OnAdd

        _entry.Controls.Add(_scan)
        _entry.Controls.Add(_browse)
        _entry.Controls.Add(_lblItem)
        _entry.Controls.Add(_lblItemSub)
        _entry.Controls.Add(_qty)
        _entry.Controls.Add(_costMode)
        _entry.Controls.Add(_packSize)
        _entry.Controls.Add(_cost)
        _entry.Controls.Add(_price)
        _entry.Controls.Add(_lblPreview)
        _entry.Controls.Add(_btnAdd)

        _lblLock.AutoSize = True
        _lblLock.Font = Theme.FontSmall
        _lblLock.ForeColor = Theme.TextMuted
        _lblLock.Location = New Point(Theme.S(24), Theme.S(230))
        _lblLock.Visible = False

        _linkUnlock.AutoSize = True
        _linkUnlock.Text = "Supplier's price changed? Unlock the cost price"
        _linkUnlock.Font = Theme.FontSmall
        _linkUnlock.LinkColor = Theme.Maroon
        _linkUnlock.ActiveLinkColor = Theme.MaroonDark
        _linkUnlock.VisitedLinkColor = Theme.Maroon
        _linkUnlock.LinkBehavior = LinkBehavior.HoverUnderline
        _linkUnlock.Cursor = Cursors.Hand
        _linkUnlock.Visible = False
        AddHandler _linkUnlock.LinkClicked, AddressOf OnUnlockCost

        _entry.Controls.Add(_lblLock)
        _entry.Controls.Add(_linkUnlock)

        ' ---- items grid
        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 56)
        AddGridColumn("item", "ITEM", "name", 30, False)
        AddGridColumn("size", "SIZE", "measurement", 13, False)
        AddGridColumn("qty", "PIECES", "qty", 8, True)
        AddGridColumn("cost", "COST / PIECE", "cost_price", 12, True)
        AddGridColumn("price", "SELLING PRICE", "selling_price", 13, True)
        AddGridColumn("line", "LINE COST", "line_cost", 13, True)
        AddGridColumn("profit", "PROFIT", "expected_profit", 12, True)
        AddGridColumn("remaining", "REMAINING", "qty_remaining", 10, True)
        AddHandler _grid.CellPainting, AddressOf OnItemCellPainting
        AddHandler _grid.CellFormatting, AddressOf OnItemCellFormatting
        AddHandler _grid.CellDoubleClick, AddressOf OnItemDoubleClick

        ' ---- totals
        _totals.Dock = DockStyle.Bottom
        _totals.Height = Theme.S(108)
        _totals.BackColor = Color.White
        AddHandler _totals.Resize, AddressOf OnTotalsResize
        AddHandler _totals.Paint, AddressOf OnTotalsPaint

        _lblSummary.AutoSize = False
        _lblSummary.Font = MidFont
        _lblSummary.ForeColor = Theme.TextDark
        _lblSummary.Location = New Point(Theme.S(24), Theme.S(12))
        _lblSummary.Size = New Size(Theme.S(900), Theme.S(30))

        _btnRemove.Text = "Remove selected line"
        _btnRemove.MakeDanger()
        _btnRemove.Size = New Size(Theme.S(200), Theme.S(40))
        _btnRemove.Location = New Point(Theme.S(24), Theme.S(54))
        AddHandler _btnRemove.Click, AddressOf OnRemove

        _btnSend.Text = "Send to inventory"
        _btnSend.Size = New Size(Theme.S(230), Theme.S(46))
        AddHandler _btnSend.Click, AddressOf OnSend

        _totals.Controls.Add(_lblSummary)
        _totals.Controls.Add(_btnRemove)
        _totals.Controls.Add(_btnSend)

        ' Fill first, then the bars (later = back = docks first = top-most)
        _batchPanel.Controls.Add(_grid)
        _batchPanel.Controls.Add(_totals)
        _batchPanel.Controls.Add(_entry)
        _batchPanel.Controls.Add(_info)
    End Sub

    Private Sub OnInfoResize(sender As Object, e As EventArgs)
        _btnDeleteBatch.Location = New Point(Math.Max(Theme.S(200), _info.ClientSize.Width - Theme.S(24) - _btnDeleteBatch.Width), Theme.S(14))
        _btnEditBatch.Location = New Point(Math.Max(Theme.S(100), _btnDeleteBatch.Left - Theme.S(10) - _btnEditBatch.Width), Theme.S(14))
    End Sub

    Private Sub OnEntryResize(sender As Object, e As EventArgs)
        LayoutEntry()
    End Sub

    ' Puts the entry boxes in rows that fit the width; on a narrow window they wrap to a second row.
    Private Sub LayoutEntry()
        If _layingOut Then Return
        _layingOut = True
        Try
            Dim w As Integer = _entry.ClientSize.Width
            If w <= 0 Then Return
            Dim gap As Integer = Theme.S(14)
            Dim rowH As Integer = Theme.S(78)
            Dim x As Integer = Theme.S(24)
            Dim y As Integer = Theme.S(148)
            Dim boxes As Control() = {_qty, _costMode, _packSize, _cost, _price}
            For Each b As Control In boxes
                If Not b.Visible Then Continue For
                If x > 24 AndAlso x + b.Width > w - 24 Then
                    x = 24
                    y += rowH
                End If
                b.Location = New Point(x, y)
                x += b.Width + gap
            Next

            ' the preview and the Add button: at the end of the row if there is room, otherwise on their own row
            If x + 160 + _btnAdd.Width > w - 16 Then
                x = 24
                y += rowH
            End If
            _btnAdd.Location = New Point(Math.Max(x + Theme.S(160), w - Theme.S(24) - _btnAdd.Width), y + Theme.S(22))
            _lblPreview.Location = New Point(x, y + Theme.S(12))
            _lblPreview.Width = Math.Max(Theme.S(140), _btnAdd.Left - x - Theme.S(14))
            _lblLock.Location = New Point(Theme.S(24), y + rowH)
            _linkUnlock.Location = New Point(_lblLock.Right + Theme.S(14), _lblLock.Top)

            Dim h As Integer = y + rowH + Theme.S(34)
            If _entry.Height <> h Then _entry.Height = h
        Finally
            _layingOut = False
        End Try
    End Sub

    Private Function IsPackMode() As Boolean
        Return _costMode.Value = "Per pack / box"
    End Function

    Private Sub UpdateModeUi()
        Dim pack As Boolean = IsPackMode()
        _packSize.Visible = pack
        _qty.Caption = If(pack, "Quantity (packs)", "Quantity (pieces)")
        _cost.Caption = If(pack, "Cost price (per pack)", "Cost price (per piece)")
        LayoutEntry()
        UpdatePreview()
    End Sub

    Private Sub OnCostModeChanged()
        UpdateModeUi()
    End Sub

    ' puts what was typed last time (mode, pieces per pack, cost) into the boxes
    Private Sub ShowCostEntry(mode As String, pack As Integer, entered As Decimal)
        _costMode.Value = If(mode = "pack", "Per pack / box", "Per piece")
        _packSize.Value = If(mode = "pack", pack.ToString(), "")
        _cost.Value = entered.ToString("0.##", CultureInfo.InvariantCulture)
    End Sub

    Private Shared Function DescribeCost(byPack As Boolean, pack As Integer, entered As Decimal, perPiece As Decimal) As String
        If byPack Then
            Return Currency.Format(entered) & " per pack of " & pack.ToString() & " (" & Currency.FormatCost(perPiece) & " per piece)"
        End If
        Return Currency.FormatCost(perPiece) & " per piece"
    End Function

    Private Sub OnEntryPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, _entry.Width, 0)
            e.Graphics.DrawLine(p, 0, _entry.Height - 1, _entry.Width, _entry.Height - 1)
        End Using
    End Sub

    Private Sub OnTotalsResize(sender As Object, e As EventArgs)
        _btnSend.Location = New Point(Math.Max(Theme.S(300), _totals.ClientSize.Width - Theme.S(24) - _btnSend.Width), Theme.S(50))
    End Sub

    Private Sub OnTotalsPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, _totals.Width, 0)
        End Using
    End Sub

    ' ------------------------------------------------------------------ loading
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadBatches()
    End Sub

    Private Function FilterKey() As String
        Select Case _filter.SelectedIndex
            Case 1
                Return "draft"
            Case 2
                Return "sent"
            Case Else
                Return "all"
        End Select
    End Function

    Private Sub OnFilterChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        LoadBatches()
    End Sub

    Private Sub LoadBatches()
        If _loading Then Return
        _loading = True
        Try
            Dim dt As DataTable = DeliveryRepository.GetBatches(FilterKey())
            _batchGrid.DataSource = dt
            _batchEmpty.Visible = (dt.Rows.Count = 0)
            If dt.Rows.Count = 0 Then _batchEmpty.BringToFront()

            _batchGrid.ClearSelection()
            Dim target As DataGridViewRow = Nothing
            For Each r As DataGridViewRow In _batchGrid.Rows
                Dim drv As DataRowView = TryCast(r.DataBoundItem, DataRowView)
                If drv IsNot Nothing AndAlso Convert.ToInt32(drv("id")) = _batchId Then
                    target = r
                    Exit For
                End If
            Next
            If target Is Nothing AndAlso _batchGrid.Rows.Count > 0 Then target = _batchGrid.Rows(0)

            If target IsNot Nothing Then
                target.Selected = True
                _batchGrid.CurrentCell = target.Cells(0)
                Dim sel As DataRowView = TryCast(target.DataBoundItem, DataRowView)
                Dim newId As Integer = If(sel Is Nothing, 0, Convert.ToInt32(sel("id")))
                If newId <> _batchId Then ClearEntry()
                _batchId = newId
            Else
                _batchId = 0
            End If
        Catch ex As Exception
            _batchEmpty.Text = "Could not load the batches: " & ex.Message
            _batchEmpty.Visible = True
            _batchEmpty.BringToFront()
        Finally
            _loading = False
        End Try
        ShowBatch()
    End Sub

    Private Sub OnBatchSelectionChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        If _batchGrid.SelectedRows.Count = 0 Then Return
        Dim drv As DataRowView = TryCast(_batchGrid.SelectedRows(0).DataBoundItem, DataRowView)
        If drv Is Nothing Then Return
        Dim id As Integer = Convert.ToInt32(drv("id"))
        If id = _batchId Then Return
        _batchId = id
        ClearEntry()
        ShowBatch()
    End Sub

    Private Sub ShowBatch()
        If _batchId = 0 Then
            _batchPanel.Visible = False
            _placeholder.Visible = True
            Return
        End If

        Dim hdr As DataRow = Nothing
        Try
            hdr = DeliveryRepository.GetBatch(_batchId)
            If hdr IsNot Nothing Then _items = DeliveryRepository.GetItems(_batchId)
        Catch ex As Exception
            Return
        End Try
        If hdr Is Nothing Then
            _batchId = 0
            _batchPanel.Visible = False
            _placeholder.Visible = True
            Return
        End If

        _placeholder.Visible = False
        _batchPanel.Visible = True
        _isDraft = (hdr("status").ToString() = "draft")

        _lblBatchNo.Text = hdr("batch_no").ToString()
        _pillStatus.SetPill(If(_isDraft, "Draft", "Delivered"),
                            If(_isDraft, Color.FromArgb(255, 243, 219), Color.FromArgb(226, 246, 234)),
                            If(_isDraft, Color.FromArgb(160, 100, 10), Color.FromArgb(22, 130, 60)))
        _pillStatus.Location = New Point(_lblBatchNo.Right + Theme.S(14), _lblBatchNo.Top + Theme.S(12))
        _lblSupplier.Text = hdr("company_name").ToString() & "   -   " & hdr("company_address").ToString()
        _company = hdr("company_name").ToString()

        Dim meta As String = "Contact: " & hdr("contact").ToString() & "   |   Delivery date: " &
                             Convert.ToDateTime(hdr("delivery_date")).ToString("MMM d, yyyy")
        If _isDraft Then
            If hdr("created_by_name") IsNot DBNull.Value Then meta &= "   |   Started by " & hdr("created_by_name").ToString()
        ElseIf hdr("sent_at") IsNot DBNull.Value Then
            meta &= "   |   Sent to inventory " & Convert.ToDateTime(hdr("sent_at")).ToString("MMM d, yyyy h:mm tt")
            If hdr("sent_by_name") IsNot DBNull.Value Then meta &= " by " & hdr("sent_by_name").ToString()
        End If
        _lblMeta.Text = meta

        _grid.DataSource = _items
        _grid.ClearSelection()
        _grid.Columns("remaining").Visible = Not _isDraft

        _entry.Visible = _isDraft
        _btnEditBatch.Visible = _isDraft
        _btnDeleteBatch.Visible = _isDraft
        _btnSend.Visible = _isDraft
        _btnRemove.Visible = _isDraft
        UpdateSummary()
        OnInfoResize(Me, EventArgs.Empty)
        OnEntryResize(Me, EventArgs.Empty)
        OnTotalsResize(Me, EventArgs.Empty)
    End Sub

    Private Sub UpdateSummary()
        Dim lines As Integer = 0
        Dim units As Integer = 0
        Dim cost As Decimal = 0D
        Dim sales As Decimal = 0D
        Dim profit As Decimal = 0D
        If _items IsNot Nothing Then
            For Each r As DataRow In _items.Rows
                lines += 1
                units += Convert.ToInt32(r("qty"))
                cost += Convert.ToDecimal(r("line_cost"))
                sales += Convert.ToInt32(r("qty")) * Convert.ToDecimal(r("selling_price"))
                profit += Convert.ToDecimal(r("expected_profit"))
            Next
        End If
        Dim margin As String = If(sales > 0D, (profit / sales * 100D).ToString("0.0", CultureInfo.InvariantCulture) & "% margin", "-")
        _lblSummary.Text = lines.ToString() & If(lines = 1, " item", " items") & " (" & units.ToString() & " pcs)    |    Total cost " &
                           Currency.Format(cost) & "    |    Expected sales " & Currency.Format(sales) &
                           "    |    Expected profit " & Currency.Format(profit) & "  (" & margin & ")"
    End Sub

    ' ------------------------------------------------------------------ painting
    Private Sub OnBatchCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim row As DataRowView = TryCast(_batchGrid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 232, 232))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim c As Rectangle = e.CellBounds
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
        Dim draft As Boolean = (row("status").ToString() = "draft")

        Dim pillText As String = If(draft, "Draft", "Delivered")
        Dim pillW As Integer = Pills.Measure(pillText)
        Pills.PaintPill(e.Graphics, New Rectangle(c.Right - pillW - Theme.S(14), c.Y + Theme.S(10), pillW, Theme.S(24)), pillText,
                        If(draft, Color.FromArgb(255, 243, 219), Color.FromArgb(226, 246, 234)),
                        If(draft, Color.FromArgb(160, 100, 10), Color.FromArgb(22, 130, 60)))

        TextRenderer.DrawText(e.Graphics, row("batch_no").ToString(), NameFont, New Rectangle(c.X + 14, c.Y + 12, c.Width - pillW - 40, 20), Theme.TextDark, flags)
        TextRenderer.DrawText(e.Graphics, row("company_name").ToString(), Theme.FontBase, New Rectangle(c.X + 14, c.Y + 34, c.Width - 28, 20), Theme.TextDark, flags)
        Dim line3 As String = Convert.ToDateTime(row("delivery_date")).ToString("MMM d, yyyy") & "   |   " &
                              row("item_count").ToString() & " items   |   " & Currency.Format(row("total_cost"))
        TextRenderer.DrawText(e.Graphics, line3, Theme.FontSmall, New Rectangle(c.X + 14, c.Y + 56, c.Width - 28, 18), Theme.TextMuted, flags)
        e.Handled = True
    End Sub

    Private Sub OnItemCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
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
        Dim sub2 As String = row("category").ToString() & "   |   " & row("barcode").ToString()
        If row("cost_mode").ToString() = "pack" Then
            Dim pk As Integer = Convert.ToInt32(row("pack_size"))
            Dim packs As Integer = Convert.ToInt32(row("qty")) \ Math.Max(1, pk)
            sub2 = packs.ToString() & " pack(s) x " & pk.ToString() & " pcs @ " & Currency.Format(row("entered_cost")) & " per pack"
        End If
        TextRenderer.DrawText(e.Graphics, sub2, Theme.FontSmall,
                              New Rectangle(c.X + Theme.S(10), c.Y + Theme.S(31), c.Width - Theme.S(14), Theme.S(18)), Theme.TextMuted, flags)
        e.Handled = True
    End Sub

    Private Sub OnItemCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Select Case _grid.Columns(e.ColumnIndex).Name
            Case "cost"
                e.Value = Currency.FormatCost(e.Value)
                e.FormattingApplied = True
            Case "price", "line", "profit"
                e.Value = Currency.Format(e.Value)
                e.FormattingApplied = True
            Case "size"
                If e.Value Is Nothing OrElse e.Value.ToString().Length = 0 Then
                    e.Value = "-"
                    e.FormattingApplied = True
                End If
        End Select
    End Sub

    ' ------------------------------------------------------------------ batches: new / edit / delete
    Private Sub OnNewBatch(sender As Object, e As EventArgs)
        Using dlg As New DeliveryBatchDialog()
            If dlg.ShowDialog(OwnerOf(Me)) <> DialogResult.OK Then Return

            Dim newId As Integer = 0
            Dim result As BatchResult
            Try
                result = DeliveryRepository.CreateBatch(dlg.SupplierId, dlg.DeliveryDate, AppSession.UserId, newId)
            Catch ex As Exception
                Notify(Me, "Delivery", "Could not create the batch: " & ex.Message, MessageBoxIcon.Error)
                Return
            End Try

            If result <> BatchResult.Ok Then
                Notify(Me, "Delivery", "Could not create the batch. Try again.", MessageBoxIcon.Warning)
                Return
            End If

            AuditLog.Write("ADD", "Delivery", "Started delivery batch (id " & newId.ToString() & ")")
            _filter.SelectedIndex = 0
            _batchId = newId
            ClearEntry()
            LoadBatches()
            _scan.FocusInput()
        End Using
    End Sub

    Private Sub OnEditBatch(sender As Object, e As EventArgs)
        If _batchId = 0 OrElse Not _isDraft Then Return
        Dim hdr As DataRow = DeliveryRepository.GetBatch(_batchId)
        If hdr Is Nothing Then Return

        Using dlg As New DeliveryBatchDialog(Convert.ToInt32(hdr("supplier_id")), Convert.ToDateTime(hdr("delivery_date")))
            If dlg.ShowDialog(OwnerOf(Me)) <> DialogResult.OK Then Return
            If DeliveryRepository.UpdateBatch(_batchId, dlg.SupplierId, dlg.DeliveryDate) = BatchResult.Ok Then
                AuditLog.Write("EDIT", "Delivery", "Changed details of batch " & hdr("batch_no").ToString())
            Else
                Notify(Me, "Delivery", "This batch can no longer be changed.", MessageBoxIcon.Information)
            End If
            LoadBatches()
        End Using
    End Sub

    Private Sub OnDeleteBatch(sender As Object, e As EventArgs)
        If _batchId = 0 OrElse Not _isDraft Then Return
        Dim no As String = _lblBatchNo.Text
        If Not Confirm(Me, "Delivery", "Delete the draft batch " & no & " and everything in it?" & Environment.NewLine &
                       "Nothing has been added to inventory yet.", MessageBoxIcon.Warning) Then Return

        If DeliveryRepository.DeleteBatch(_batchId) = BatchResult.Ok Then
            AuditLog.Write("DELETE", "Delivery", "Deleted draft batch " & no)
        End If
        _batchId = 0
        ClearEntry()
        LoadBatches()
    End Sub

    ' ------------------------------------------------------------------ entry bar
    Private Sub ClearEntry()
        _productId = 0
        _productName = ""
        _lblItem.Text = "No item chosen yet. Scan a barcode, or click Browse products."
        _lblItem.ForeColor = Theme.TextMuted
        _lblItemSub.Text = ""
        _qty.Value = ""
        _cost.Value = ""
        _price.Value = ""
        _packSize.Value = ""
        _costMode.Value = "Per piece"
        _lblPreview.Text = ""
        _btnAdd.Text = "Add to batch"
        _hasRef = False
        _refCost = 0D
        _refInfo = ""
        _refMode = "piece"
        _refPack = 1
        _refEntered = 0D
        _costUnlocked = False
        _cost.IsReadOnly = False
        _packSize.IsReadOnly = False
        UpdateModeUi()
        _lblLock.Visible = False
        _linkUnlock.Visible = False
        SetEntryEnabled(False)
    End Sub

    Private Sub SetEntryEnabled(isOn As Boolean)
        _qty.Enabled = isOn
        _costMode.Enabled = isOn
        _packSize.Enabled = isOn
        _cost.Enabled = isOn
        _price.Enabled = isOn
        _btnAdd.Enabled = isOn
    End Sub

    Private Sub OnScanEnter()
        If _batchId = 0 OrElse Not _isDraft Then Return
        Dim text As String = _scan.Value.Trim()
        _scan.Value = ""

        If text.Length = 0 Then
            OpenPicker("")
            Return
        End If

        Dim id As Integer = 0
        Try
            id = ProductLookup.FindByBarcode(text)
        Catch ex As Exception
            Notify(Me, "Delivery", "Could not reach the database: " & ex.Message, MessageBoxIcon.Error)
            Return
        End Try

        If id > 0 Then
            ChooseProduct(id)
        Else
            OpenPicker(text)
        End If
    End Sub

    Private Sub OnBrowse(sender As Object, e As EventArgs)
        If _batchId = 0 OrElse Not _isDraft Then Return
        OpenPicker(_scan.Value.Trim())
        _scan.Value = ""
    End Sub

    Private Sub OpenPicker(initialText As String)
        Using dlg As New ProductPickerDialog(initialText)
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then ChooseProduct(dlg.SelectedProductId)
        End Using
    End Sub

    ''' <summary>Loads the item into the entry bar. If it is already in the batch, its line is loaded for editing.</summary>
    Private Sub ChooseProduct(productId As Integer)
        Dim brief As DataRow = Nothing
        Try
            brief = ProductLookup.GetBrief(productId)
        Catch ex As Exception
            Notify(Me, "Delivery", "Could not load the product: " & ex.Message, MessageBoxIcon.Error)
            Return
        End Try
        If brief Is Nothing Then
            Notify(Me, "Delivery", "This product no longer exists.", MessageBoxIcon.Information)
            Return
        End If

        _productId = productId
        Dim size As String = brief("measurement").ToString()
        _productName = brief("name").ToString() & If(size.Length > 0, "  " & size, "")
        _lblItem.Text = _productName
        _lblItem.ForeColor = Theme.TextDark

        Dim curCost As Decimal = Convert.ToDecimal(brief("cost_price"))
        Dim curPrice As Decimal = Convert.ToDecimal(brief("selling_price"))
        _lblItemSub.Text = brief("category").ToString() & "   |   Barcode " & brief("barcode").ToString() &
                           "   |   In stock " & Convert.ToInt32(brief("qty_on_hand")).ToString() &
                           "   |   Current cost " & Currency.FormatCost(curCost) & " per piece" & "   |   Current price " & Currency.Format(curPrice)

        SetEntryEnabled(True)
        _btnAdd.Text = "Add to batch"
        _qty.Value = ""
        _packSize.Value = ""
        _costMode.Value = "Per piece"
        _costUnlocked = False
        _hasRef = False
        _refInfo = ""
        _cost.IsReadOnly = False
        _packSize.IsReadOnly = False

        ' what did this supplier charge the last time for this item?
        Dim last As DataRow = Nothing
        Try
            last = DeliveryRepository.GetLastSupplierPrice(_batchId, productId)
        Catch
            last = Nothing
        End Try

        If last IsNot Nothing Then
            _hasRef = True
            _refCost = Convert.ToDecimal(last("cost_price"))
            _refMode = last("cost_mode").ToString()
            _refPack = Convert.ToInt32(last("pack_size"))
            _refEntered = Convert.ToDecimal(last("entered_cost"))
            _refInfo = "last delivery " & last("batch_no").ToString() & ", " & Convert.ToDateTime(last("delivery_date")).ToString("MMM d, yyyy")
            ShowCostEntry(_refMode, _refPack, _refEntered)
            _price.Value = Convert.ToDecimal(last("selling_price")).ToString("0.##", CultureInfo.InvariantCulture)
        Else
            ' first time from this supplier: the cost has to be typed in
            _cost.Value = ""
            _price.Value = If(curPrice > 0D, curPrice.ToString("0.##", CultureInfo.InvariantCulture), "")
        End If

        ' already in this batch? load that line for editing
        If _items IsNot Nothing Then
            For Each r As DataRow In _items.Rows
                If Convert.ToInt32(r("product_id")) = productId Then
                    Dim mode As String = r("cost_mode").ToString()
                    Dim pk As Integer = Math.Max(1, Convert.ToInt32(r("pack_size")))
                    Dim pieces As Integer = Convert.ToInt32(r("qty"))
                    ShowCostEntry(mode, pk, Convert.ToDecimal(r("entered_cost")))
                    _qty.Value = If(mode = "pack", (pieces \ pk).ToString(), pieces.ToString())
                    _price.Value = Convert.ToDecimal(r("selling_price")).ToString("0.##", CultureInfo.InvariantCulture)
                    _btnAdd.Text = "Update item"
                    If _hasRef AndAlso Math.Abs(Convert.ToDecimal(r("cost_price")) - _refCost) > 0.00005D Then _costUnlocked = True
                    Exit For
                End If
            Next
        End If

        UpdateModeUi()
        ApplyCostLock()
        UpdatePreview()
        _qty.FocusInput()
    End Sub

    Private Sub ApplyCostLock()
        Dim locked As Boolean = _hasRef AndAlso Not _costUnlocked
        _cost.IsReadOnly = locked
        _packSize.IsReadOnly = locked
        _costMode.Enabled = Not locked

        If locked Then
            _lblLock.Text = "Cost is locked at " & DescribeCost(_refMode = "pack", _refPack, _refEntered, _refCost) & " - what " & _company & " charged (" & _refInfo & ")."
            _lblLock.Visible = True
            _linkUnlock.Visible = True
        ElseIf _hasRef Then
            _lblLock.Text = "Cost unlocked. " & _company & " charged " & DescribeCost(_refMode = "pack", _refPack, _refEntered, _refCost) & " before. A change is recorded in the audit trail."
            _lblLock.Visible = True
            _linkUnlock.Visible = False
        Else
            _lblLock.Text = "First delivery of this item from " & _company & ": enter its cost price (per piece, or per pack / box)."
            _lblLock.Visible = True
            _linkUnlock.Visible = False
        End If
        _linkUnlock.Location = New Point(_lblLock.Right + Theme.S(14), _lblLock.Top)
    End Sub

    Private Sub OnUnlockCost(sender As Object, e As LinkLabelLinkClickedEventArgs)
        If Not Confirm(Me, "Delivery", "Only unlock the cost price if " & _company & " really changed its price." & Environment.NewLine &
                       "The change is recorded in the audit trail with your name. Unlock it?", MessageBoxIcon.Warning) Then Return
        _costUnlocked = True
        ApplyCostLock()
        _cost.FocusInput()
    End Sub

    Private Sub OnFieldEnter()
        ' Enter in Quantity or Cost moves on to the next box
        SelectNextControl(Me.ActiveControl, True, True, True, True)
    End Sub

    Private Sub OnEntryChanged()
        UpdatePreview()
    End Sub

    Private Shared Function TryDec(text As String, ByRef value As Decimal) As Boolean
        Return Decimal.TryParse(If(text, "").Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, value)
    End Function

    Private Sub UpdatePreview()
        If _productId = 0 Then
            _lblPreview.Text = ""
            Return
        End If
        Dim pack As Boolean = IsPackMode()
        Dim q As Integer
        Dim c As Decimal
        Dim p As Decimal
        Dim pk As Integer = 1
        If pack Then
            If Not Integer.TryParse(_packSize.Value.Trim(), pk) OrElse pk < 2 Then
                _lblPreview.ForeColor = Theme.TextMuted
                _lblPreview.Text = "Enter how many pieces are in 1 pack."
                Return
            End If
        End If

        If Integer.TryParse(_qty.Value.Trim(), q) AndAlso q > 0 AndAlso TryDec(_cost.Value, c) AndAlso c > 0D AndAlso TryDec(_price.Value, p) AndAlso p > 0D Then
            Dim pieces As Integer = q * pk
            Dim unit As Decimal = If(pack, Math.Round(c / pk, 4, MidpointRounding.AwayFromZero), c)
            Dim lineCost As Decimal = Math.Round(If(pack, q * c, pieces * c), 2, MidpointRounding.AwayFromZero)
            Dim profit As Decimal = pieces * p - lineCost
            Dim margin As Decimal = (p - unit) / p * 100D
            Dim line1 As String = If(pack,
                pieces.ToString("N0", CultureInfo.InvariantCulture) & " pieces, each costs " & Currency.FormatCost(unit) & "   |   Cost " & Currency.Format(lineCost),
                "Line cost " & Currency.Format(lineCost))
            _lblPreview.Text = line1 & Environment.NewLine &
                               "Profit " & Currency.Format(profit) & "  (" & margin.ToString("0.0", CultureInfo.InvariantCulture) & "% margin)"
            _lblPreview.ForeColor = If(profit >= 0D, Color.FromArgb(22, 130, 60), Theme.AlertRed)
        Else
            _lblPreview.Text = ""
        End If
    End Sub

    Private Sub OnAdd(sender As Object, e As EventArgs)
        OnAdd()
    End Sub

    Private Sub OnAdd()
        If _batchId = 0 OrElse Not _isDraft OrElse _productId = 0 Then Return

        Dim pack As Boolean = IsPackMode()
        Dim problem As String = If(pack, InputField.CheckAll(_qty, _packSize, _cost, _price), InputField.CheckAll(_qty, _cost, _price))
        If problem.Length > 0 Then
            _lblPreview.Text = problem
            _lblPreview.ForeColor = Theme.AlertRed
            Return
        End If

        Dim q As Integer = Integer.Parse(_qty.Value.Trim(), CultureInfo.InvariantCulture)
        Dim pk As Integer = 1
        If pack Then pk = Integer.Parse(_packSize.Value.Trim(), CultureInfo.InvariantCulture)
        Dim c As Decimal
        Dim p As Decimal
        TryDec(_cost.Value, c)
        TryDec(_price.Value, p)
        Dim unit As Decimal = If(pack, Math.Round(c / pk, 4, MidpointRounding.AwayFromZero), c)

        If p <= unit Then
            If Not Confirm(Me, "Delivery", "The selling price per piece (" & Currency.Format(p) & ") is not higher than the cost per piece (" & Currency.FormatCost(unit) & ")." &
                           Environment.NewLine & "Add it anyway?", MessageBoxIcon.Warning) Then Return
        End If

        Try
            Select Case DeliveryRepository.SaveItem(_batchId, _productId, q, If(pack, "pack", "piece"), pk, c, p, _costUnlocked)
                Case BatchResult.Ok
                    If _hasRef AndAlso _costUnlocked AndAlso Math.Abs(unit - _refCost) > 0.00005D Then
                        AuditLog.Write("PRICE_CHANGE", "Delivery",
                                       "Supplier price changed for " & _productName.Trim() & " from " & _company & ": " &
                                       DescribeCost(_refMode = "pack", _refPack, _refEntered, _refCost) & " to " &
                                       DescribeCost(pack, pk, c, unit) & " (batch " & _lblBatchNo.Text & ")")
                    End If
                    ClearEntry()
                    ShowBatch()
                    LoadBatches()
                    _scan.FocusInput()
                Case BatchResult.NotDraft
                    Notify(Me, "Delivery", "This batch was already sent to inventory.", MessageBoxIcon.Information)
                    LoadBatches()
                Case BatchResult.NotFound
                    Notify(Me, "Delivery", "This batch no longer exists.", MessageBoxIcon.Information)
                    LoadBatches()
                Case BatchResult.PriceLocked
                    Notify(Me, "Delivery", _company & "'s cost price for this item is locked." & Environment.NewLine &
                           "If the supplier really changed its price, click 'Supplier's price changed' first.", MessageBoxIcon.Warning)
                Case BatchResult.Invalid
                    Notify(Me, "Delivery", "Check the quantity, the pieces in a pack and the prices: they must be more than zero.", MessageBoxIcon.Warning)
                Case Else
                    Notify(Me, "Delivery", "Could not save the item. Try again.", MessageBoxIcon.Warning)
            End Select
        Catch ex As Exception
            Notify(Me, "Delivery", "Could not save the item: " & ex.Message, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ------------------------------------------------------------------ lines
    Private Function SelectedItem() As DataRowView
        If _grid.SelectedRows.Count = 0 Then Return Nothing
        Return TryCast(_grid.SelectedRows(0).DataBoundItem, DataRowView)
    End Function

    Private Sub OnItemDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 OrElse Not _isDraft Then Return
        Dim row As DataRowView = SelectedItem()
        If row Is Nothing Then Return
        ChooseProduct(Convert.ToInt32(row("product_id")))
    End Sub

    Private Sub OnRemove(sender As Object, e As EventArgs)
        If Not _isDraft Then Return
        Dim row As DataRowView = SelectedItem()
        If row Is Nothing Then
            Notify(Me, "Delivery", "Click a line in the list first.", MessageBoxIcon.Information)
            Return
        End If

        Dim label As String = row("name").ToString() & " " & row("measurement").ToString()
        If Not Confirm(Me, "Delivery", "Remove " & label.Trim() & " from this batch?", MessageBoxIcon.Question) Then Return

        Try
            If DeliveryRepository.RemoveItem(_batchId, Convert.ToInt32(row("id"))) = BatchResult.NotDraft Then
                Notify(Me, "Delivery", "This batch was already sent to inventory.", MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            Notify(Me, "Delivery", "Could not remove the line: " & ex.Message, MessageBoxIcon.Error)
        End Try
        If _productId = Convert.ToInt32(row("product_id")) Then ClearEntry()
        ShowBatch()
        LoadBatches()
    End Sub

    ' ------------------------------------------------------------------ send to inventory
    Private Sub OnSend(sender As Object, e As EventArgs)
        If _batchId = 0 OrElse Not _isDraft Then Return
        If _items Is Nothing OrElse _items.Rows.Count = 0 Then
            Notify(Me, "Delivery", "Add at least one item to the batch first.", MessageBoxIcon.Information)
            Return
        End If

        Dim no As String = _lblBatchNo.Text
        If Not Confirm(Me, "Send to inventory", "Send " & no & " to inventory?" & Environment.NewLine & Environment.NewLine &
                       _lblSummary.Text & Environment.NewLine & Environment.NewLine &
                       "Stock goes up and the cost and selling prices are updated. After this the batch is locked and cannot be edited.",
                       MessageBoxIcon.Question) Then Return

        Try
            Select Case DeliveryRepository.SendToInventory(_batchId, AppSession.UserId)
                Case BatchResult.Ok
                    AuditLog.Write("SEND", "Delivery", "Sent batch " & no & " to inventory: " & _lblSummary.Text)
                    ClearEntry()
                    ShowBatch()
                    LoadBatches()
                    Notify(Me, "Delivery", no & " is now in inventory.", MessageBoxIcon.Information)
                Case BatchResult.Empty
                    Notify(Me, "Delivery", "The batch is empty.", MessageBoxIcon.Information)
                Case BatchResult.NotDraft
                    Notify(Me, "Delivery", "This batch was already sent.", MessageBoxIcon.Information)
                    LoadBatches()
                Case Else
                    Notify(Me, "Delivery", "Could not send the batch. Nothing was changed. Try again.", MessageBoxIcon.Warning)
            End Select
        Catch ex As Exception
            Notify(Me, "Delivery", "Could not send the batch: " & ex.Message & Environment.NewLine & "Nothing was changed.", MessageBoxIcon.Error)
        End Try
    End Sub

End Class
