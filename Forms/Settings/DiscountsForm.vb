Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Settings > Discounts. A name and a percentage; actions: add, edit, activate / deactivate, delete.
''' Only active discounts can be picked at the POS. Superadmin only.
''' </summary>
Public Class DiscountsForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly DetailsWidth As Integer = Theme.S(340)
    Private Shared ReadOnly ContentW As Integer = Theme.S(268)

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly BigNameFont As Font = Theme.MakeFont("Segoe UI Semibold", 14.0F)
    Private Shared ReadOnly PercentFont As Font = Theme.MakeFont("Segoe UI Semibold", 15.0F)
    Private Shared ReadOnly HugeFont As Font = Theme.MakeFont("Segoe UI Semibold", 40.0F)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _search As New SearchBox("Search discounts...")
    Private ReadOnly _add As New MaroonButton()

    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cardTotal As New StatCard()
    Private ReadOnly _cardActive As New StatCard()

    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()

    Private ReadOnly _details As New Panel()
    Private ReadOnly _scroll As New FlowLayoutPanel()
    Private ReadOnly _bar As New FlowLayoutPanel()
    Private ReadOnly _dPercent As New Label()
    Private ReadOnly _dName As New Label()
    Private ReadOnly _pillRow As New Panel()
    Private ReadOnly _pill As New PillLabel()
    Private ReadOnly _vUsed As Label
    Private ReadOnly _vCreated As Label
    Private ReadOnly _btnEdit As New MaroonButton()
    Private ReadOnly _btnToggle As New MaroonButton()
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
        _vUsed = BuildRow("USED IN SALES", 24)
        _vCreated = BuildRow("ADDED", 24)
        BuildActions()

        Controls.Add(_listHost)
        Controls.Add(_details)
        Controls.Add(_stats)
        Controls.Add(_header)
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"discounts", "sales"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadDiscounts()
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Discounts"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "The discounts a cashier can pick at the POS. Inactive ones are hidden there"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _search.Size = New Size(Theme.S(280), Theme.S(40))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _add.Text = "+  Add discount"
        _add.Size = New Size(Theme.S(160), Theme.S(40))
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

        For Each c As StatCard In New StatCard() {_cardTotal, _cardActive}
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            _stats.Controls.Add(c)
        Next
        _cardTotal.Caption = "Discounts"
        _cardActive.Caption = "Active"
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
        AddColumn("discount", "DISCOUNT", "name", 44)
        AddColumn("percent", "PERCENTAGE", "percentage", 20)
        AddColumn("status", "STATUS", "status", 18)
        AddColumn("created", "ADDED", "created_at", 18)

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
        DetailsLayout.Setup(_details, _scroll, _bar, New Padding(Theme.S(28), Theme.S(26), Theme.S(12), Theme.S(16)))
        AddHandler _scroll.Paint, AddressOf OnDetailsPaint

        _dPercent.AutoSize = False
        _dPercent.Size = New Size(ContentW, Theme.S(80))
        _dPercent.TextAlign = ContentAlignment.MiddleCenter
        _dPercent.Font = HugeFont
        _dPercent.ForeColor = Theme.Maroon
        _dPercent.Margin = New Padding(0)
        _scroll.Controls.Add(_dPercent)

        _dName.AutoSize = False
        _dName.Size = New Size(ContentW, Theme.S(56))
        _dName.TextAlign = ContentAlignment.TopCenter
        _dName.Font = BigNameFont
        _dName.ForeColor = Theme.TextDark
        _dName.Margin = New Padding(0, Theme.S(4), 0, 0)
        _scroll.Controls.Add(_dName)

        _pillRow.Size = New Size(ContentW, Theme.S(32))
        _pillRow.BackColor = _details.BackColor
        _pillRow.Margin = New Padding(0, 0, 0, Theme.S(8))
        _pillRow.Controls.Add(_pill)
        _scroll.Controls.Add(_pillRow)
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
        _btnEdit.Text = "Edit discount"
        _btnToggle.Text = "Deactivate"
        _btnToggle.MakeSecondary()
        _btnDelete.Text = "Delete discount"
        _btnDelete.MakeDanger()

        AddHandler _btnEdit.Click, AddressOf OnEdit
        AddHandler _btnToggle.Click, AddressOf OnToggle
        AddHandler _btnDelete.Click, AddressOf OnDelete

        Dim first As Boolean = True
        For Each b As MaroonButton In New MaroonButton() {_btnEdit, _btnToggle, _btnDelete}
            b.Size = New Size(ContentW, Theme.S(40))
            b.Margin = New Padding(0, If(first, 0, Theme.S(8)), 0, 0)
            first = False
            _bar.Controls.Add(b)
        Next
    End Sub

    ' ------------------------------------------------------------------ data
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadDiscounts()
    End Sub

    Private Sub OnSearchChanged()
        LoadDiscounts()
    End Sub

    Private Sub LoadDiscounts()
        If _loading Then Return
        _loading = True
        Try
            Dim keep As Integer = _selectedId
            Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex

            Dim dt As DataTable = DiscountRepository.GetAll(_search.Query)
            _grid.DataSource = dt

            Dim counts As DiscountCounts = DiscountRepository.GetCounts()
            _cardTotal.Value = counts.Total.ToString()
            _cardActive.Value = counts.Active.ToString()

            If dt.Rows.Count = 0 Then
                _empty.Text = If(_search.Query.Length > 0, "No discounts match your search.", "No discounts yet. Click Add discount.")
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
            _empty.Text = "Could not load the discounts: " & ex.Message
            _empty.Visible = True
            _empty.BringToFront()
        Finally
            _loading = False
        End Try
        ShowDetails()
    End Sub

    ' ------------------------------------------------------------------ list
    Private Shared Function StatusText(status As String) As String
        Return If(status = "active", "Active", "Inactive")
    End Function

    Private Sub OnCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        If colName <> "discount" AndAlso colName <> "percent" AndAlso colName <> "status" Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Dim c As Rectangle = e.CellBounds
        Dim active As Boolean = (row("status").ToString() = "active")
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding

        Select Case colName
            Case "discount"
                Dim used As Integer = Convert.ToInt32(row("used_count"))
                TextRenderer.DrawText(e.Graphics, row("name").ToString(), NameFont, New Rectangle(c.X + 12, c.Y + 11, c.Width - 20, 20),
                                      If(active, Theme.TextDark, Theme.TextMuted), flags)
                TextRenderer.DrawText(e.Graphics, "Used in " & used.ToString() & If(used = 1, " sale", " sales"), Theme.FontSmall,
                                      New Rectangle(c.X + Theme.S(12), c.Y + Theme.S(33), c.Width - Theme.S(20), Theme.S(18)), Theme.TextMuted, flags)

            Case "percent"
                TextRenderer.DrawText(e.Graphics, Percent.Format(row("percentage")), PercentFont,
                                      New Rectangle(c.X + Theme.S(10), c.Y, c.Width - Theme.S(14), c.Height), If(active, Theme.Maroon, Theme.TextMuted),
                                      TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)

            Case "status"
                Dim t As String = StatusText(row("status").ToString())
                Pills.Draw(e.Graphics, c, t, Pills.StatusBack(t), Pills.StatusFore(t))
        End Select
        e.Handled = True
    End Sub

    Private Sub OnCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        If _grid.Columns(e.ColumnIndex).Name = "created" Then
            e.Value = Convert.ToDateTime(e.Value).ToString("MMM d, yyyy")
            e.FormattingApplied = True
        End If
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
        Dim active As Boolean = (row("status").ToString() = "active")
        _dPercent.Text = Percent.Format(row("percentage"))
        _dName.Text = row("name").ToString()

        Dim t As String = StatusText(row("status").ToString())
        _pill.SetPill(t, Pills.StatusBack(t), Pills.StatusFore(t))
        _pill.Location = New Point(Math.Max(0, (_pillRow.Width - _pill.Width) \ 2), Theme.S(4))

        Dim used As Integer = Convert.ToInt32(row("used_count"))
        _vUsed.Text = used.ToString() & If(used = 1, " sale", " sales")
        _vCreated.Text = Convert.ToDateTime(row("created_at")).ToString("MMM d, yyyy")
        _btnToggle.Text = If(active, "Deactivate", "Activate")
    End Sub

    Private Sub OnDetailsPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _details.Height)
        End Using
    End Sub

    ' ------------------------------------------------------------------ actions
    Private Sub OnAdd(sender As Object, e As EventArgs)
        Using dlg As New DiscountEditForm()
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadDiscounts()
        End Using
    End Sub

    Private Sub OnEdit(sender As Object, e As EventArgs)
        If _selectedId <= 0 Then Return
        Using dlg As New DiscountEditForm(_selectedId)
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadDiscounts()
        End Using
    End Sub

    Private Sub OnToggle(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim name As String = row("name").ToString()
        Dim wasActive As Boolean = (row("status").ToString() = "active")

        Try
            If DiscountRepository.SetActive(id, Not wasActive) = SaveResult.Ok Then
                AuditLog.Write(If(wasActive, "DEACTIVATE", "ACTIVATE"), "Discounts",
                               If(wasActive, "Deactivated", "Activated") & " discount '" & name & "'")
            End If
        Catch ex As Exception
            Notify(Me, "Discounts", "Could not change the discount: " & ex.Message, MessageBoxIcon.Error)
        End Try
        LoadDiscounts()
    End Sub

    Private Sub OnDelete(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim name As String = row("name").ToString()

        If Not Confirm(Me, "Discounts", "Delete the discount '" & name & "' (" & Percent.Format(row("percentage")) & ")?" & Environment.NewLine & Environment.NewLine &
                       "Past sales keep the name and amount of the discount they used." & Environment.NewLine &
                       "If you only want to hide it at the POS, use Deactivate instead.", MessageBoxIcon.Warning) Then Return

        Try
            If DiscountRepository.Delete(id) = SaveResult.Ok Then
                AuditLog.Write("DELETE", "Discounts", "Deleted discount '" & name & "'")
            End If
        Catch ex As Exception
            Notify(Me, "Discounts", "Could not delete the discount: " & ex.Message, MessageBoxIcon.Error)
        End Try
        _selectedId = 0
        LoadDiscounts()
    End Sub

End Class
