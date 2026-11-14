Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms

''' <summary>
''' The shared screen of every report: Daily / Weekly / Monthly tabs with date arrows, status figures on top,
''' charts beside the table, search, and Save PDF / Save Excel / Print. A report only supplies its views and
''' the ReportDoc for each (see ReportData). Data loads on a background thread so the screen never freezes.
''' </summary>
Public MustInherit Class ReportFormBase
    Inherits Form
    Implements ILiveForm

    ' ---- what a report supplies
    Protected MustOverride ReadOnly Property ReportTitle As String
    Protected MustOverride ReadOnly Property ReportBlurb As String
    Protected MustOverride ReadOnly Property ViewNames As String()
    Protected MustOverride Function BuildDoc(view As Integer, range As DateRange, period As ReportPeriod) As ReportDoc
    Public MustOverride ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables

    ''' <summary>Text of the button for the selected row (e.g. "View receipt"). Empty = no button.</summary>
    Protected Overridable Function RowActionText(view As Integer) As String
        Return ""
    End Function

    Protected Overridable Sub OnRowAction(view As Integer, row As DataRowView)
    End Sub

    ''' <summary>Text of a second button for the selected row (e.g. "Print copy"). Empty = none.</summary>
    Protected Overridable Function RowAction2Text(view As Integer) As String
        Return ""
    End Function

    Protected Overridable Sub OnRowAction2(view As Integer, row As DataRowView)
    End Sub

    Protected Overridable Function SearchEnabled(view As Integer) As Boolean
        Return False
    End Function

    ' ---- a card with a thin rounded border (holds one chart)
    Private Class CardPanel
        Inherits Panel

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            BackColor = Color.White
            Padding = New Padding(Theme.S(12))
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.Clear(Color.White)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using path As GraphicsPath = Theme.RoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), 14)
                Using pen As New Pen(Theme.BorderColor, 1.0F)
                    e.Graphics.DrawPath(pen, path)
                End Using
            End Using
        End Sub
    End Class

    ' ---- header
    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _btnPdf As New MaroonButton()
    Private ReadOnly _btnExcel As New MaroonButton()
    Private ReadOnly _btnPrint As New MaroonButton()

    ' ---- period bar
    Private ReadOnly _periodBar As New Panel()
    Private ReadOnly _periods As New PillTabs()
    Private ReadOnly _dateChip As New DateChip()
    Private ReadOnly _btnToday As New MaroonButton()

    ' ---- figures
    Private ReadOnly _stats As New FlowLayoutPanel()
    Private ReadOnly _cards As New List(Of StatCard)()

    ' ---- view bar
    Private ReadOnly _viewBar As New Panel()
    Private ReadOnly _views As New PillTabs()
    Private ReadOnly _btnAction As New MaroonButton()
    Private ReadOnly _btnAction2 As New MaroonButton()
    Private ReadOnly _search As New SearchBox("Search this list...")

    ' ---- body
    Private ReadOnly _body As New Panel()
    Private ReadOnly _tableHost As New Panel()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _empty As New Label()
    Private ReadOnly _loading As New Label()
    Private ReadOnly _foot As New Label()
    Private ReadOnly _chartHost As New Panel()
    Private ReadOnly _card1 As New CardPanel()
    Private ReadOnly _card2 As New CardPanel()
    Private ReadOnly _gap As New Panel()
    Private ReadOnly _chart1 As New BarChart()
    Private ReadOnly _chart2 As New BarChart()

    ' ---- state
    Private _period As ReportPeriod = ReportPeriod.Daily
    Private _anchor As DateTime = DateTime.Today
    Private _view As Integer
    Private _doc As ReportDoc
    Private _version As Integer
    Private _loaded As Boolean

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildHeader()
        BuildPeriodBar()
        BuildStats()
        BuildViewBar()
        BuildBody()

        Controls.Add(_body)
        Controls.Add(_viewBar)
        Controls.Add(_stats)
        Controls.Add(_periodBar)
        Controls.Add(_header)
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub StyleExport(b As MaroonButton, text As String, width As Integer)
        b.Text = text
        b.MakeSecondary()
        b.Height = Theme.S(40)
        b.Width = Theme.S(width)
        _header.Controls.Add(b)
    End Sub

    Private Sub BuildHeader()
        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White

        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = False
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))
        _subtitle.Size = New Size(Theme.S(700), Theme.S(28))
        _subtitle.AutoEllipsis = True

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)

        StyleExport(_btnPdf, "Save PDF", 104)
        StyleExport(_btnExcel, "Save Excel", 112)
        StyleExport(_btnPrint, "Print", 84)
        AddHandler _btnPdf.Click, AddressOf OnSavePdf
        AddHandler _btnExcel.Click, AddressOf OnSaveExcel
        AddHandler _btnPrint.Click, AddressOf OnPrint
        AddHandler _header.Resize, AddressOf OnHeaderResize
        OnHeaderResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnHeaderResize(sender As Object, e As EventArgs)
        Dim x As Integer = _header.ClientSize.Width - Theme.S(32)
        For Each b As MaroonButton In New MaroonButton() {_btnPrint, _btnExcel, _btnPdf}
            x -= b.Width
            b.Location = New Point(x, Theme.S(20))
            x -= Theme.S(10)
        Next
        _subtitle.Width = Math.Max(Theme.S(200), x - _subtitle.Left)
    End Sub

    Private Sub BuildPeriodBar()
        _periodBar.Dock = DockStyle.Top
        _periodBar.Height = Theme.S(58)
        _periodBar.BackColor = Color.White

        _periods.Location = New Point(Theme.S(32), Theme.S(9))
        _periods.SetItems("Daily", "Weekly", "Monthly")

        _dateChip.Location = New Point(Theme.S(300), Theme.S(9))
        _dateChip.SetValue(_anchor, ReportPeriods.ShortLabel(_period, _anchor))

        _btnToday.MakeSecondary()
        _btnToday.Text = "Today"
        _btnToday.Size = New Size(Theme.S(80), Theme.S(40))

        _periodBar.Controls.Add(_periods)
        _periodBar.Controls.Add(_dateChip)
        _periodBar.Controls.Add(_btnToday)
        AddHandler _periodBar.Resize, AddressOf OnPeriodBarResize
        OnPeriodBarResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnPeriodBarResize(sender As Object, e As EventArgs)
        _dateChip.Location = New Point(_periods.Right + Theme.S(20), Theme.S(9))
        _btnToday.Location = New Point(_dateChip.Right + Theme.S(10), Theme.S(9))
    End Sub

    Private Sub BuildStats()
        _stats.Dock = DockStyle.Top
        _stats.Height = Theme.S(84)
        _stats.BackColor = Color.White
        _stats.FlowDirection = FlowDirection.LeftToRight
        _stats.WrapContents = False
        _stats.Padding = New Padding(Theme.S(32), Theme.S(4), 0, 0)
        For i As Integer = 0 To 5
            Dim c As New StatCard()
            c.Margin = New Padding(0, 0, Theme.S(14), 0)
            c.Visible = False
            _cards.Add(c)
            _stats.Controls.Add(c)
        Next
        AddHandler _stats.Resize, AddressOf OnStatsResize
    End Sub

    Private Sub OnStatsResize(sender As Object, e As EventArgs)
        Dim visibleCount As Integer = 0
        For Each c As StatCard In _cards
            If c.Visible Then visibleCount += 1
        Next
        If visibleCount = 0 Then Return
        Dim avail As Integer = _stats.ClientSize.Width - Theme.S(32) - Theme.S(32)
        Dim w As Integer = (avail - Theme.S(14) * (visibleCount - 1)) \ visibleCount
        w = Math.Max(Theme.S(150), Math.Min(Theme.S(260), w))
        For Each c As StatCard In _cards
            c.Width = w
        Next
    End Sub

    Private Sub BuildViewBar()
        _viewBar.Dock = DockStyle.Top
        _viewBar.Height = Theme.S(56)
        _viewBar.BackColor = Color.White

        _views.Location = New Point(Theme.S(32), Theme.S(8))

        _btnAction.MakeSecondary()
        _btnAction.Size = New Size(Theme.S(150), Theme.S(40))
        _btnAction.Visible = False
        _btnAction.Enabled = False
        AddHandler _btnAction.Click, AddressOf OnActionClick

        _btnAction2.Size = New Size(Theme.S(140), Theme.S(40))
        _btnAction2.Visible = False
        _btnAction2.Enabled = False
        AddHandler _btnAction2.Click, AddressOf OnAction2Click

        _search.Size = New Size(Theme.S(280), Theme.S(40))
        _search.Visible = False
        AddHandler _search.SearchChanged, AddressOf OnSearchChanged

        _viewBar.Controls.Add(_views)
        _viewBar.Controls.Add(_btnAction)
        _viewBar.Controls.Add(_btnAction2)
        _viewBar.Controls.Add(_search)
        AddHandler _viewBar.Resize, AddressOf OnViewBarResize
        OnViewBarResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnViewBarResize(sender As Object, e As EventArgs)
        Dim x As Integer = _viewBar.ClientSize.Width - Theme.S(32)
        If _search.Visible Then
            x -= _search.Width
            _search.Location = New Point(x, Theme.S(8))
            x -= Theme.S(12)
        End If
        For Each b As MaroonButton In New MaroonButton() {_btnAction2, _btnAction}
            If b.Visible Then
                x -= b.Width
                b.Location = New Point(x, Theme.S(8))
                x -= Theme.S(10)
            End If
        Next
    End Sub

    Private Sub BuildBody()
        _body.Dock = DockStyle.Fill
        _body.BackColor = Color.White
        _body.Padding = New Padding(Theme.S(32), 0, Theme.S(32), Theme.S(16))

        ' charts (right)
        _chartHost.Dock = DockStyle.Right
        _chartHost.BackColor = Color.White
        _chartHost.Padding = New Padding(Theme.S(16), 0, 0, 0)
        _chart1.Dock = DockStyle.Fill
        _chart2.Dock = DockStyle.Fill
        _card1.Controls.Add(_chart1)
        _card2.Controls.Add(_chart2)
        _gap.Dock = DockStyle.Top
        _gap.Height = Theme.S(12)
        _gap.BackColor = Color.White
        _card2.Dock = DockStyle.Fill
        _card1.Dock = DockStyle.Top
        _chartHost.Controls.Add(_card2)
        _chartHost.Controls.Add(_gap)
        _chartHost.Controls.Add(_card1)
        AddHandler _chartHost.Resize, AddressOf OnChartHostResize
        AddHandler _body.Resize, AddressOf OnBodyResize

        ' table (left)
        _tableHost.Dock = DockStyle.Fill
        _tableHost.BackColor = Color.White

        _grid.Dock = DockStyle.Fill
        GridStyle.Apply(_grid, 46)
        AddHandler _grid.SelectionChanged, AddressOf OnGridSelectionChanged
        AddHandler _grid.CellDoubleClick, AddressOf OnGridDoubleClick

        _foot.Dock = DockStyle.Bottom
        _foot.Height = Theme.S(28)
        _foot.Font = Theme.FontSmall
        _foot.ForeColor = Theme.TextMuted
        _foot.TextAlign = ContentAlignment.MiddleLeft

        _empty.Dock = DockStyle.Fill
        _empty.TextAlign = ContentAlignment.MiddleCenter
        _empty.Font = Theme.FontHeading
        _empty.ForeColor = Theme.TextMuted
        _empty.BackColor = Color.White
        _empty.Visible = False

        _loading.Dock = DockStyle.Fill
        _loading.TextAlign = ContentAlignment.MiddleCenter
        _loading.Font = Theme.FontHeading
        _loading.ForeColor = Theme.TextMuted
        _loading.BackColor = Color.White
        _loading.Text = "Loading..."
        _loading.Visible = False

        _tableHost.Controls.Add(_grid)
        _tableHost.Controls.Add(_foot)
        _tableHost.Controls.Add(_empty)
        _tableHost.Controls.Add(_loading)

        _body.Controls.Add(_tableHost)
        _body.Controls.Add(_chartHost)
        _chartHost.Visible = False
    End Sub

    Private Sub OnBodyResize(sender As Object, e As EventArgs)
        _chartHost.Width = Math.Max(Theme.S(340), CInt(_body.ClientSize.Width * 0.4F))
    End Sub

    Private Sub OnChartHostResize(sender As Object, e As EventArgs)
        LayoutCharts()
    End Sub

    ''' <summary>One chart fills the card area; two charts share it (top and bottom).</summary>
    Private Sub LayoutCharts()
        If _card2.Visible Then
            _card1.Dock = DockStyle.Top
            _card1.Height = Math.Max(Theme.S(120), (_chartHost.ClientSize.Height - _gap.Height) \ 2)
        Else
            _card1.Dock = DockStyle.Fill
        End If
    End Sub

    ' ------------------------------------------------------------------ start
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        _title.Text = ReportTitle
        _subtitle.Text = ReportBlurb
        _views.SetItems(ViewNames)
        _views.Visible = ViewNames.Length > 1
        OnBodyResize(Me, EventArgs.Empty)
        UpdateViewControls()

        AddHandler _periods.SelectedIndexChanged, AddressOf OnPeriodChanged
        AddHandler _views.SelectedIndexChanged, AddressOf OnViewChanged
        AddHandler _btnToday.Click, AddressOf OnTodayClick
        AddHandler _dateChip.DateChosen, AddressOf OnDateChosen

        _loaded = True
        LoadAsync(True)
    End Sub

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        If _loaded Then LoadAsync(False)
    End Sub

    ' ------------------------------------------------------------------ navigation
    Private Sub OnPeriodChanged(sender As Object, e As EventArgs)
        _period = CType(_periods.SelectedIndex, ReportPeriod)
        UpdateChip()
        LoadAsync(True)
    End Sub

    Private Sub UpdateChip()
        _dateChip.SetValue(_anchor, ReportPeriods.ShortLabel(_period, _anchor))
        OnPeriodBarResize(Me, EventArgs.Empty)
    End Sub

    Private Sub OnViewChanged(sender As Object, e As EventArgs)
        _view = _views.SelectedIndex
        _search.Visible = SearchEnabled(_view)
        UpdateViewControls()
        LoadAsync(True)
    End Sub

    Private Sub UpdateViewControls()
        Dim actionText As String = RowActionText(_view)
        _btnAction.Text = actionText
        _btnAction.Visible = actionText.Length > 0
        Dim action2Text As String = RowAction2Text(_view)
        _btnAction2.Text = action2Text
        _btnAction2.Visible = action2Text.Length > 0
        _search.Visible = SearchEnabled(_view)
        OnViewBarResize(Me, EventArgs.Empty)
        _viewBar.Visible = (ViewNames.Length > 1) OrElse _btnAction.Visible OrElse _btnAction2.Visible OrElse _search.Visible
    End Sub

    Private Sub OnDateChosen(value As DateTime)
        _anchor = value.Date
        UpdateChip()
        LoadAsync(True)
    End Sub

    Private Sub OnTodayClick(sender As Object, e As EventArgs)
        _anchor = DateTime.Today
        UpdateChip()
        LoadAsync(True)
    End Sub

    ' ------------------------------------------------------------------ loading
    Private Sub LoadAsync(showOverlay As Boolean)
        If Not IsHandleCreated Then Return
        _version += 1
        Dim ver As Integer = _version
        Dim v As Integer = _view
        Dim p As ReportPeriod = _period
        Dim r As DateRange = ReportPeriods.RangeFor(p, _anchor)

        If showOverlay Then
            _loading.Visible = True
            _loading.BringToFront()
        End If

        Task.Run(Function() BuildDoc(v, r, p)).ContinueWith(
            Sub(t As Task(Of ReportDoc))
                Try
                    If IsDisposed OrElse Not IsHandleCreated Then Return
                    BeginInvoke(New Action(Sub() OnLoaded(ver, t)))
                Catch ex As InvalidOperationException
                    ' the screen was closed while loading
                End Try
            End Sub)
    End Sub

    Private Sub OnLoaded(ver As Integer, t As Task(Of ReportDoc))
        If ver <> _version Then Return                 ' an older request: a newer one is on its way
        _loading.Visible = False
        If t.IsFaulted OrElse t.IsCanceled Then
            Dim msg As String = If(t.Exception IsNot Nothing, t.Exception.GetBaseException().Message, "cancelled")
            _empty.Text = "The report could not be loaded." & vbCrLf & msg
            _empty.Visible = True
            _grid.Visible = False
            Return
        End If
        ShowDoc(t.Result)
    End Sub

    Private Sub ShowDoc(doc As ReportDoc)
        _doc = doc

        _subtitle.Text = doc.PeriodLabel & If(doc.Note.Length > 0, "   |   " & doc.Note, "")

        ' status figures
        For i As Integer = 0 To _cards.Count - 1
            If i < doc.Kpis.Count Then
                _cards(i).Value = doc.Kpis(i).Value
                _cards(i).Caption = doc.Kpis(i).Caption
                _cards(i).Warn = doc.Kpis(i).Warn
                _cards(i).Visible = True
            Else
                _cards(i).Visible = False
            End If
        Next
        OnStatsResize(Me, EventArgs.Empty)

        ' charts
        Dim shown As New List(Of ReportChart)()
        For Each c As ReportChart In doc.Charts
            shown.Add(c)
        Next
        _chartHost.Visible = shown.Count > 0
        _card1.Visible = shown.Count > 0
        _card2.Visible = shown.Count > 1
        _gap.Visible = shown.Count > 1
        If shown.Count > 0 Then _chart1.SetChart(shown(0))
        If shown.Count > 1 Then _chart2.SetChart(shown(1))
        LayoutCharts()

        ' table
        _grid.DataSource = Nothing
        _grid.Columns.Clear()
        For Each col As ReportColumn In doc.Columns
            Dim gc As New DataGridViewTextBoxColumn()
            gc.Name = col.Field
            gc.HeaderText = col.Header
            gc.DataPropertyName = col.Field
            gc.FillWeight = col.Weight
            gc.MinimumWidth = Theme.S(col.MinWidth)
            gc.SortMode = DataGridViewColumnSortMode.NotSortable
            If col.RightAligned Then
                gc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                gc.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
            End If
            Dim fmt As String = ReportFormat.GridFormat(col.Kind)
            If fmt.Length > 0 Then gc.DefaultCellStyle.Format = fmt
            _grid.Columns.Add(gc)
        Next
        _grid.DataSource = doc.Data.DefaultView
        ApplyFilter()

        _btnPdf.Enabled = doc.Columns.Count > 0
        _btnExcel.Enabled = doc.Columns.Count > 0
        _btnPrint.Enabled = doc.Columns.Count > 0
        UpdateEmptyAndFooter()
        OnGridSelectionChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnSearchChanged()
        ApplyFilter()
        UpdateEmptyAndFooter()
    End Sub

    Private Sub ApplyFilter()
        If _doc Is Nothing OrElse _doc.Data Is Nothing Then Return
        Dim dv As DataView = _doc.Data.DefaultView
        Dim q As String = _search.Query
        Try
            If q.Length = 0 OrElse Not SearchEnabled(_view) Then
                dv.RowFilter = ""
            Else
                Dim esc As String = q.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]")
                Dim parts As New List(Of String)()
                For Each c As ReportColumn In _doc.Columns
                    parts.Add("Convert([" & c.Field & "], 'System.String') LIKE '%" & esc & "%'")
                Next
                dv.RowFilter = String.Join(" OR ", parts)
            End If
        Catch ex As Exception
            dv.RowFilter = ""
        End Try
    End Sub

    Private Sub UpdateEmptyAndFooter()
        If _doc Is Nothing Then Return
        Dim shown As Integer = _doc.Data.DefaultView.Count
        Dim total As Integer = _doc.Data.Rows.Count
        Dim hasTable As Boolean = _doc.Columns.Count > 0

        _empty.Text = If(total = 0, _doc.EmptyText, "Nothing matches your search.")
        _empty.Visible = hasTable AndAlso shown = 0
        _grid.Visible = hasTable AndAlso shown > 0

        Dim f As String = ""
        If hasTable AndAlso total > 0 Then
            f = If(shown = total, total.ToString("N0") & If(total = 1, " row", " rows"), shown.ToString("N0") & " of " & total.ToString("N0") & " rows")
        End If
        If _doc.TruncatedNote.Length > 0 Then f &= "   |   " & _doc.TruncatedNote
        _foot.Text = f
    End Sub

    ' ------------------------------------------------------------------ row action (receipt copy, delivery items...)
    Private Function SelectedRow() As DataRowView
        If _grid.CurrentRow Is Nothing Then Return Nothing
        Return TryCast(_grid.CurrentRow.DataBoundItem, DataRowView)
    End Function

    Private Sub OnGridSelectionChanged(sender As Object, e As EventArgs)
        Dim has As Boolean = SelectedRow() IsNot Nothing
        _btnAction.Enabled = _btnAction.Visible AndAlso has
        _btnAction2.Enabled = _btnAction2.Visible AndAlso has
    End Sub

    Private Sub OnActionClick(sender As Object, e As EventArgs)
        RunRowAction()
    End Sub

    Private Sub OnAction2Click(sender As Object, e As EventArgs)
        If RowAction2Text(_view).Length = 0 Then Return
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        OnRowAction2(_view, row)
    End Sub

    Private Sub OnGridDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then RunRowAction()
    End Sub

    Private Sub RunRowAction()
        If RowActionText(_view).Length = 0 Then Return
        Dim row As DataRowView = SelectedRow()
        If row Is Nothing Then Return
        OnRowAction(_view, row)
    End Sub

    ' ------------------------------------------------------------------ export
    Private Function HasDoc() As Boolean
        Return _doc IsNot Nothing AndAlso _doc.Columns.Count > 0
    End Function

    Private Function AskPath(filter As String, extension As String) As String
        Using dlg As New SaveFileDialog()
            dlg.Filter = filter
            dlg.DefaultExt = extension
            dlg.AddExtension = True
            dlg.OverwritePrompt = True
            dlg.FileName = _doc.FileBaseName() & "." & extension
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            If dlg.ShowDialog(OwnerOf(Me)) <> DialogResult.OK Then Return Nothing
            Return dlg.FileName
        End Using
    End Function

    Private Sub AfterSave(path As String, kind As String)
        Try
            AuditLog.Write("EXPORT", ReportTitle, "Saved " & kind & ": " & System.IO.Path.GetFileName(path))
        Catch
            ' the audit trail must never stop an export
        End Try
        If MessageBox.Show(OwnerOf(Me), "Saved:" & vbCrLf & path & vbCrLf & vbCrLf & "Open it now?", ReportTitle,
                           MessageBoxButtons.YesNo, MessageBoxIcon.Information) = DialogResult.Yes Then
            Try
                System.Diagnostics.Process.Start(New System.Diagnostics.ProcessStartInfo(path) With {.UseShellExecute = True})
            Catch ex As Exception
                MessageBox.Show(OwnerOf(Me), "Could not open the file: " & ex.Message, ReportTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End If
    End Sub

    Private Sub OnSavePdf(sender As Object, e As EventArgs)
        If Not HasDoc() Then Return
        Dim path As String = AskPath("PDF file (*.pdf)|*.pdf", "pdf")
        If path Is Nothing Then Return
        Try
            ReportExport.SavePdf(_doc, path)
            AfterSave(path, "PDF")
        Catch ex As Exception
            MessageBox.Show(OwnerOf(Me), "Could not save the PDF." & vbCrLf & ex.Message, ReportTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub OnSaveExcel(sender As Object, e As EventArgs)
        If Not HasDoc() Then Return
        Dim path As String = AskPath("Excel file (*.xlsx)|*.xlsx", "xlsx")
        If path Is Nothing Then Return
        Try
            ReportExport.SaveXlsx(_doc, path)
            AfterSave(path, "Excel")
        Catch ex As Exception
            MessageBox.Show(OwnerOf(Me), "Could not save the Excel file." & vbCrLf & ex.Message, ReportTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub OnPrint(sender As Object, e As EventArgs)
        If Not HasDoc() Then Return
        Try
            Try
                AuditLog.Write("PRINT", ReportTitle, "Opened print preview: " & _doc.Title & " (" & _doc.PeriodLabel & ")")
            Catch
            End Try
            ReportExport.PrintPreview(_doc, OwnerOf(Me))
        Catch ex As Exception
            MessageBox.Show(OwnerOf(Me), "Could not print the report." & vbCrLf & ex.Message, ReportTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

End Class
