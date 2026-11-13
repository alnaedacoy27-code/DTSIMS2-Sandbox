Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Add a product (or another size of one), or edit one.</summary>
Public Class ProductEditForm
    Inherits ModalDialog

    Private ReadOnly _isEdit As Boolean
    Private ReadOnly _productId As Integer
    Private ReadOnly _catIds As New Dictionary(Of String, Integer)()
    Private ReadOnly _name As New InputField("Product name", InputKind.Title, 480, True)
    Private ReadOnly _category As New ChoiceField("Category", 233)
    Private ReadOnly _size As New InputField("Size / measurement", InputKind.Measurement, 233, True)
    Private ReadOnly _barcode As New InputField("Barcode", InputKind.Barcode, 480, False)
    Private ReadOnly _hint As New Label()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    ''' <summary>Add a product. Give a name and category to add another size of an existing one.</summary>
    Public Sub New(Optional prefillName As String = "", Optional prefillCategoryId As Integer = 0)
        MyBase.New(If(prefillName.Length > 0, "Add a size", "Add product"),
                   If(prefillName.Length > 0, "Another size of " & prefillName & ". Each size has its own barcode and stock.",
                      "Create a product. Every size is its own item with its own barcode."), 560, 520)
        _isEdit = False
        _productId = 0
        Build()
        LoadCategories(prefillCategoryId)
        _name.Value = prefillName
    End Sub

    ''' <summary>Edit an existing product.</summary>
    Public Sub New(productId As Integer)
        MyBase.New("Edit product", "Change the name, category, size or barcode.", 560, 520)
        _isEdit = True
        _productId = productId
        Build()

        Dim row As DataRow = Nothing
        Try
            row = ProductRepository.GetProduct(productId)
        Catch ex As Exception
            ShowMessage("Could not load this product: " & ex.Message, True)
        End Try
        If row Is Nothing Then
            LoadCategories(0)
            _save.Enabled = False
            If Message.Text.Length = 0 Then ShowMessage("This product no longer exists.", True)
            Return
        End If

        LoadCategories(Convert.ToInt32(row("category_id")))
        _name.Value = row("name").ToString()
        _size.Value = row("measurement").ToString()
        _barcode.Value = row("barcode").ToString()
        _barcode.Required = True
    End Sub

    Private Sub Build()
        _name.Location = New Point(Theme.S(40), Theme.S(110))
        _category.Location = New Point(Theme.S(40), Theme.S(184))
        _size.Location = New Point(Theme.S(287), Theme.S(184))
        _barcode.Location = New Point(Theme.S(40), Theme.S(258))
        _barcode.SwallowEnter = True      ' a scanner presses Enter after the code: do not submit the form
        AddHandler _barcode.EnterPressed, AddressOf OnBarcodeEnter

        _hint.AutoSize = False
        _hint.Size = New Size(Theme.S(480), Theme.S(38))
        _hint.Location = New Point(Theme.S(40), Theme.S(332))
        _hint.Font = Theme.FontSmall
        _hint.ForeColor = Theme.TextMuted
        _hint.Text = If(_isEdit,
            "Changing the barcode means new labels must be printed.",
            "Leave the barcode empty and the system makes one for you. For packaged goods, scan the manufacturer's barcode here instead.")

        Message.Location = New Point(Theme.S(40), Theme.S(376))
        Message.Size = New Size(Theme.S(480), Theme.S(34))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(140), Theme.S(44))
        _cancel.Location = New Point(Theme.S(220), Theme.S(420))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = If(_isEdit, "Save changes", "Add product")
        _save.Size = New Size(Theme.S(140), Theme.S(44))
        _save.Location = New Point(Theme.S(380), Theme.S(420))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_name)
        Controls.Add(_category)
        Controls.Add(_size)
        Controls.Add(_barcode)
        Controls.Add(_hint)
        Controls.Add(_cancel)
        Controls.Add(_save)
        ClientSize = New Size(Theme.S(560), Theme.S(420) + Theme.S(44) + Theme.S(28))

        AcceptButton = _save
        CancelButton = _cancel
    End Sub

    Private Sub LoadCategories(selectId As Integer)
        Try
            Dim names As New List(Of String)()
            Dim selectName As String = ""
            For Each r As DataRow In CategoryRepository.GetAll().Rows
                Dim n As String = r("name").ToString()
                Dim id As Integer = Convert.ToInt32(r("id"))
                names.Add(n)
                _catIds(n) = id
                If id = selectId Then selectName = n
            Next
            _category.SetChoices(names.ToArray())
            If selectName.Length > 0 Then _category.Value = selectName
            If names.Count = 0 Then ShowMessage("Create a category first (Products > Categories).", True)
        Catch ex As Exception
            ShowMessage("Could not load the categories: " & ex.Message, True)
        End Try
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _name.Value.Length > 0 AndAlso Not _isEdit Then
            _size.FocusInput()
        Else
            _name.FocusInput()
        End If
    End Sub

    Private Sub OnBarcodeEnter()
        SelectNextControl(_barcode, True, True, True, True)
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)

        Dim problem As String = InputField.CheckAll(_name, _size, _barcode)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        Dim catName As String = _category.Value
        If catName.Length = 0 OrElse Not _catIds.ContainsKey(catName) Then
            ShowMessage("Choose a category (use the Categories button to create one).", True)
            Return
        End If
        Dim categoryId As Integer = _catIds(catName)

        _save.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim result As SaveResult
            If _isEdit Then
                result = ProductRepository.UpdateProduct(_productId, categoryId, _name.Value, _size.Value, _barcode.Value)
            Else
                result = ProductRepository.CreateProduct(categoryId, _name.Value, _size.Value, _barcode.Value)
            End If

            Select Case result
                Case SaveResult.Ok
                    Dim label As String = _name.Value.Trim() & If(_size.Value.Trim().Length > 0, " " & MeasurementRules.Normalize(_size.Value), "")
                    AuditLog.Write(If(_isEdit, "EDIT", "ADD"), "Products", If(_isEdit, "Edited", "Added") & " product '" & label & "'")
                    DialogResult = DialogResult.OK
                Case SaveResult.Duplicate
                    ShowMessage("This product already exists with the same category and size.", True)
                    _size.FocusInput()
                Case SaveResult.BarcodeTaken
                    ShowMessage("That barcode is already used by another product.", True)
                    _barcode.FocusInput()
                Case SaveResult.NotFound
                    ShowMessage("This product no longer exists.", True)
                Case SaveResult.Invalid
                    ShowMessage("Enter the product name and its size / measurement.", True)
                Case Else
                    ShowMessage("Could not save this product.", True)
            End Select
        Catch ex As Exception
            ShowMessage("Could not save: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            If DialogResult <> DialogResult.OK Then _save.Enabled = True
        End Try
    End Sub
End Class

''' <summary>The list of categories: add, rename, delete. (Categories no longer need a screen of their own.)</summary>
Public Class CategoriesDialog
    Inherits ModalDialog

    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _name As New InputField("Category name", InputKind.Title, 270, True)
    Private ReadOnly _desc As New InputField("Description (optional)", InputKind.Text, 270, False)
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _clear As New MaroonButton()
    Private ReadOnly _delete As New MaroonButton()
    Private ReadOnly _close As New MaroonButton()

    Private _selectedId As Integer
    Private _loading As Boolean

    Public Sub New()
        MyBase.New("Categories", "Group your products. A category needs a name; the description is optional.", 640, 620)

        _grid.Location = New Point(Theme.S(40), Theme.S(108))
        _grid.Size = New Size(Theme.S(560), Theme.S(210))
        GridStyle.Apply(_grid, 42)
        Dim c1 As New DataGridViewTextBoxColumn()
        c1.Name = "name" : c1.HeaderText = "CATEGORY" : c1.DataPropertyName = "name" : c1.FillWeight = 34
        c1.SortMode = DataGridViewColumnSortMode.NotSortable
        Dim c2 As New DataGridViewTextBoxColumn()
        c2.Name = "desc" : c2.HeaderText = "DESCRIPTION" : c2.DataPropertyName = "description" : c2.FillWeight = 46
        c2.SortMode = DataGridViewColumnSortMode.NotSortable
        Dim c3 As New DataGridViewTextBoxColumn()
        c3.Name = "count" : c3.HeaderText = "PRODUCTS" : c3.DataPropertyName = "product_count" : c3.FillWeight = 20
        c3.SortMode = DataGridViewColumnSortMode.NotSortable
        _grid.Columns.Add(c1)
        _grid.Columns.Add(c2)
        _grid.Columns.Add(c3)
        AddHandler _grid.SelectionChanged, AddressOf OnGridSelectionChanged

        _name.Location = New Point(Theme.S(40), Theme.S(336))
        _desc.Location = New Point(Theme.S(330), Theme.S(336))
        _desc.MaxLength = 255

        _save.Size = New Size(Theme.S(160), Theme.S(42))
        _save.Location = New Point(Theme.S(40), Theme.S(424))
        AddHandler _save.Click, AddressOf OnSave

        _clear.Text = "New"
        _clear.MakeSecondary()
        _clear.Size = New Size(Theme.S(100), Theme.S(42))
        _clear.Location = New Point(Theme.S(212), Theme.S(424))
        AddHandler _clear.Click, AddressOf OnClear

        _delete.Text = "Delete"
        _delete.MakeDanger()
        _delete.Size = New Size(Theme.S(110), Theme.S(42))
        _delete.Location = New Point(Theme.S(324), Theme.S(424))
        AddHandler _delete.Click, AddressOf OnDelete

        Message.Location = New Point(Theme.S(40), Theme.S(478))
        Message.Size = New Size(Theme.S(560), Theme.S(34))

        _close.Text = "Close"
        _close.MakeSecondary()
        _close.Size = New Size(Theme.S(120), Theme.S(42))
        _close.Location = New Point(Theme.S(480), Theme.S(528))
        AddHandler _close.Click, AddressOf OnClose

        Controls.Add(_grid)
        Controls.Add(_name)
        Controls.Add(_desc)
        Controls.Add(_save)
        Controls.Add(_clear)
        Controls.Add(_delete)
        Controls.Add(_close)
        ClientSize = New Size(Theme.S(640), Theme.S(528) + Theme.S(42) + Theme.S(26))

        CancelButton = _close
        AcceptButton = _save
        LoadCategories(0)
    End Sub

    Private Sub OnClose(sender As Object, e As EventArgs)
        DialogResult = DialogResult.OK
    End Sub

    Private Sub LoadCategories(selectId As Integer)
        _loading = True
        Try
            Dim dt As DataTable = CategoryRepository.GetAll()
            _grid.DataSource = dt
            _grid.ClearSelection()
            _selectedId = 0
            For Each r As DataGridViewRow In _grid.Rows
                Dim drv As DataRowView = TryCast(r.DataBoundItem, DataRowView)
                If drv IsNot Nothing AndAlso Convert.ToInt32(drv("id")) = selectId Then
                    r.Selected = True
                    _grid.CurrentCell = r.Cells(0)
                    _selectedId = selectId
                    Exit For
                End If
            Next
        Catch ex As Exception
            ShowMessage("Could not load the categories: " & ex.Message, True)
        Finally
            _loading = False
        End Try
        FillFromSelection()
    End Sub

    Private Sub OnGridSelectionChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        _selectedId = 0
        If _grid.SelectedRows.Count > 0 Then
            Dim drv As DataRowView = TryCast(_grid.SelectedRows(0).DataBoundItem, DataRowView)
            If drv IsNot Nothing Then _selectedId = Convert.ToInt32(drv("id"))
        End If
        FillFromSelection()
    End Sub

    Private Sub FillFromSelection()
        If _selectedId > 0 AndAlso _grid.SelectedRows.Count > 0 Then
            Dim drv As DataRowView = TryCast(_grid.SelectedRows(0).DataBoundItem, DataRowView)
            If drv IsNot Nothing Then
                _name.Value = drv("name").ToString()
                _desc.Value = If(drv("description") Is DBNull.Value, "", drv("description").ToString())
            End If
            _save.Text = "Save changes"
            _delete.Enabled = True
        Else
            _name.Value = ""
            _desc.Value = ""
            _save.Text = "Add category"
            _delete.Enabled = False
        End If
        ShowMessage("", False)
    End Sub

    Private Sub OnClear(sender As Object, e As EventArgs)
        _grid.ClearSelection()
        _selectedId = 0
        FillFromSelection()
        _name.FocusInput()
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Dim problem As String = InputField.CheckAll(_name, _desc)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        Try
            Dim result As SaveResult
            Dim editing As Boolean = (_selectedId > 0)
            If editing Then
                result = CategoryRepository.Update(_selectedId, _name.Value, _desc.Value)
            Else
                result = CategoryRepository.Create(_name.Value, _desc.Value)
            End If

            Select Case result
                Case SaveResult.Ok
                    AuditLog.Write(If(editing, "EDIT", "ADD"), "Products", If(editing, "Edited", "Added") & " category '" & _name.Value.Trim() & "'")
                    LoadCategories(0)
                    ShowMessage(If(editing, "Category saved.", "Category added."), False)
                Case SaveResult.Duplicate
                    ShowMessage("A category with this name already exists.", True)
                    _name.FocusInput()
                Case SaveResult.NotFound
                    ShowMessage("This category no longer exists.", True)
                    LoadCategories(0)
                Case Else
                    ShowMessage("Could not save the category.", True)
            End Select
        Catch ex As Exception
            ShowMessage("Could not save: " & ex.Message, True)
        End Try
    End Sub

    Private Sub OnDelete(sender As Object, e As EventArgs)
        If _selectedId <= 0 Then Return
        Dim nm As String = _name.Value
        If MessageBox.Show(Me, "Delete the category '" & nm & "'?", "Categories", MessageBoxButtons.YesNo,
                           MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return

        Try
            Select Case CategoryRepository.Delete(_selectedId)
                Case SaveResult.Ok
                    AuditLog.Write("DELETE", "Products", "Deleted category '" & nm & "'")
                    LoadCategories(0)
                    ShowMessage("Category deleted.", False)
                Case SaveResult.InUse
                    ShowMessage("Products still use this category. Move or delete them first.", True)
                Case Else
                    LoadCategories(0)
            End Select
        Catch ex As Exception
            ShowMessage("Could not delete: " & ex.Message, True)
        End Try
    End Sub
End Class
