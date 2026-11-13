Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>A caption above a rounded date box.</summary>
Public Class DateField
    Inherits Panel

    Private ReadOnly _caption As New Label()
    Private ReadOnly _box As New Panel()
    Private ReadOnly _picker As New DateTimePicker()

    Public Sub New(caption As String, Optional fieldWidth As Integer = 233)
        Size = New Size(fieldWidth, Theme.S(70))
        BackColor = Color.White

        _caption.Text = caption
        _caption.AutoSize = True
        _caption.Font = Theme.FontCaption
        _caption.ForeColor = Theme.TextMuted
        _caption.Location = New Point(0, 0)

        _box.Location = New Point(0, Theme.S(22))
        _box.Size = New Size(fieldWidth, Theme.S(42))
        _box.BackColor = Color.White
        AddHandler _box.Paint, AddressOf OnBoxPaint

        _picker.Format = DateTimePickerFormat.Custom
        _picker.CustomFormat = "MMM d, yyyy"
        _picker.Font = Theme.FontInput
        _picker.Left = Theme.S(8)
        _picker.Width = fieldWidth - Theme.S(16)
        _picker.Top = (_box.Height - _picker.Height) \ 2

        _box.Controls.Add(_picker)
        Controls.Add(_caption)
        Controls.Add(_box)
    End Sub

    ' The latest date that can be picked (deliveries cannot be dated in the future)
    Public Property MaxDate As Date
        Get
            Return _picker.MaxDate
        End Get
        Set(v As Date)
            _picker.MaxDate = v
        End Set
    End Property

    Public Property Value As Date
        Get
            Return _picker.Value.Date
        End Get
        Set(v As Date)
            _picker.Value = If(v > _picker.MaxDate, _picker.MaxDate, v)
        End Set
    End Property

    Private Sub OnBoxPaint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, _box.Width - 1, _box.Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(rect, 8)
            Using pen As New Pen(Theme.BorderColor, 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub
End Class

''' <summary>Start a new delivery batch (supplier + date), or change them while it is still a draft.</summary>
Public Class DeliveryBatchDialog
    Inherits ModalDialog

    Private ReadOnly _ids As New Dictionary(Of String, Integer)()
    Private ReadOnly _supplier As New ChoiceField("Supplier (company - address - contact person)", 480)
    Private ReadOnly _date As New DateField("Delivery date", 233)
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    Public Property SupplierId As Integer
    Public Property DeliveryDate As Date = Date.Today

    ''' <summary>New batch.</summary>
    Public Sub New()
        MyBase.New("New delivery batch", "Choose who delivered. The next steps unlock once a supplier is picked.", 560, 400)
        Build(False)
        LoadSuppliers(0)
        _date.Value = Date.Today
    End Sub

    ''' <summary>Change the supplier or date of a draft batch.</summary>
    Public Sub New(supplierId As Integer, deliveryDate As Date)
        MyBase.New("Edit batch details", "Only a draft batch can be changed.", 560, 400)
        Build(True)
        LoadSuppliers(supplierId)
        _date.Value = deliveryDate
    End Sub

    Private Sub Build(isEdit As Boolean)
        _supplier.Location = New Point(Theme.S(40), Theme.S(110))
        _date.Location = New Point(Theme.S(40), Theme.S(184))
        _date.MaxDate = Date.Today

        Message.Location = New Point(Theme.S(40), Theme.S(262))
        Message.Size = New Size(Theme.S(480), Theme.S(34))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(140), Theme.S(44))
        _cancel.Location = New Point(Theme.S(220), Theme.S(306))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = If(isEdit, "Save changes", "Create batch")
        _save.Size = New Size(Theme.S(140), Theme.S(44))
        _save.Location = New Point(Theme.S(380), Theme.S(306))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_supplier)
        Controls.Add(_date)
        Controls.Add(_cancel)
        Controls.Add(_save)
        ClientSize = New Size(Theme.S(560), Theme.S(306) + Theme.S(44) + Theme.S(28))
        AcceptButton = _save
        CancelButton = _cancel
    End Sub

    Private Sub LoadSuppliers(selectId As Integer)
        Try
            Dim names As New List(Of String)()
            Dim selectName As String = ""
            For Each r As DataRow In DeliveryRepository.GetSupplierChoices().Rows
                Dim label As String = r("company_name").ToString() & "  -  " & r("company_address").ToString() & "  (" & r("name").ToString() & ")"
                Dim id As Integer = Convert.ToInt32(r("id"))
                names.Add(label)
                _ids(label) = id
                If id = selectId Then selectName = label
            Next
            _supplier.SetChoices(names.ToArray())
            If selectName.Length > 0 Then _supplier.Value = selectName
            If names.Count = 0 Then
                _save.Enabled = False
                ShowMessage("Add a supplier first (File Maintenance > Suppliers).", True)
            End If
        Catch ex As Exception
            _save.Enabled = False
            ShowMessage("Could not load the suppliers: " & ex.Message, True)
        End Try
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Dim label As String = _supplier.Value
        If label.Length = 0 OrElse Not _ids.ContainsKey(label) Then
            ShowMessage("Choose the supplier first.", True)
            Return
        End If
        If _date.Value > Date.Today Then
            ShowMessage("The delivery date cannot be in the future.", True)
            Return
        End If
        SupplierId = _ids(label)
        DeliveryDate = _date.Value
        DialogResult = DialogResult.OK
    End Sub
End Class

''' <summary>
''' Find a product to receive: type a name / size / category, or scan a barcode. Enter picks the match
''' when there is only one. Not in the list? Create it right here.
''' </summary>
Public Class ProductPickerDialog
    Inherits ModalDialog

    Private ReadOnly _search As New InputField("Scan a barcode or type a name, size or category", InputKind.Text, 480, False)
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _timer As New Timer()
    Private ReadOnly _newLink As New LinkLabel()
    Private ReadOnly _select As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    Public Property SelectedProductId As Integer

    Public Sub New(Optional initialText As String = "")
        MyBase.New("Choose a product", "Pick the item you received. A barcode scan selects it straight away.", 780, 640)

        _search.Location = New Point(Theme.S(40), Theme.S(100))
        _search.SwallowEnter = True
        AddHandler _search.ValueChanged, AddressOf OnSearchTyped
        AddHandler _search.EnterPressed, AddressOf OnSearchEnter

        _grid.Location = New Point(Theme.S(40), Theme.S(182))
        _grid.Size = New Size(Theme.S(700), Theme.S(330))
        GridStyle.Apply(_grid, 44)
        AddCol("name", "PRODUCT", "name", 32)
        AddCol("measurement", "SIZE", "measurement", 16)
        AddCol("category", "CATEGORY", "category", 20)
        AddCol("barcode", "BARCODE", "barcode", 20)
        AddCol("qty_on_hand", "IN STOCK", "qty_on_hand", 12)
        AddHandler _grid.CellDoubleClick, AddressOf OnGridDoubleClick
        AddHandler _grid.KeyDown, AddressOf OnGridKeyDown

        _newLink.AutoSize = True
        _newLink.Text = "Not in the list? Create a new product or size"
        _newLink.Font = Theme.FontSmall
        _newLink.LinkColor = Theme.Maroon
        _newLink.ActiveLinkColor = Theme.MaroonDark
        _newLink.VisitedLinkColor = Theme.Maroon
        _newLink.LinkBehavior = LinkBehavior.HoverUnderline
        _newLink.Cursor = Cursors.Hand
        _newLink.Location = New Point(Theme.S(40), Theme.S(526))
        AddHandler _newLink.LinkClicked, AddressOf OnNewProduct

        Message.Location = New Point(Theme.S(40), Theme.S(548))
        Message.Size = New Size(Theme.S(480), Theme.S(22))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(120), Theme.S(44))
        _cancel.Location = New Point(Theme.S(480), Theme.S(560))
        AddHandler _cancel.Click, AddressOf OnCancel

        _select.Text = "Select"
        _select.Size = New Size(Theme.S(120), Theme.S(44))
        _select.Location = New Point(Theme.S(620), Theme.S(560))
        AddHandler _select.Click, AddressOf OnSelect

        Controls.Add(_search)
        Controls.Add(_grid)
        Controls.Add(_newLink)
        Controls.Add(_cancel)
        Controls.Add(_select)
        ClientSize = New Size(Theme.S(780), Theme.S(560) + Theme.S(44) + Theme.S(26))
        AcceptButton = _select
        CancelButton = _cancel

        _timer.Interval = 250
        AddHandler _timer.Tick, AddressOf OnTimerTick

        _search.Value = initialText
        Reload()
    End Sub

    Private Sub AddCol(name As String, headerText As String, dataProp As String, weight As Single)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = name
        c.HeaderText = headerText
        c.DataPropertyName = dataProp
        c.FillWeight = weight
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        _grid.Columns.Add(c)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _search.FocusInput()
    End Sub

    Private Sub Reload()
        Try
            _grid.DataSource = ProductRepository.GetProducts(_search.Value, 0)
            _grid.ClearSelection()
            If _grid.Rows.Count > 0 Then
                _grid.Rows(0).Selected = True
                _grid.CurrentCell = _grid.Rows(0).Cells(0)
            End If
            ShowMessage(If(_grid.Rows.Count = 0, "No product matches.", ""), True)
        Catch ex As Exception
            ShowMessage("Could not load the products: " & ex.Message, True)
        End Try
    End Sub

    Private Sub OnSearchTyped()
        _timer.Stop()
        _timer.Start()
    End Sub

    Private Sub OnTimerTick(sender As Object, e As EventArgs)
        _timer.Stop()
        Reload()
    End Sub

    ''' <summary>A scanner presses Enter after the code: search right now and take the match.</summary>
    Private Sub OnSearchEnter()
        _timer.Stop()
        Reload()
        If _grid.Rows.Count = 1 Then
            ChooseCurrent()
        ElseIf _grid.Rows.Count > 1 Then
            _grid.Focus()
        End If
    End Sub

    Private Sub ChooseCurrent()
        If _grid.SelectedRows.Count = 0 Then
            ShowMessage("Pick a product from the list.", True)
            Return
        End If
        Dim drv As DataRowView = TryCast(_grid.SelectedRows(0).DataBoundItem, DataRowView)
        If drv Is Nothing Then Return
        SelectedProductId = Convert.ToInt32(drv("id"))
        DialogResult = DialogResult.OK
    End Sub

    Private Sub OnSelect(sender As Object, e As EventArgs)
        ChooseCurrent()
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnGridDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then ChooseCurrent()
    End Sub

    Private Sub OnGridKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            e.Handled = True
            ChooseCurrent()
        End If
    End Sub

    Private Sub OnNewProduct(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Try
            If CategoryRepository.GetAll().Rows.Count = 0 Then
                ShowMessage("Create a category first (Products > Categories).", True)
                Return
            End If
        Catch ex As Exception
            ShowMessage("Could not reach the database: " & ex.Message, True)
            Return
        End Try

        Dim typed As String = _search.Value.Trim()
        Dim looksLikeCode As Boolean = (typed.Length > 0 AndAlso typed.Replace("-", "").Trim().Length > 0 AndAlso IsAllDigits(typed))
        Using dlg As New ProductEditForm(If(looksLikeCode, "", typed), 0)
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Reload()
                If _grid.Rows.Count = 1 Then ChooseCurrent()
            End If
        End Using
    End Sub

    Private Shared Function IsAllDigits(s As String) As Boolean
        For Each c As Char In s
            If c < "0"c OrElse c > "9"c Then Return False
        Next
        Return True
    End Function

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _timer.Stop()
            _timer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
