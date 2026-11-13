Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' File Maintenance > Suppliers. Same person / company / phone may repeat (branches);
''' only the address must be unique. Refreshes by itself (ILiveForm).
''' </summary>
Public Class SuppliersForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly DetailsWidth As Integer = Theme.S(340)
    Private Shared ReadOnly ContentW As Integer = Theme.S(268)

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly BigNameFont As Font = Theme.MakeFont("Segoe UI Semibold", 14.0F)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _search As New SearchBox("Search suppliers...")
    Private ReadOnly _add As New MaroonButton()

    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cardSuppliers As New StatCard()
    Private ReadOnly _cardCompanies As New StatCard()

    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()

    Private ReadOnly _details As New Panel()
    Private ReadOnly _scroll As New FlowLayoutPanel()
    Private ReadOnly _bar As New FlowLayoutPanel()
    Private ReadOnly _avatar As New Avatar()
    Private ReadOnly _dName As New Label()
    Private ReadOnly _vPerson As Label
    Private ReadOnly _vNumber As Label
    Private ReadOnly _vEmail As Label
    Private ReadOnly _vAddress As Label
    Private ReadOnly _vBranches As Label
    Private ReadOnly _vDeliveries As Label
    Private ReadOnly _btnEdit As New MaroonButton()
    Private ReadOnly _btnBranch As New MaroonButton()
    Private ReadOnly _btnDelete As New MaroonButton()

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
        _vPerson = BuildRow("CONTACT PERSON", 24)
        _vNumber = BuildRow("CONTACT NUMBER", 24)
        _vEmail = BuildRow("EMAIL", 24)
        _vAddress = BuildRow("ADDRESS", 62)
        _vBranches = BuildRow("BRANCHES OF THIS COMPANY", 24)
        _vDeliveries = BuildRow("DELIVERIES RECEIVED", 24)
        BuildActions()

        Controls.Add(_listHost)
        Controls.Add(_details)
        Controls.Add(_stats)
        Controls.Add(_header)
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"suppliers", "deliveries"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadSuppliers()
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Suppliers"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "The companies you buy from. A company can have several branches, each with its own address"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _search.Size = New Size(Theme.S(280), Theme.S(40))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _add.Text = "+  Add supplier"
        _add.Size = New Size(Theme.S(150), Theme.S(40))
        AddHandler _add.Click, AddressOf OnAdd

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)
        _header.Controls.Add(_search)
        _header.Controls.Add(_add)
        AddHandler _header.Resize, AddressOf OnHeaderResize
        OnHeaderResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnHeaderResize(sender As Object, e As EventArgs)
        _add.Location = New Point(Math.Max(Theme.S(300), _header.ClientSize.Width - Theme.S(32) - _add.Width), Theme.S(30))
        _search.Location = New Point(Math.Max(0, _add.Left - Theme.S(16) - _search.Width), Theme.S(30))
    End Sub

    Private Sub BuildStats()
        _stats.Dock = DockStyle.Top
        _stats.Height = Theme.S(84)
        _stats.BackColor = Color.White
        _stats.FlowDirection = FlowDirection.LeftToRight
        _stats.WrapContents = False
        _stats.Padding = New Padding(Theme.S(32), Theme.S(4), 0, 0)

        For Each c As StatCard In New StatCard() {_cardSuppliers, _cardCompanies}
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            _stats.Controls.Add(c)
        Next
        _cardSuppliers.Caption = "Suppliers (branches)"
        _cardCompanies.Caption = "Companies"
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
        AddColumn("company", "COMPANY", "company_name", 32)
        AddColumn("contact", "CONTACT NUMBER", "contact_number", 16)
        AddColumn("email", "EMAIL", "email", 22)
        AddColumn("address", "ADDRESS", "company_address", 30)

        AddHandler _grid.CellPainting, AddressOf OnCellPainting
        AddHandler _grid.CellFormatting, AddressOf OnCellFormatting
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
        _dName.Margin = New Padding(0, 0, 0, Theme.S(4))
        _scroll.Controls.Add(_dName)
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
        _btnEdit.Text = "Edit supplier"
        _btnBranch.Text = "Add another branch"
        _btnBranch.MakeSecondary()
        _btnDelete.Text = "Delete supplier"
        _btnDelete.MakeDanger()

        AddHandler _btnEdit.Click, AddressOf OnEdit
        AddHandler _btnBranch.Click, AddressOf OnAddBranch
        AddHandler _btnDelete.Click, AddressOf OnDelete

        Dim first As Boolean = True
        For Each b As MaroonButton In New MaroonButton() {_btnEdit, _btnBranch, _btnDelete}
            b.Size = New Size(ContentW, Theme.S(40))
            b.Margin = New Padding(0, If(first, 0, Theme.S(8)), 0, 0)
            first = False
            _bar.Controls.Add(b)
        Next
    End Sub

    ' ------------------------------------------------------------------ data
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadSuppliers()
    End Sub

    Private Sub OnSearchChanged()
        LoadSuppliers()
    End Sub

    Private Sub LoadSuppliers()
        If _loading Then Return
        _loading = True
        Try
            Dim keep As Integer = _selectedId
            Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex

            Dim dt As DataTable = SupplierRepository.GetSuppliers(_search.Query)
            _grid.DataSource = dt

            Dim counts As SupplierCounts = SupplierRepository.GetCounts()
            _cardSuppliers.Value = counts.Suppliers.ToString()
            _cardCompanies.Value = counts.Companies.ToString()

            If dt.Rows.Count = 0 Then
                _empty.Text = If(_search.Query.Length > 0, "No suppliers match your search.", "No suppliers yet. Click Add supplier.")
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
            _empty.Text = "Could not load suppliers: " & ex.Message
            _empty.Visible = True
            _empty.BringToFront()
        Finally
            _loading = False
        End Try
        ShowDetails()
    End Sub

    ' ------------------------------------------------------------------ grid
    Private Sub OnCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        If _grid.Columns(e.ColumnIndex).Name <> "company" Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim c As Rectangle = e.CellBounds
        Dim company As String = row("company_name").ToString()
        Dim circle As New Rectangle(c.X + Theme.S(12), c.Y + (c.Height - Theme.S(38)) \ 2, Theme.S(38), Theme.S(38))
        Avatar.PaintInitials(e.Graphics, circle, company, 10.0F)

        Dim line2 As String = row("name").ToString()
        If Convert.ToInt32(row("branches")) > 1 Then line2 &= "   |   " & row("branches").ToString() & " branches"

        Dim textX As Integer = circle.Right + Theme.S(12)
        Dim textW As Integer = Math.Max(Theme.S(10), c.Right - textX - Theme.S(8))
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
        TextRenderer.DrawText(e.Graphics, company, NameFont, New Rectangle(textX, c.Y + 11, textW, 20), Theme.TextDark, flags)
        TextRenderer.DrawText(e.Graphics, line2, Theme.FontSmall, New Rectangle(textX, c.Y + 32, textW, 18), Theme.TextMuted, flags)
        e.Handled = True
    End Sub

    Private Sub OnCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        If colName = "contact" OrElse colName = "email" Then
            e.Value = TextOrDash(e.Value)
            e.FormattingApplied = True
        End If
    End Sub

    Private Shared Function TextOrDash(o As Object) As String
        If o Is Nothing OrElse o Is DBNull.Value Then Return "-"
        Dim s As String = o.ToString().Trim()
        Return If(s.Length = 0, "-", s)
    End Function

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
        Dim company As String = row("company_name").ToString()
        _dName.Text = company
        _avatar.DisplayName = company
        _vPerson.Text = row("name").ToString()
        _vNumber.Text = TextOrDash(row("contact_number"))
        _vEmail.Text = TextOrDash(row("email"))
        _vAddress.Text = row("company_address").ToString()

        Dim branches As Integer = Convert.ToInt32(row("branches"))
        _vBranches.Text = If(branches > 1, branches.ToString() & " branches", "Just this one")
        _vDeliveries.Text = Convert.ToInt32(row("deliveries")).ToString()
    End Sub

    Private Sub OnDetailsPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _details.Height)
        End Using
    End Sub

    ' ------------------------------------------------------------------ actions
    Private Sub OnAdd(sender As Object, e As EventArgs)
        Using dlg As New SupplierEditForm()
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadSuppliers()
        End Using
    End Sub

    Private Sub OnAddBranch(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Using dlg As New SupplierEditForm(row("company_name").ToString(), row("name").ToString(),
                                          If(row("contact_number") Is DBNull.Value, "", row("contact_number").ToString()),
                                          If(row("email") Is DBNull.Value, "", row("email").ToString()))
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadSuppliers()
        End Using
    End Sub

    Private Sub OnEdit(sender As Object, e As EventArgs)
        If _selectedId <= 0 Then Return
        Using dlg As New SupplierEditForm(_selectedId)
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadSuppliers()
        End Using
    End Sub

    Private Sub OnDelete(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim company As String = row("company_name").ToString()
        Dim address As String = row("company_address").ToString()

        If Not Confirm(Me, "Suppliers", "Delete the supplier " & company & Environment.NewLine & address & "?" &
                       Environment.NewLine & Environment.NewLine & "This cannot be undone.", MessageBoxIcon.Warning) Then Return

        Try
            Select Case SupplierRepository.DeleteSupplier(id)
                Case SaveResult.Ok
                    AuditLog.Write("DELETE", "Suppliers", "Deleted supplier '" & company & "' (" & address & ")")
                Case SaveResult.InUse
                    Notify(Me, "Suppliers", "This supplier already has deliveries, so it cannot be deleted." & Environment.NewLine &
                           "Its delivery history must stay in the reports.", MessageBoxIcon.Information)
                Case Else
                    ' already gone
            End Select
        Catch ex As Exception
            Notify(Me, "Suppliers", "Could not delete the supplier: " & ex.Message, MessageBoxIcon.Error)
        End Try
        _selectedId = 0
        LoadSuppliers()
    End Sub

End Class
