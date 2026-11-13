Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Customer Accounts: members with a loyalty card, their points and their history.
''' Add / edit / delete, and view, save or print the card.
''' </summary>
Public Class CustomersForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly DetailsWidth As Integer = Theme.S(360)
    Private Shared ReadOnly ContentW As Integer = Theme.S(288)

    Private Shared ReadOnly NameFont As Font = Theme.MakeFont("Segoe UI Semibold", 10.0F)
    Private Shared ReadOnly PointsFont As Font = Theme.MakeFont("Segoe UI Semibold", 13.0F)
    Private Shared ReadOnly BigNameFont As Font = Theme.MakeFont("Segoe UI Semibold", 14.0F)
    Private Shared ReadOnly BigPointsFont As Font = Theme.MakeFont("Segoe UI Semibold", 26.0F)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _search As New SearchBox("Search name, number, card...")
    Private ReadOnly _add As New MaroonButton()

    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cardMembers As New StatCard()
    Private ReadOnly _cardPoints As New StatCard()
    Private ReadOnly _cardNew As New StatCard()

    Private ReadOnly _listHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()
    Private ReadOnly _lblSms As New Label()

    Private ReadOnly _details As New Panel()
    Private ReadOnly _scroll As New FlowLayoutPanel()
    Private ReadOnly _bar As New FlowLayoutPanel()
    Private ReadOnly _cardView As New CardPreview()
    Private ReadOnly _dName As New Label()
    Private ReadOnly _vPoints As Label
    Private ReadOnly _vContact As Label
    Private ReadOnly _vAddress As Label
    Private ReadOnly _vSince As Label
    Private ReadOnly _vLast As Label
    Private ReadOnly _vTotals As Label
    Private ReadOnly _vActivity As Label
    Private ReadOnly _btnCard As New MaroonButton()
    Private ReadOnly _btnEdit As New MaroonButton()
    Private ReadOnly _btnDelete As New MaroonButton()

    Private _loading As Boolean
    Private _selectedId As Integer
    Private ReadOnly _model As New CardModel()

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildStats()
        BuildGrid()
        BuildDetailsTop()
        _vPoints = BuildRow("POINTS", 40, BigPointsFont, Theme.Maroon)
        _vContact = BuildRow("MOBILE NUMBER", 22, Theme.FontBase, Theme.TextDark)
        _vAddress = BuildRow("ADDRESS", 44, Theme.FontBase, Theme.TextDark)
        _vSince = BuildRow("MEMBER SINCE", 22, Theme.FontBase, Theme.TextDark)
        _vLast = BuildRow("LAST PURCHASE", 22, Theme.FontBase, Theme.TextDark)
        _vTotals = BuildRow("POINTS EARNED / USED", 22, Theme.FontBase, Theme.TextDark)
        _vActivity = BuildRow("RECENT POINTS", 92, Theme.FontSmall, Theme.TextDark)
        BuildActions()

        Controls.Add(_listHost)
        Controls.Add(_details)
        Controls.Add(_stats)
        Controls.Add(_header)
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"customers", "points_ledger"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadCustomers()
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Customer Accounts"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "Loyalty members, their cards and their points"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _search.Size = New Size(Theme.S(300), Theme.S(40))
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _add.Text = "+  Add customer"
        _add.Size = New Size(Theme.S(170), Theme.S(40))
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

        For Each c As StatCard In New StatCard() {_cardMembers, _cardPoints, _cardNew}
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            _stats.Controls.Add(c)
        Next
        _cardMembers.Caption = "Members"
        _cardPoints.Caption = "Points held"
        _cardNew.Caption = "New this month"
    End Sub

    Private Sub AddColumn(name As String, headerText As String, dataProp As String, weight As Single)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = name
        c.HeaderText = headerText
        c.DataPropertyName = dataProp
        c.FillWeight = weight
        c.MinimumWidth = 90
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        _grid.Columns.Add(c)
    End Sub

    Private Sub BuildGrid()
        _listHost.Dock = DockStyle.Fill
        _listHost.BackColor = Color.White
        _listHost.Padding = New Padding(Theme.S(32), Theme.S(4), Theme.S(16), Theme.S(8))

        _lblSms.Dock = DockStyle.Bottom
        _lblSms.Height = Theme.S(26)
        _lblSms.Font = Theme.FontSmall
        _lblSms.ForeColor = Theme.TextMuted
        _lblSms.TextAlign = ContentAlignment.MiddleLeft

        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 64)
        AddColumn("customer", "CUSTOMER", "name", 34)
        AddColumn("contact", "CONTACT", "contact_number", 30)
        AddColumn("points", "POINTS", "points_balance", 16)
        AddColumn("last", "LAST PURCHASE", "last_purchase_at", 20)

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
        _listHost.Controls.Add(_lblSms)
    End Sub

    Private Sub BuildDetailsTop()
        _details.Dock = DockStyle.Right
        _details.Width = DetailsWidth
        _details.BackColor = Color.FromArgb(251, 249, 249)
        DetailsLayout.Setup(_details, _scroll, _bar, New Padding(Theme.S(24), Theme.S(20), Theme.S(8), Theme.S(16)))
        AddHandler _scroll.Paint, AddressOf OnDetailsPaint

        _cardView.Size = New Size(ContentW, Theme.S(188))
        _cardView.BackColor = _details.BackColor
        _cardView.Margin = New Padding(0)
        _cardView.Model = _model
        _scroll.Controls.Add(_cardView)

        _dName.AutoSize = False
        _dName.Size = New Size(ContentW, Theme.S(32))
        _dName.TextAlign = ContentAlignment.MiddleLeft
        _dName.Font = BigNameFont
        _dName.ForeColor = Theme.TextDark
        _dName.Margin = New Padding(0, Theme.S(8), 0, 0)
        _scroll.Controls.Add(_dName)
    End Sub

    Private Function BuildRow(caption As String, valueHeight As Integer, f As Font, color As Color) As Label
        Dim cap As New Label()
        cap.AutoSize = False
        cap.Size = New Size(ContentW, Theme.S(18))
        cap.Font = Theme.FontCaption
        cap.ForeColor = Theme.TextMuted
        cap.Text = caption
        cap.Margin = New Padding(0, Theme.S(10), 0, 0)
        _scroll.Controls.Add(cap)

        Dim v As New Label()
        v.AutoSize = False
        v.Size = New Size(ContentW, valueHeight)
        v.Font = f
        v.ForeColor = color
        v.Margin = New Padding(0)
        _scroll.Controls.Add(v)
        Return v
    End Function

    Private Sub BuildActions()
        _btnCard.Text = "View / print card"
        _btnEdit.Text = "Edit customer"
        _btnEdit.MakeSecondary()
        _btnDelete.Text = "Delete customer"
        _btnDelete.MakeDanger()

        AddHandler _btnCard.Click, AddressOf OnCard
        AddHandler _btnEdit.Click, AddressOf OnEdit
        AddHandler _btnDelete.Click, AddressOf OnDelete

        Dim first As Boolean = True
        For Each b As MaroonButton In New MaroonButton() {_btnCard, _btnEdit, _btnDelete}
            b.Size = New Size(ContentW, Theme.S(40))
            b.Margin = New Padding(0, If(first, 0, Theme.S(8)), 0, 0)
            first = False
            _bar.Controls.Add(b)
        Next
    End Sub

    ' ------------------------------------------------------------------ data
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadCustomers()
    End Sub

    Private Sub OnSearchChanged()
        LoadCustomers()
    End Sub

    Private Sub LoadCustomers()
        If _loading Then Return
        _loading = True
        Try
            Dim keep As Integer = _selectedId
            Dim scroll As Integer = _grid.FirstDisplayedScrollingRowIndex

            PerfLog.Mark("customers: load start")
            Dim dt As DataTable = CustomerRepository.GetAll(_search.Query)
            PerfLog.Mark("customers: list queried")
            _grid.DataSource = dt

            Dim counts As CustomerCounts = CustomerRepository.GetCounts()
            _cardMembers.Value = counts.Total.ToString()
            _cardPoints.Value = Loyalty.Fmt(counts.Points)
            _cardNew.Value = counts.NewThisMonth.ToString()
            PerfLog.Mark("customers: counts done")
            _lblSms.Text = SmsService.StatusText()
            PerfLog.Mark("customers: sms status done")

            If dt.Rows.Count = 0 Then
                _empty.Text = If(_search.Query.Length > 0, "No customer matches your search.", "No customers yet. Click Add customer.")
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
            PerfLog.Mark("customers: grid ready")
        Catch ex As Exception
            _empty.Text = "Could not load the customers: " & ex.Message
            _empty.Visible = True
            _empty.BringToFront()
        Finally
            _loading = False
        End Try
        ShowDetails()
    End Sub

    ' ------------------------------------------------------------------ list
    Private Sub OnCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        If colName <> "customer" AndAlso colName <> "contact" AndAlso colName <> "points" Then Return
        Dim row As DataRowView = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If row Is Nothing Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.SelectionBackground)
        Using pen As New Pen(Color.FromArgb(238, 238, 238))
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
        End Using

        Dim c As Rectangle = e.CellBounds
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
        Select Case colName
            Case "customer"
                TextRenderer.DrawText(e.Graphics, row("name").ToString(), NameFont, New Rectangle(c.X + 12, c.Y + 11, c.Width - 20, 20), Theme.TextDark, flags)
                TextRenderer.DrawText(e.Graphics, "Card " & row("card_number").ToString(), Theme.FontSmall, New Rectangle(c.X + 12, c.Y + 33, c.Width - 20, 18), Theme.TextMuted, flags)
            Case "contact"
                TextRenderer.DrawText(e.Graphics, row("contact_number").ToString(), NameFont, New Rectangle(c.X + 8, c.Y + 11, c.Width - 14, 20), Theme.TextDark, flags)
                TextRenderer.DrawText(e.Graphics, row("address").ToString(), Theme.FontSmall, New Rectangle(c.X + 8, c.Y + 33, c.Width - 14, 18), Theme.TextMuted, flags)
            Case "points"
                TextRenderer.DrawText(e.Graphics, Loyalty.Fmt(Convert.ToDecimal(row("points_balance"))) & " pts", PointsFont,
                                      New Rectangle(c.X + Theme.S(8), c.Y, c.Width - Theme.S(12), c.Height), Theme.Maroon,
                                      TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
        End Select
        e.Handled = True
    End Sub

    Private Sub OnCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        If _grid.Columns(e.ColumnIndex).Name = "last" Then
            e.Value = If(e.Value Is Nothing OrElse e.Value Is DBNull.Value, "Never", Convert.ToDateTime(e.Value).ToString("MMM d, yyyy"))
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

        _model.CustomerName = row("name").ToString()
        _model.CardNumber = row("card_number").ToString()
        _cardView.Model = _model
        _cardView.Invalidate()

        _dName.Text = row("name").ToString()
        _vPoints.Text = Loyalty.Fmt(Convert.ToDecimal(row("points_balance"))) & " pts"
        _vContact.Text = row("contact_number").ToString()
        _vAddress.Text = row("address").ToString()
        _vSince.Text = Convert.ToDateTime(row("created_at")).ToString("MMM d, yyyy")

        If row("last_purchase_at") Is DBNull.Value Then
            _vLast.Text = "No purchase yet"
        Else
            Dim last As DateTime = Convert.ToDateTime(row("last_purchase_at"))
            _vLast.Text = last.ToString("MMM d, yyyy") & If(last < DateTime.Today.AddYears(-20), "   (over 20 years ago)", "")
        End If
        _vTotals.Text = Loyalty.Fmt(Convert.ToDecimal(row("earned_total"))) & " earned   /   " & Loyalty.Fmt(Convert.ToDecimal(row("used_total"))) & " used"

        Dim lines As New List(Of String)()
        Try
            For Each r As DataRow In CustomerRepository.GetLedger(_selectedId, 4).Rows
                Dim pts As Decimal = Convert.ToDecimal(r("points"))
                lines.Add(Convert.ToDateTime(r("created_at")).ToString("MMM d") & "   " & If(pts >= 0D, "+", "") & Loyalty.Fmt(pts) &
                          "   (balance " & Loyalty.Fmt(Convert.ToDecimal(r("balance_after"))) & ")")
            Next
        Catch
            ' leave empty
        End Try
        _vActivity.Text = If(lines.Count = 0, "No points yet.", String.Join(Environment.NewLine, lines))
    End Sub

    Private Sub OnDetailsPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _details.Height)
        End Using
    End Sub

    ' ------------------------------------------------------------------ actions
    Private Sub OnAdd(sender As Object, e As EventArgs)
        Dim newId As Integer = 0
        Using dlg As New CustomerEditForm()
            If dlg.ShowDialog(OwnerOf(Me)) <> DialogResult.OK Then Return
            newId = dlg.CreatedId
        End Using
        _selectedId = newId
        LoadCustomers()
        If newId > 0 Then
            Using card As New CardDialog(newId)
                card.ShowDialog(OwnerOf(Me))
            End Using
        End If
    End Sub

    Private Sub OnEdit(sender As Object, e As EventArgs)
        If _selectedId <= 0 Then Return
        Using dlg As New CustomerEditForm(_selectedId)
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then LoadCustomers()
        End Using
    End Sub

    Private Sub OnCard(sender As Object, e As EventArgs)
        If _selectedId <= 0 Then Return
        Using dlg As New CardDialog(_selectedId)
            dlg.ShowDialog(OwnerOf(Me))
        End Using
    End Sub

    Private Sub OnDelete(sender As Object, e As EventArgs)
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim name As String = row("name").ToString()
        Dim pts As Decimal = Convert.ToDecimal(row("points_balance"))

        Dim msg As String = "Delete the account of '" & name & "'?" & Environment.NewLine & Environment.NewLine &
                            "The card stops working and the points history is deleted. Past sales keep the customer's name."
        If pts > 0D Then msg &= Environment.NewLine & Environment.NewLine & "This customer still has " & Loyalty.Fmt(pts) & " points that will be lost."
        If Not Confirm(Me, "Customers", msg, MessageBoxIcon.Warning) Then Return

        Try
            If CustomerRepository.Delete(id) = SaveResult.Ok Then
                AuditLog.Write("DELETE", "Customers", "Deleted customer '" & name & "' (" & Loyalty.Fmt(pts) & " points)")
            End If
        Catch ex As Exception
            Notify(Me, "Customers", "Could not delete the customer: " & ex.Message, MessageBoxIcon.Error)
        End Try
        _selectedId = 0
        LoadCustomers()
    End Sub

End Class
