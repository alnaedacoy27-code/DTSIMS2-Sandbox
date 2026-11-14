Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Reports > Delivery Report. What was received from suppliers in a day, week or month: totals, cost by supplier,
''' and every delivery (double-click to see its items and how much of it is still in stock).
''' </summary>
Public Class DeliveryReportForm
    Inherits ReportFormBase

    Protected Overrides ReadOnly Property ReportTitle As String
        Get
            Return "Delivery Report"
        End Get
    End Property

    Protected Overrides ReadOnly Property ReportBlurb As String
        Get
            Return "Supplier deliveries received into inventory"
        End Get
    End Property

    Protected Overrides ReadOnly Property ViewNames As String()
        Get
            Return New String() {"Summary", "Deliveries"}
        End Get
    End Property

    Public Overrides ReadOnly Property WatchedTables As String()
        Get
            Return New String() {"deliveries", "delivery_items", "suppliers"}
        End Get
    End Property

    Protected Overrides Function BuildDoc(view As Integer, range As DateRange, period As ReportPeriod) As ReportDoc
        If view = 1 Then Return ReportData.DeliveryList(period, range)
        Return ReportData.DeliverySummary(period, range)
    End Function

    Protected Overrides Function RowActionText(view As Integer) As String
        Return If(view = 1, "View items", "")
    End Function

    Protected Overrides Function SearchEnabled(view As Integer) As Boolean
        Return view = 1
    End Function

    Protected Overrides Sub OnRowAction(view As Integer, row As DataRowView)
        Dim id As Integer = Convert.ToInt32(row("id"))
        Dim batch As String = Convert.ToString(row("batch_no"))
        Dim supplier As String = Convert.ToString(row("supplier"))
        Dim items As DataTable
        Try
            items = ReportData.DeliveryItems(id)
        Catch ex As Exception
            MessageBox.Show(OwnerOf(Me), "The items could not be loaded." & vbCrLf & ex.Message, "Delivery Report", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try
        Using dlg As New DeliveryItemsDialog(batch, supplier, items)
            dlg.ShowDialog(OwnerOf(Me))
        End Using
    End Sub

End Class

''' <summary>The items of one delivery.</summary>
Public Class DeliveryItemsDialog
    Inherits Form

    Private ReadOnly _grid As New DataGridView()

    Public Sub New(batch As String, supplier As String, items As DataTable)
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        Text = "Delivery " & batch
        BackColor = Color.White
        Font = Theme.FontBase
        ClientSize = New Size(Theme.S(900), Theme.S(560))

        Dim top As New Panel()
        top.Dock = DockStyle.Top
        top.Height = Theme.S(84)
        Dim title As New Label()
        title.AutoSize = True
        title.Font = Theme.FontHeading
        title.ForeColor = Theme.Maroon
        title.Text = "Delivery " & batch
        title.Location = New Point(Theme.S(24), Theme.S(14))
        Dim sub1 As New Label()
        sub1.AutoSize = True
        sub1.Font = Theme.FontBase
        sub1.ForeColor = Theme.TextMuted
        sub1.Text = supplier & "   |   " & items.Rows.Count.ToString() & If(items.Rows.Count = 1, " item", " items")
        sub1.Location = New Point(Theme.S(24), Theme.S(48))
        top.Controls.Add(title)
        top.Controls.Add(sub1)

        Dim bottom As New Panel()
        bottom.Dock = DockStyle.Bottom
        bottom.Height = Theme.S(70)
        Dim close As New MaroonButton()
        close.Text = "Close"
        close.MakeSecondary()
        close.Size = New Size(Theme.S(120), Theme.S(44))
        close.Location = New Point(Theme.S(756), Theme.S(14))
        AddHandler close.Click, Sub(s As Object, e As EventArgs) Me.Close()
        bottom.Controls.Add(close)

        Dim host As New Panel()
        host.Dock = DockStyle.Fill
        host.Padding = New Padding(Theme.S(24), 0, Theme.S(24), 0)
        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 44)
        AddColumn("product", "PRODUCT", "product", 26, False, "")
        AddColumn("size", "SIZE", "size", 11, False, "")
        AddColumn("qty", "RECEIVED", "qty", 9, True, "#,##0")
        AddColumn("remaining", "STILL LEFT", "remaining", 9, True, "#,##0")
        AddColumn("cost", "COST EACH", "cost_price", 10, True, "'" & ReportFormat.PesoSign & "'#,##0.00")
        AddColumn("price", "SELLING PRICE", "selling_price", 11, True, "'" & ReportFormat.PesoSign & "'#,##0.00")
        AddColumn("line", "LINE COST", "line_cost", 11, True, "'" & ReportFormat.PesoSign & "'#,##0.00")
        AddColumn("profit", "EXPECTED PROFIT", "expected_profit", 12, True, "'" & ReportFormat.PesoSign & "'#,##0.00")
        _grid.DataSource = items.DefaultView
        host.Controls.Add(_grid)

        Controls.Add(host)
        Controls.Add(bottom)
        Controls.Add(top)
    End Sub

    Private Sub AddColumn(name As String, header As String, prop As String, weight As Single, right As Boolean, format As String)
        Dim c As New DataGridViewTextBoxColumn()
        c.Name = name
        c.HeaderText = header
        c.DataPropertyName = prop
        c.FillWeight = weight
        c.MinimumWidth = Theme.S(80)
        c.SortMode = DataGridViewColumnSortMode.NotSortable
        If right Then
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
        End If
        If format.Length > 0 Then c.DefaultCellStyle.Format = format
        _grid.Columns.Add(c)
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        WindowStyle.ApplyMaroonTitleBar(Me)
    End Sub

End Class
