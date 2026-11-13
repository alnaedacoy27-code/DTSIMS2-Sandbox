Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>
''' Point of sale. Full screen: product tiles and search on the left, the cart on the right.
''' Proceed slides in a payment pad, then the receipt. Special prices need an Admin's approval.
''' </summary>
Public Class PosForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly CartWidth As Integer = Theme.S(480)

    Private Shared ReadOnly BigMoney As Font = Theme.MakeFont("Segoe UI Semibold", 30.0F)
    Private Shared ReadOnly MidBold As Font = Theme.MakeFont("Segoe UI Semibold", 13.0F)
    Private Shared ReadOnly Title18 As Font = Theme.MakeFont("Segoe UI Semibold", 18.0F)

    ''' <summary>The cashier pressed "Exit POS": the main window shows the home screen again.</summary>
    Public Event ExitRequested()

    ' ---- top bar
    Private ReadOnly _top As New Panel()
    Private ReadOnly _lblBrand As New Label()
    Private ReadOnly _lblWho As New Label()
    Private ReadOnly _btnExit As New MaroonButton()

    ' ---- left
    Private ReadOnly _body As New Panel()
    Private ReadOnly _left As New Panel()
    Private ReadOnly _toolbar As New Panel()
    Private ReadOnly _search As New InputField("Search or scan a barcode", InputKind.Text, 420, False)
    Private ReadOnly _cat As New ComboBox()
    Private ReadOnly _tiles As New FlowLayoutPanel()
    Private ReadOnly _tilesNote As New Label()
    Private ReadOnly _footer As New Panel()
    Private ReadOnly _cardCount As New StatCard()
    Private ReadOnly _cardAmount As New StatCard()

    ' ---- right
    Private ReadOnly _right As New Panel()
    Private ReadOnly _cartPanel As New Panel()
    Private ReadOnly _cartHead As New Panel()
    Private ReadOnly _lblCartTitle As New Label()
    Private ReadOnly _lblCartMsg As New Label()
    Private ReadOnly _lnkClear As New LinkLabel()
    Private ReadOnly _cartList As New FlowLayoutPanel()
    Private ReadOnly _cartEmpty As New Label()
    Private ReadOnly _summary As New Panel()
    Private ReadOnly _valSub As New Label()
    Private ReadOnly _cmbDiscount As New ComboBox()
    Private ReadOnly _valDisc As New Label()
    Private ReadOnly _btnSpecial As New MaroonButton()
    Private ReadOnly _valSpecial As New Label()
    Private ReadOnly _lnkSpecialX As New LinkLabel()
    Private ReadOnly _lblVat As New Label()
    Private ReadOnly _valVat As New Label()
    Private ReadOnly _valTotal As New Label()
    Private ReadOnly _btnProceed As New MaroonButton()

    Private ReadOnly _payPanel As New Panel()
    Private ReadOnly _lblPayCap As New Label()
    Private ReadOnly _lblPayLoy As New Label()
    Private ReadOnly _lnkLoy As New LinkLabel()
    Private ReadOnly _lblPayTotal As New Label()
    Private ReadOnly _lblReceived As New Label()
    Private ReadOnly _lblChange As New Label()
    Private ReadOnly _padPay As New NumpadPad()
    Private ReadOnly _quick As New List(Of MaroonButton)()
    Private ReadOnly _lblPayMsg As New Label()
    Private ReadOnly _btnPayBack As New MaroonButton()
    Private ReadOnly _btnPayDone As New MaroonButton()

    Private ReadOnly _receiptPanel As New Panel()
    Private ReadOnly _lblRecChange As New Label()
    Private ReadOnly _paper As New ReceiptPaper()
    Private ReadOnly _lblRecMsg As New Label()
    Private ReadOnly _btnPrint As New MaroonButton()
    Private ReadOnly _btnNew As New MaroonButton()

    ' ---- state
    Private ReadOnly _items As New List(Of CartItem)()
    Private ReadOnly _catIds As New List(Of Integer)()
    Private ReadOnly _discIds As New List(Of Integer)()
    Private ReadOnly _discPct As New List(Of Decimal)()
    Private ReadOnly _received As New AmountBuffer()
    Private ReadOnly _slide As New SlideAnimator()
    Private ReadOnly _searchTimer As New Timer()
    Private ReadOnly _liveTimer As New Timer()
    Private ReadOnly _msgTimer As New Timer()
    Private ReadOnly _tick As New Timer()
    Private ReadOnly _lblClock As New Label()

    Private _vatPct As Decimal
    Private _peso As Decimal = 1D
    Private _spend As Decimal = 200D
    Private _loy As LoyaltyChoice
    Private _loyAsked As Boolean
    Private _specialAmount As Decimal
    Private _approverId As Integer
    Private _approverName As String = ""
    Private _totals As SaleTotals = PosMath.Compute(0D, 0D, 0D, 0D, 0D)
    Private _lastReceipt As ReceiptModel
    Private _payOpen As Boolean
    Private _receiptOpen As Boolean
    Private _loading As Boolean

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi

        BackColor = Color.White
        Font = Theme.FontBase

        BuildTop()
        BuildLeft()
        BuildRight()

        _body.Dock = DockStyle.Fill
        _body.Controls.Add(_left)
        _body.Controls.Add(_right)
        Controls.Add(_body)
        Controls.Add(_top)

        _searchTimer.Interval = 250
        AddHandler _searchTimer.Tick, AddressOf OnSearchTimer
        _liveTimer.Interval = 700
        AddHandler _liveTimer.Tick, AddressOf OnLiveTimer
        _msgTimer.Interval = 3500
        AddHandler _msgTimer.Tick, AddressOf OnMsgTimer
        _tick.Interval = 1000
        AddHandler _tick.Tick, AddressOf OnClockTick
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"products", "discounts", "sales", "vat_rates", "categories"}
        End Get
    End Property

    ''' <summary>Stock, prices, discounts or sales changed somewhere: refresh (a moment later, once).</summary>
    Public Sub RefreshData() Implements ILiveForm.RefreshData
        _liveTimer.Stop()
        _liveTimer.Start()
    End Sub

    ' ------------------------------------------------------------------ building
    Private Sub BuildTop()
        _top.Dock = DockStyle.Top
        _top.Height = Theme.S(66)
        _top.BackColor = Theme.Maroon

        _lblBrand.AutoSize = True
        _lblBrand.Font = Title18
        _lblBrand.ForeColor = Color.White
        _lblBrand.Text = "DARWIN TRADING  |  POS"
        _lblBrand.Location = New Point(Theme.S(24), Theme.S(16))

        _lblWho.AutoSize = True
        _lblWho.Font = Theme.FontBase
        _lblWho.ForeColor = Color.FromArgb(240, 214, 214)

        _lblClock.AutoSize = True
        _lblClock.Font = Theme.FontBase
        _lblClock.ForeColor = Color.FromArgb(240, 214, 214)

        _btnExit.Text = "Exit POS"
        _btnExit.Size = New Size(Theme.S(130), Theme.S(44))
        _btnExit.BackColor = Color.White
        _btnExit.ForeColor = Theme.Maroon
        _btnExit.FlatAppearance.MouseOverBackColor = Theme.MaroonTint
        AddHandler _btnExit.Click, AddressOf OnExit

        _top.Controls.Add(_lblBrand)
        _top.Controls.Add(_lblWho)
        _top.Controls.Add(_lblClock)
        _top.Controls.Add(_btnExit)
        AddHandler _top.Resize, AddressOf OnTopResize
    End Sub

    Private Sub OnTilesSized(sender As Object, e As EventArgs)
        FitTiles()
    End Sub

    ''' <summary>Stretch the product tiles so each row uses the width (tiles stay between 236 and 300 wide).</summary>
    Private Sub FitTiles()
        If _tiles.Controls.Count = 0 Then Return
        Dim minW As Integer = Theme.S(236)
        Dim gap As Integer = Theme.S(12)
        Dim avail As Integer = _tiles.ClientSize.Width - _tiles.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth
        If avail < minW Then Return
        Dim cols As Integer = Math.Max(1, (avail + gap) \ (minW + gap))
        Dim w As Integer = Math.Max(minW, Math.Min(Theme.S(300), (avail + gap) \ cols - gap))
        _tiles.SuspendLayout()
        For Each c As Control In _tiles.Controls
            If TypeOf c Is ProductTile Then c.Width = w
        Next
        _tiles.ResumeLayout()
    End Sub

    Private Sub OnTopResize(sender As Object, e As EventArgs)
        _btnExit.Location = New Point(_top.ClientSize.Width - Theme.S(24) - _btnExit.Width, Theme.S(11))
        _lblClock.Location = New Point(_btnExit.Left - Theme.S(24) - _lblClock.Width, Theme.S(24))
        _lblWho.Location = New Point(Math.Max(_lblBrand.Right + Theme.S(20), _lblClock.Left - Theme.S(30) - _lblWho.Width), Theme.S(24))
    End Sub

    Private Sub BuildLeft()
        _left.Dock = DockStyle.Fill
        _left.BackColor = Color.FromArgb(251, 249, 249)

        _toolbar.Dock = DockStyle.Top
        _toolbar.Height = Theme.S(92)
        _toolbar.BackColor = Color.White
        _toolbar.Padding = New Padding(Theme.S(20), Theme.S(10), Theme.S(20), 0)
        AddHandler _toolbar.Paint, AddressOf OnToolbarPaint

        _search.Location = New Point(Theme.S(20), Theme.S(10))
        _search.SwallowEnter = True
        AddHandler _search.ValueChanged, AddressOf OnSearchTyped
        AddHandler _search.EnterPressed, AddressOf OnSearchEnter

        _cat.DropDownStyle = ComboBoxStyle.DropDownList
        _cat.FlatStyle = FlatStyle.Flat
        _cat.Font = Theme.MakeFont("Segoe UI", 12.0F)
        _cat.BackColor = Color.White
        _cat.Width = Theme.S(240)
        _cat.Location = New Point(Theme.S(20) + Theme.S(420) + Theme.S(16), Theme.S(34))
        AddHandler _cat.SelectedIndexChanged, AddressOf OnCategoryChanged

        _toolbar.Controls.Add(_search)
        _toolbar.Controls.Add(_cat)

        _tiles.Dock = DockStyle.Fill
        _tiles.AutoScroll = True
        _tiles.WrapContents = True
        _tiles.FlowDirection = FlowDirection.LeftToRight
        _tiles.Padding = New Padding(Theme.S(16), Theme.S(14), Theme.S(8), Theme.S(8))
        _tiles.BackColor = _left.BackColor
        AddHandler _tiles.SizeChanged, AddressOf OnTilesSized

        _tilesNote.Dock = DockStyle.Fill
        _tilesNote.TextAlign = ContentAlignment.MiddleCenter
        _tilesNote.Font = Theme.FontHeading
        _tilesNote.ForeColor = Theme.TextMuted
        _tilesNote.BackColor = _left.BackColor
        _tilesNote.Visible = False

        _footer.Dock = DockStyle.Bottom
        _footer.Height = Theme.S(84)
        _footer.BackColor = Color.White
        AddHandler _footer.Paint, AddressOf OnFooterPaint

        _cardCount.Size = New Size(190, 64)
        _cardCount.Location = New Point(20, 10)
        _cardCount.Caption = "My transactions (24 h)"
        _cardAmount.Size = New Size(Theme.S(260), Theme.S(64))
        _cardAmount.Location = New Point(Theme.S(20) + Theme.S(190) + Theme.S(14), Theme.S(10))
        _cardAmount.Caption = "My total sales (24 h)"
        _footer.Controls.Add(_cardCount)
        _footer.Controls.Add(_cardAmount)

        _left.Controls.Add(_tiles)
        _left.Controls.Add(_tilesNote)
        _left.Controls.Add(_footer)
        _left.Controls.Add(_toolbar)
    End Sub

    Private Sub OnToolbarPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, _toolbar.Height - 1, _toolbar.Width, _toolbar.Height - 1)
        End Using
    End Sub

    Private Sub OnFooterPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, _footer.Width, 0)
        End Using
    End Sub

    Private Function MakeLabel(font As Font, color As Color, width As Integer, align As ContentAlignment) As Label
        Dim l As New Label()
        l.AutoSize = False
        l.Font = font
        l.ForeColor = color
        l.Size = New Size(width, Theme.S(30))
        l.TextAlign = align
        Return l
    End Function

    Private Sub BuildRight()
        _right.Dock = DockStyle.Right
        _right.Width = CartWidth
        _right.BackColor = Color.White
        AddHandler _right.Resize, AddressOf OnRightResize
        AddHandler _right.Paint, AddressOf OnRightPaint

        BuildCart()
        BuildPay()
        BuildReceipt()

        _right.Controls.Add(_cartPanel)
        _right.Controls.Add(_payPanel)
        _right.Controls.Add(_receiptPanel)
        _payPanel.Visible = False
        _receiptPanel.Visible = False
    End Sub

    Private Sub OnRightPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, 0, _right.Height)
        End Using
    End Sub

    Private Sub OnRightResize(sender As Object, e As EventArgs)
        Dim w As Integer = _right.ClientSize.Width
        Dim h As Integer = _right.ClientSize.Height
        _cartPanel.SetBounds(0, 0, w, h)
        _payPanel.SetBounds(If(_payOpen, 0, w), 0, w, h)
        _receiptPanel.SetBounds(If(_receiptOpen, 0, w), 0, w, h)
        LayoutPay()
        LayoutReceipt()
    End Sub

    ' ---- cart
    Private Sub BuildCart()
        _cartPanel.BackColor = Color.White

        _cartHead.Dock = DockStyle.Top
        _cartHead.Height = Theme.S(60)
        _cartHead.BackColor = Color.White

        _lblCartTitle.AutoSize = True
        _lblCartTitle.Font = Title18
        _lblCartTitle.ForeColor = Theme.Maroon
        _lblCartTitle.Text = "Current sale"
        _lblCartTitle.Location = New Point(Theme.S(16), Theme.S(12))

        _lblCartMsg.AutoSize = False
        _lblCartMsg.Font = Theme.FontSmall
        _lblCartMsg.ForeColor = Theme.AlertRed
        _lblCartMsg.Size = New Size(Theme.S(200), Theme.S(36))
        _lblCartMsg.TextAlign = ContentAlignment.MiddleLeft
        _lblCartMsg.Location = New Point(Theme.S(190), Theme.S(12))

        _lnkClear.AutoSize = True
        _lnkClear.Text = "Clear all"
        _lnkClear.Font = Theme.FontBase
        _lnkClear.LinkColor = Theme.Maroon
        _lnkClear.ActiveLinkColor = Theme.MaroonDark
        _lnkClear.VisitedLinkColor = Theme.Maroon
        _lnkClear.LinkBehavior = LinkBehavior.HoverUnderline
        _lnkClear.Cursor = Cursors.Hand
        AddHandler _lnkClear.LinkClicked, AddressOf OnClearAll

        _cartHead.Controls.Add(_lblCartTitle)
        _cartHead.Controls.Add(_lblCartMsg)
        _cartHead.Controls.Add(_lnkClear)
        AddHandler _cartHead.Resize, AddressOf OnCartHeadResize

        _cartList.Dock = DockStyle.Fill
        _cartList.FlowDirection = FlowDirection.TopDown
        _cartList.WrapContents = False
        _cartList.AutoScroll = True
        _cartList.BackColor = Color.White
        AddHandler _cartList.SizeChanged, AddressOf OnCartListSize

        _cartEmpty.Dock = DockStyle.Fill
        _cartEmpty.TextAlign = ContentAlignment.MiddleCenter
        _cartEmpty.Font = Theme.FontBase
        _cartEmpty.ForeColor = Theme.TextMuted
        _cartEmpty.BackColor = Color.White
        _cartEmpty.Text = "Tap an item to add it," & Environment.NewLine & "or scan its barcode."

        BuildSummary()

        _cartPanel.Controls.Add(_cartList)
        _cartPanel.Controls.Add(_cartEmpty)
        _cartPanel.Controls.Add(_summary)
        _cartPanel.Controls.Add(_cartHead)
    End Sub

    Private Sub OnCartHeadResize(sender As Object, e As EventArgs)
        _lnkClear.Location = New Point(_cartHead.ClientSize.Width - Theme.S(16) - _lnkClear.Width, Theme.S(22))
        _lblCartMsg.Width = Math.Max(Theme.S(60), _lnkClear.Left - _lblCartMsg.Left - Theme.S(8))
    End Sub

    Private Sub OnCartListSize(sender As Object, e As EventArgs)
        Dim w As Integer = Math.Max(Theme.S(200), _cartList.ClientSize.Width - Theme.S(4))
        For Each c As Control In _cartList.Controls
            c.Width = w
        Next
    End Sub

    Private Sub BuildSummary()
        _summary.Dock = DockStyle.Bottom
        _summary.Height = Theme.S(364)
        _summary.BackColor = Color.FromArgb(251, 249, 249)
        AddHandler _summary.Paint, AddressOf OnSummaryPaint
        AddHandler _summary.Resize, AddressOf OnSummaryResize

        Dim subCap As Label = MakeLabel(MidBold, Theme.TextDark, 120, ContentAlignment.MiddleLeft)
        subCap.Text = "Subtotal"
        subCap.Location = New Point(Theme.S(16), Theme.S(12))
        _valSub.AutoSize = False
        _valSub.Font = MidBold
        _valSub.ForeColor = Theme.TextDark
        _valSub.TextAlign = ContentAlignment.MiddleRight
        _valSub.Size = New Size(Theme.S(200), Theme.S(30))

        _cmbDiscount.DropDownStyle = ComboBoxStyle.DropDownList
        _cmbDiscount.FlatStyle = FlatStyle.Flat
        _cmbDiscount.Font = Theme.MakeFont("Segoe UI", 11.5F)
        _cmbDiscount.BackColor = Color.White
        _cmbDiscount.Width = Theme.S(250)
        _cmbDiscount.Location = New Point(Theme.S(16), Theme.S(54))
        AddHandler _cmbDiscount.SelectedIndexChanged, AddressOf OnDiscountChanged
        _valDisc.AutoSize = False
        _valDisc.Font = MidBold
        _valDisc.ForeColor = Theme.TextDark
        _valDisc.TextAlign = ContentAlignment.MiddleRight
        _valDisc.Size = New Size(Theme.S(160), Theme.S(30))

        _btnSpecial.Text = "Special price"
        _btnSpecial.MakeSecondary()
        _btnSpecial.Size = New Size(Theme.S(170), Theme.S(44))
        _btnSpecial.Location = New Point(Theme.S(16), Theme.S(100))
        AddHandler _btnSpecial.Click, AddressOf OnSpecial
        _valSpecial.AutoSize = False
        _valSpecial.Font = MidBold
        _valSpecial.ForeColor = Theme.TextDark
        _valSpecial.TextAlign = ContentAlignment.MiddleRight
        _valSpecial.Size = New Size(Theme.S(160), Theme.S(30))
        _lnkSpecialX.AutoSize = True
        _lnkSpecialX.Text = "remove"
        _lnkSpecialX.Font = Theme.FontSmall
        _lnkSpecialX.LinkColor = Theme.AlertRed
        _lnkSpecialX.ActiveLinkColor = Theme.AlertRed
        _lnkSpecialX.VisitedLinkColor = Theme.AlertRed
        _lnkSpecialX.Visible = False
        AddHandler _lnkSpecialX.LinkClicked, AddressOf OnRemoveSpecial

        _lblVat.AutoSize = False
        _lblVat.Font = Theme.FontSmall
        _lblVat.ForeColor = Theme.TextMuted
        _lblVat.Size = New Size(Theme.S(260), Theme.S(22))
        _lblVat.Location = New Point(Theme.S(16), Theme.S(156))
        _valVat.AutoSize = False
        _valVat.Font = Theme.FontSmall
        _valVat.ForeColor = Theme.TextMuted
        _valVat.TextAlign = ContentAlignment.MiddleRight
        _valVat.Size = New Size(Theme.S(160), Theme.S(22))

        Dim totalCap As Label = MakeLabel(Theme.MakeFont("Segoe UI Semibold", 15.0F), Theme.TextDark, 120, ContentAlignment.MiddleLeft)
        totalCap.Text = "TOTAL"
        totalCap.Location = New Point(Theme.S(16), Theme.S(190))
        totalCap.Height = Theme.S(56)
        _valTotal.AutoSize = False
        _valTotal.Font = Theme.MakeFont("Segoe UI Semibold", 30.0F)
        _valTotal.ForeColor = Theme.Maroon
        _valTotal.TextAlign = ContentAlignment.MiddleRight
        _valTotal.Size = New Size(Theme.S(320), Theme.S(56))

        _btnProceed.Text = "PROCEED"
        _btnProceed.Font = Theme.MakeFont("Segoe UI Semibold", 16.0F)
        _btnProceed.Size = New Size(Theme.S(440), Theme.S(72))
        _btnProceed.Location = New Point(Theme.S(16), Theme.S(262))
        AddHandler _btnProceed.Click, AddressOf OnProceed

        _summary.Controls.Add(subCap)
        _summary.Controls.Add(_valSub)
        _summary.Controls.Add(_cmbDiscount)
        _summary.Controls.Add(_valDisc)
        _summary.Controls.Add(_btnSpecial)
        _summary.Controls.Add(_valSpecial)
        _summary.Controls.Add(_lnkSpecialX)
        _summary.Controls.Add(_lblVat)
        _summary.Controls.Add(_valVat)
        _summary.Controls.Add(totalCap)
        _summary.Controls.Add(_valTotal)
        _summary.Controls.Add(_btnProceed)
    End Sub

    Private Sub OnSummaryPaint(sender As Object, e As PaintEventArgs)
        Using p As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawLine(p, 0, 0, _summary.Width, 0)
            e.Graphics.DrawLine(p, Theme.S(16), Theme.S(182), _summary.Width - Theme.S(16), Theme.S(182))
        End Using
    End Sub

    Private Sub OnSummaryResize(sender As Object, e As EventArgs)
        Dim w As Integer = _summary.ClientSize.Width
        _valSub.Location = New Point(w - Theme.S(16) - _valSub.Width, Theme.S(12))
        _valDisc.Location = New Point(w - Theme.S(16) - _valDisc.Width, Theme.S(56))
        _valSpecial.Location = New Point(w - Theme.S(16) - _valSpecial.Width, Theme.S(107))
        _lnkSpecialX.Location = New Point(_btnSpecial.Right + Theme.S(12), Theme.S(114))
        _valVat.Location = New Point(w - Theme.S(16) - _valVat.Width, Theme.S(156))
        _valTotal.Location = New Point(w - Theme.S(16) - _valTotal.Width, Theme.S(190))
        _btnProceed.Width = w - Theme.S(32)
    End Sub

    ' ---- payment
    Private Sub BuildPay()
        _payPanel.BackColor = Color.White

        Dim title As Label = MakeLabel(Title18, Theme.Maroon, 300, ContentAlignment.MiddleLeft)
        title.Text = "Payment"
        title.Location = New Point(Theme.S(20), Theme.S(14))
        title.Height = Theme.S(40)

        Dim totCap As Label = _lblPayCap
        totCap.AutoSize = False
        totCap.Font = Theme.FontCaption
        totCap.ForeColor = Theme.TextMuted
        totCap.TextAlign = ContentAlignment.MiddleLeft
        totCap.Size = New Size(Theme.S(300), Theme.S(20))
        totCap.Text = "TOTAL TO PAY"
        totCap.Location = New Point(Theme.S(20), Theme.S(62))

        _lblPayLoy.AutoSize = False
        _lblPayLoy.Font = Theme.FontSmall
        _lblPayLoy.ForeColor = Theme.Maroon
        _lblPayLoy.Location = New Point(Theme.S(150), Theme.S(12))
        _lblPayLoy.Size = New Size(Theme.S(300), Theme.S(32))

        _lnkLoy.AutoSize = True
        _lnkLoy.Font = Theme.FontSmall
        _lnkLoy.LinkColor = Theme.Maroon
        _lnkLoy.ActiveLinkColor = Theme.MaroonDark
        _lnkLoy.VisitedLinkColor = Theme.Maroon
        _lnkLoy.LinkBehavior = LinkBehavior.HoverUnderline
        _lnkLoy.Cursor = Cursors.Hand
        _lnkLoy.Text = "Add a loyalty card"
        _lnkLoy.Location = New Point(Theme.S(150), Theme.S(44))
        AddHandler _lnkLoy.LinkClicked, AddressOf OnChangeLoyalty

        _payPanel.Controls.Add(_lblPayLoy)
        _payPanel.Controls.Add(_lnkLoy)
        _lblPayTotal.AutoSize = False
        _lblPayTotal.Font = BigMoney
        _lblPayTotal.ForeColor = Theme.TextDark
        _lblPayTotal.Location = New Point(Theme.S(20), Theme.S(82))
        _lblPayTotal.Size = New Size(Theme.S(300), Theme.S(54))

        Dim recCap As Label = MakeLabel(Theme.FontCaption, Theme.TextMuted, 300, ContentAlignment.MiddleLeft)
        recCap.Text = "AMOUNT RECEIVED"
        recCap.Location = New Point(Theme.S(20), Theme.S(142))
        recCap.Height = Theme.S(20)
        _lblReceived.AutoSize = False
        _lblReceived.Font = BigMoney
        _lblReceived.ForeColor = Theme.Maroon
        _lblReceived.BackColor = Theme.MaroonTint
        _lblReceived.TextAlign = ContentAlignment.MiddleRight
        _lblReceived.Location = New Point(Theme.S(20), Theme.S(164))
        _lblReceived.Size = New Size(Theme.S(300), Theme.S(70))

        _lblChange.AutoSize = False
        _lblChange.Font = Theme.MakeFont("Segoe UI Semibold", 15.0F)
        _lblChange.Location = New Point(Theme.S(20), Theme.S(240))
        _lblChange.Size = New Size(Theme.S(300), Theme.S(34))

        Dim amounts As Decimal() = {0D, 100D, 500D, 1000D}
        Dim names As String() = {"Exact", "+ 100", "+ 500", "+ 1,000"}
        For i As Integer = 0 To 3
            Dim b As New MaroonButton()
            b.Text = names(i)
            b.Tag = amounts(i)
            b.Height = Theme.S(46)
            b.Font = Theme.MakeFont("Segoe UI Semibold", 12.0F)
            b.MakeSecondary()
            AddHandler b.Click, AddressOf OnQuickAmount
            _quick.Add(b)
            _payPanel.Controls.Add(b)
        Next

        AddHandler _padPay.KeyPressed, AddressOf OnPayKey

        _lblPayMsg.AutoSize = False
        _lblPayMsg.Font = Theme.FontSmall
        _lblPayMsg.ForeColor = Theme.AlertRed

        _btnPayBack.Text = "Back"
        _btnPayBack.MakeSecondary()
        _btnPayBack.Font = Theme.MakeFont("Segoe UI Semibold", 13.0F)
        AddHandler _btnPayBack.Click, AddressOf OnPayBack

        _btnPayDone.Text = "Complete sale"
        _btnPayDone.Font = Theme.MakeFont("Segoe UI Semibold", 15.0F)
        AddHandler _btnPayDone.Click, AddressOf OnPayDone

        _payPanel.Controls.Add(title)
        _payPanel.Controls.Add(totCap)
        _payPanel.Controls.Add(_lblPayTotal)
        _payPanel.Controls.Add(recCap)
        _payPanel.Controls.Add(_lblReceived)
        _payPanel.Controls.Add(_lblChange)
        _payPanel.Controls.Add(_padPay)
        _payPanel.Controls.Add(_lblPayMsg)
        _payPanel.Controls.Add(_btnPayBack)
        _payPanel.Controls.Add(_btnPayDone)
    End Sub

    Private Sub LayoutPay()
        Dim w As Integer = _payPanel.ClientSize.Width
        Dim h As Integer = _payPanel.ClientSize.Height
        If w <= 0 OrElse h <= 0 Then Return

        _lblPayTotal.Width = w - Theme.S(40)
        _lblPayLoy.Width = Math.Max(Theme.S(120), w - Theme.S(170))
        _lblReceived.Width = w - Theme.S(40)
        _lblChange.Width = w - Theme.S(40)

        Dim gap As Integer = Theme.S(8)
        Dim qw As Integer = (w - Theme.S(40) - 3 * gap) \ 4
        For i As Integer = 0 To _quick.Count - 1
            _quick(i).SetBounds(Theme.S(20) + i * (qw + gap), Theme.S(284), qw, Theme.S(46))
        Next

        Dim padTop As Integer = Theme.S(340)
        Dim bottomH As Integer = Theme.S(64)
        _btnPayBack.SetBounds(Theme.S(20), h - Theme.S(16) - bottomH, Theme.S(120), bottomH)
        _btnPayDone.SetBounds(Theme.S(20) + Theme.S(120) + Theme.S(10), h - Theme.S(16) - bottomH, w - Theme.S(40) - Theme.S(130), bottomH)
        _lblPayMsg.SetBounds(Theme.S(20), h - Theme.S(16) - bottomH - Theme.S(24), w - Theme.S(40), Theme.S(22))
        _padPay.SetBounds(Theme.S(20), padTop, w - Theme.S(40), Math.Max(Theme.S(180), h - padTop - Theme.S(16) - bottomH - Theme.S(28)))
    End Sub

    ' ---- receipt
    Private Sub BuildReceipt()
        _receiptPanel.BackColor = Color.White

        Dim title As Label = MakeLabel(Title18, Color.FromArgb(22, 130, 60), 300, ContentAlignment.MiddleLeft)
        title.Text = "Sale complete"
        title.Location = New Point(Theme.S(20), Theme.S(12))
        title.Height = Theme.S(40)

        _lblRecChange.AutoSize = False
        _lblRecChange.Font = Theme.MakeFont("Segoe UI Semibold", 24.0F)
        _lblRecChange.ForeColor = Theme.Maroon
        _lblRecChange.Location = New Point(Theme.S(20), Theme.S(54))
        _lblRecChange.Size = New Size(Theme.S(300), Theme.S(50))

        _paper.Location = New Point(Theme.S(10), Theme.S(112))

        _lblRecMsg.AutoSize = False
        _lblRecMsg.Font = Theme.FontSmall
        _lblRecMsg.ForeColor = Theme.AlertRed

        _btnPrint.Text = "Print receipt"
        _btnPrint.MakeSecondary()
        _btnPrint.Font = Theme.MakeFont("Segoe UI Semibold", 13.0F)
        AddHandler _btnPrint.Click, AddressOf OnPrintClicked

        _btnNew.Text = "New sale"
        _btnNew.Font = Theme.MakeFont("Segoe UI Semibold", 15.0F)
        AddHandler _btnNew.Click, AddressOf OnNewSale

        _receiptPanel.Controls.Add(title)
        _receiptPanel.Controls.Add(_lblRecChange)
        _receiptPanel.Controls.Add(_paper)
        _receiptPanel.Controls.Add(_lblRecMsg)
        _receiptPanel.Controls.Add(_btnPrint)
        _receiptPanel.Controls.Add(_btnNew)
    End Sub

    Private Sub LayoutReceipt()
        Dim w As Integer = _receiptPanel.ClientSize.Width
        Dim h As Integer = _receiptPanel.ClientSize.Height
        If w <= 0 OrElse h <= 0 Then Return
        _lblRecChange.Width = w - Theme.S(40)
        Dim bottomH As Integer = Theme.S(64)
        _btnPrint.SetBounds(Theme.S(20), h - Theme.S(16) - bottomH, (w - Theme.S(50)) \ 2, bottomH)
        _btnNew.SetBounds(Theme.S(20) + (w - Theme.S(50)) \ 2 + Theme.S(10), h - Theme.S(16) - bottomH, (w - Theme.S(50)) \ 2, bottomH)
        _lblRecMsg.SetBounds(Theme.S(20), h - Theme.S(16) - bottomH - Theme.S(24), w - Theme.S(40), Theme.S(22))
        _paper.SetBounds(Theme.S(10), Theme.S(112), w - Theme.S(20), Math.Max(Theme.S(120), h - Theme.S(112) - Theme.S(16) - bottomH - Theme.S(30)))
    End Sub

    ' ------------------------------------------------------------------ start / data
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        _lblWho.Text = AppSession.FullName & "  (" & Permissions.RoleDisplay(AppSession.Role) & ")"
        OnClockTick(Me, EventArgs.Empty)
        OnTopResize(Me, EventArgs.Empty)
        OnSummaryResize(Me, EventArgs.Empty)
        OnCartHeadResize(Me, EventArgs.Empty)
        OnRightResize(Me, EventArgs.Empty)

        LoadCategories()
        LoadDiscounts()
        LoadVat()
        LoadTiles()
        LoadStats()
        RenderCart()
        UpdateTotals()
        _tick.Start()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _search.FocusInput()
    End Sub

    Private Sub OnClockTick(sender As Object, e As EventArgs)
        _lblClock.Text = DateTime.Now.ToString("ddd, MMM d  h:mm:ss tt")
        OnTopResize(Me, EventArgs.Empty)
    End Sub

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

    Private Function CurrentCategoryId() As Integer
        Dim i As Integer = _cat.SelectedIndex
        If i <= 0 OrElse i >= _catIds.Count Then Return 0
        Return _catIds(i)
    End Function

    Private Function CurrentDiscountId() As Integer
        Dim i As Integer = _cmbDiscount.SelectedIndex
        If i <= 0 OrElse i >= _discIds.Count Then Return 0
        Return _discIds(i)
    End Function

    Private Function CurrentDiscountPct() As Decimal
        Dim i As Integer = _cmbDiscount.SelectedIndex
        If i <= 0 OrElse i >= _discPct.Count Then Return 0D
        Return _discPct(i)
    End Function

    Private Sub LoadDiscounts()
        Dim keep As Integer = CurrentDiscountId()
        _loading = True
        Try
            _cmbDiscount.Items.Clear()
            _discIds.Clear()
            _discPct.Clear()
            _cmbDiscount.Items.Add("No discount")
            _discIds.Add(0)
            _discPct.Add(0D)
            Dim idx As Integer = 0
            For Each r As DataRow In PosRepository.GetActiveDiscounts().Rows
                Dim pct As Decimal = Convert.ToDecimal(r("percentage"))
                _cmbDiscount.Items.Add(r("name").ToString() & "  (" & Percent.Format(pct) & ")")
                _discIds.Add(Convert.ToInt32(r("id")))
                _discPct.Add(pct)
                If Convert.ToInt32(r("id")) = keep Then idx = _cmbDiscount.Items.Count - 1
            Next
            _cmbDiscount.SelectedIndex = idx
        Catch
            If _cmbDiscount.Items.Count = 0 Then
                _cmbDiscount.Items.Add("No discount")
                _discIds.Add(0)
                _discPct.Add(0D)
                _cmbDiscount.SelectedIndex = 0
            End If
        Finally
            _loading = False
        End Try
    End Sub

    Private Sub LoadVat()
        Try
            _vatPct = VatRepository.GetActivePercentage()
            _peso = Loyalty.PesoPerPoint()
            _spend = Loyalty.SpendPerPoint()
        Catch
            ' keep the last known rate
        End Try
    End Sub

    Private Sub LoadStats()
        Try
            Dim s As MyStats = PosRepository.GetMyStats(AppSession.UserId)
            _cardCount.Value = s.Count.ToString()
            _cardAmount.Value = Currency.Format(s.Amount)
        Catch
            ' offline: keep what is shown
        End Try
    End Sub

    Private Sub LoadTiles()
        If _loading Then Return
        Dim scroll As Integer = _tiles.VerticalScroll.Value
        Try
            Dim dt As DataTable = PosRepository.GetSellable(_search.Value.Trim(), CurrentCategoryId())

            _tiles.SuspendLayout()
            For i As Integer = _tiles.Controls.Count - 1 To 0 Step -1
                Dim c As Control = _tiles.Controls(i)
                _tiles.Controls.RemoveAt(i)
                c.Dispose()
            Next
            For Each r As DataRow In dt.Rows
                Dim t As New ProductTile()
                t.ProductId = Convert.ToInt32(r("id"))
                t.Title = r("name").ToString()
                t.SizeText = r("measurement").ToString()
                t.Category = r("category").ToString()
                t.Price = Convert.ToDecimal(r("selling_price"))
                t.Stock = Convert.ToInt32(r("qty_on_hand"))
                t.Margin = New Padding(0, 0, Theme.S(12), Theme.S(12))
                AddHandler t.Click, AddressOf OnTileClick
                _tiles.Controls.Add(t)
            Next
            _tiles.ResumeLayout()
            FitTiles()
            If scroll > 0 Then _tiles.AutoScrollPosition = New Point(0, scroll)

            _tilesNote.Visible = (dt.Rows.Count = 0)
            If dt.Rows.Count = 0 Then
                _tilesNote.Text = If(_search.Value.Trim().Length > 0 OrElse CurrentCategoryId() > 0,
                                     "No item matches.", "Nothing to sell yet." & Environment.NewLine & "Items with no stock are hidden.")
                _tilesNote.BringToFront()
            End If
        Catch ex As Exception
            _tilesNote.Text = "Could not load the items: " & ex.Message
            _tilesNote.Visible = True
            _tilesNote.BringToFront()
        End Try
    End Sub

    Private Sub OnSearchTyped()
        _searchTimer.Stop()
        _searchTimer.Start()
    End Sub

    Private Sub OnSearchTimer(sender As Object, e As EventArgs)
        _searchTimer.Stop()
        LoadTiles()
    End Sub

    Private Sub OnCategoryChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        LoadTiles()
    End Sub

    Private Sub OnLiveTimer(sender As Object, e As EventArgs)
        _liveTimer.Stop()
        LoadCategories()
        LoadDiscounts()
        LoadVat()
        LoadTiles()
        LoadStats()
        UpdateTotals()
    End Sub

    ''' <summary>A barcode scanner types the code and presses Enter: add that item. Otherwise add the only match.</summary>
    Private Sub OnSearchEnter()
        _searchTimer.Stop()
        Dim text As String = _search.Value.Trim()
        If text.Length = 0 Then Return

        Dim id As Integer = 0
        Try
            id = ProductLookup.FindByBarcode(text)
        Catch ex As Exception
            Toast("Could not reach the database.")
            Return
        End Try

        If id = 0 Then
            LoadTiles()
            If _tiles.Controls.Count = 1 Then
                id = DirectCast(_tiles.Controls(0), ProductTile).ProductId
            Else
                Toast(If(_tiles.Controls.Count = 0, "No item matches.", "Pick the item from the list."))
                Return
            End If
        End If

        AddToCart(id)
        _search.Value = ""
        LoadTiles()
        _search.FocusInput()
    End Sub

    ' ------------------------------------------------------------------ the cart
    Private Sub Toast(text As String)
        _lblCartMsg.Text = text
        _msgTimer.Stop()
        _msgTimer.Start()
    End Sub

    Private Sub OnMsgTimer(sender As Object, e As EventArgs)
        _msgTimer.Stop()
        _lblCartMsg.Text = ""
    End Sub

    Private Function FindItem(productId As Integer) As CartItem
        For Each it As CartItem In _items
            If it.ProductId = productId Then Return it
        Next
        Return Nothing
    End Function

    Private Sub OnTileClick(sender As Object, e As EventArgs)
        Dim t As ProductTile = TryCast(sender, ProductTile)
        If t Is Nothing Then Return
        AddToCart(t.ProductId)
    End Sub

    Private Sub AddToCart(productId As Integer)
        Dim row As DataRow = Nothing
        Try
            row = PosRepository.GetSellableOne(productId)
        Catch ex As Exception
            Toast("Could not reach the database.")
            Return
        End Try
        If row Is Nothing Then
            Toast("That item is out of stock.")
            LoadTiles()
            Return
        End If

        Dim stock As Integer = Convert.ToInt32(row("qty_on_hand"))
        Dim price As Decimal = Convert.ToDecimal(row("selling_price"))
        Dim it As CartItem = FindItem(productId)
        If it IsNot Nothing Then
            If it.Qty >= stock Then
                Toast("No more stock of " & it.Name & ".")
                Return
            End If
            it.Qty += 1
            it.Stock = stock
            it.UnitPrice = price
        Else
            it = New CartItem()
            it.ProductId = productId
            it.Name = row("name").ToString()
            it.Size = row("measurement").ToString()
            it.Barcode = row("barcode").ToString()
            it.UnitPrice = price
            it.CostPrice = Convert.ToDecimal(row("cost_price"))
            it.Qty = 1
            it.Stock = stock
            _items.Add(it)
        End If
        OnCartChanged()
    End Sub

    ''' <summary>Any change to the cart cancels a special price (it was agreed for the old total).</summary>
    Private Sub OnCartChanged()
        ResetLoyalty()
        If _specialAmount > 0D Then
            ClearSpecial()
            Toast("Special price removed: the cart changed.")
        End If
        RenderCart()
        UpdateTotals()
    End Sub

    Private Sub RenderCart()
        _cartList.SuspendLayout()
        For i As Integer = _cartList.Controls.Count - 1 To 0 Step -1
            Dim c As Control = _cartList.Controls(i)
            _cartList.Controls.RemoveAt(i)
            c.Dispose()
        Next
        Dim w As Integer = Math.Max(Theme.S(200), _cartList.ClientSize.Width - Theme.S(4))
        For Each it As CartItem In _items
            Dim line As New CartLine(it)
            line.Width = w
            line.Margin = New Padding(0)
            AddHandler line.MinusClicked, AddressOf OnLineMinus
            AddHandler line.PlusClicked, AddressOf OnLinePlus
            AddHandler line.QtyClicked, AddressOf OnLineQty
            AddHandler line.RemoveClicked, AddressOf OnLineRemove
            _cartList.Controls.Add(line)
        Next
        _cartList.ResumeLayout()
        _cartEmpty.Visible = (_items.Count = 0)
        If _items.Count = 0 Then _cartEmpty.BringToFront()
    End Sub

    Private Sub OnLineMinus(item As CartItem)
        item.Qty -= 1
        If item.Qty <= 0 Then _items.Remove(item)
        OnCartChanged()
    End Sub

    Private Sub OnLinePlus(item As CartItem)
        If item.Qty >= item.Stock Then
            Toast("Only " & item.Stock.ToString() & " in stock.")
            Return
        End If
        item.Qty += 1
        OnCartChanged()
    End Sub

    Private Sub OnLineQty(item As CartItem)
        Using dlg As New QtyDialog(item.Label, item.Qty, item.Stock)
            If dlg.ShowDialog(OwnerOf(Me)) = DialogResult.OK Then
                item.Qty = dlg.Quantity
                OnCartChanged()
            End If
        End Using
    End Sub

    Private Sub OnLineRemove(item As CartItem)
        _items.Remove(item)
        OnCartChanged()
    End Sub

    Private Sub OnClearAll(sender As Object, e As LinkLabelLinkClickedEventArgs)
        If _items.Count = 0 Then Return
        If Not Confirm(Me, "POS", "Remove everything from the current sale?", MessageBoxIcon.Question) Then Return
        ResetSale()
    End Sub

    Private Sub ResetSale()
        ResetLoyalty()
        _items.Clear()
        ClearSpecial()
        _loading = True
        If _cmbDiscount.Items.Count > 0 Then _cmbDiscount.SelectedIndex = 0
        _loading = False
        RenderCart()
        UpdateTotals()
    End Sub

    Private Sub OnDiscountChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        ResetLoyalty()
        If _specialAmount > 0D Then
            ClearSpecial()
            Toast("Special price removed: the total changed.")
        End If
        UpdateTotals()
    End Sub

    Private Function SubtotalOf() As Decimal
        Dim s As Decimal = 0D
        For Each it As CartItem In _items
            s += it.LineTotal
        Next
        Return s
    End Function

    Private Function CostOf() As Decimal
        Dim s As Decimal = 0D
        For Each it As CartItem In _items
            s += it.Qty * it.CostPrice
        Next
        Return s
    End Function

    Private Sub UpdateTotals()
        _totals = PosMath.Compute(SubtotalOf(), CostOf(), CurrentDiscountPct(), _specialAmount, _vatPct)
        _valSub.Text = Currency.Format(_totals.Subtotal)
        _valDisc.Text = If(_totals.DiscountAmount > 0D, "-" & Currency.Format(_totals.DiscountAmount), "-")
        _valSpecial.Text = If(_totals.SpecialAmount > 0D, "-" & Currency.Format(_totals.SpecialAmount), "-")
        _lnkSpecialX.Visible = (_totals.SpecialAmount > 0D)
        _btnSpecial.Text = If(_totals.SpecialAmount > 0D, "Special price (approved)", "Special price")
        _lblVat.Text = If(_vatPct > 0D, "VAT included (" & Percent.Format(_vatPct) & ")", "VAT")
        _valVat.Text = If(_vatPct > 0D, Currency.Format(_totals.VatAmount), "-")
        _valTotal.Text = Currency.Format(_totals.Total)
        _btnProceed.Enabled = (_items.Count > 0)
        Dim usingPoints As Boolean = (_loy IsNot Nothing AndAlso _loy.Mode = "use")
        _btnSpecial.Enabled = (_items.Count > 0 AndAlso Not usingPoints)
        _cmbDiscount.Enabled = (_items.Count > 0 AndAlso Not usingPoints)
        _lnkClear.Visible = (_items.Count > 0)
    End Sub

    ' ------------------------------------------------------------------ special price
    Private Sub ClearSpecial()
        _specialAmount = 0D
        _approverId = 0
        _approverName = ""
    End Sub

    Private Sub OnRemoveSpecial(sender As Object, e As LinkLabelLinkClickedEventArgs)
        ResetLoyalty()
        ClearSpecial()
        UpdateTotals()
    End Sub

    Private Sub OnSpecial(sender As Object, e As EventArgs)
        If _items.Count = 0 Then Return
        Dim before As SaleTotals = PosMath.Compute(SubtotalOf(), CostOf(), CurrentDiscountPct(), 0D, _vatPct)
        If before.Total <= 0.01D Then Return

        Using dlg As New SpecialPriceDialog(before.Total, before.CostTotal)
            If dlg.ShowDialog(OwnerOf(Me)) <> DialogResult.OK Then Return
            Dim amount As Decimal = dlg.SpecialAmount

            Dim role As String = Permissions.NormalizeRole(AppSession.Role)
            Dim id As Integer
            Dim nm As String
            If role = "admin" OrElse role = "superadmin" Then
                id = AppSession.UserId
                nm = AppSession.FullName
            Else
                Dim summary As String = "Special price: " & Currency.Format(amount) & " off. The customer would pay " &
                                        Currency.Format(before.Total - amount) & "."
                Using ap As New ApprovalDialog(summary)
                    If ap.ShowDialog(OwnerOf(Me)) <> DialogResult.OK Then Return
                    id = ap.ApproverId
                    nm = ap.ApproverName
                End Using
            End If

            ResetLoyalty()
            _specialAmount = amount
            _approverId = id
            _approverName = nm
            UpdateTotals()
        End Using
    End Sub

    ' ------------------------------------------------------------------ payment
    Private Sub OnProceed(sender As Object, e As EventArgs)
        If _items.Count = 0 Then Return
        ' every sale asks: does the customer have a loyalty card?
        If Not _loyAsked Then
            If Not AskLoyalty() Then Return
        End If
        OpenPay()
    End Sub

    Private Sub ResetLoyalty()
        _loy = Nothing
        _loyAsked = False
    End Sub

    Private Sub ClearDiscountAndSpecial()
        _loading = True
        If _cmbDiscount.Items.Count > 0 Then _cmbDiscount.SelectedIndex = 0
        _loading = False
        ClearSpecial()
    End Sub

    ' Returns False when the cashier chose to go back to the cart.
    Private Function AskLoyalty() As Boolean
        Dim hasDiscount As Boolean = (CurrentDiscountPct() > 0D OrElse _specialAmount > 0D)
        Dim plain As SaleTotals = PosMath.Compute(SubtotalOf(), CostOf(), 0D, 0D, _vatPct)
        Using dlg As New LoyaltyDialog(_totals.Total, plain.Total, hasDiscount)
            Select Case dlg.ShowDialog(OwnerOf(Me))
                Case DialogResult.OK
                    Dim c As LoyaltyChoice = dlg.Choice
                    If c Is Nothing Then Return False
                    If c.Mode = "use" AndAlso hasDiscount Then
                        If Not Confirm(Me, "POS", "Paying with points turns off the discount and the special price on this sale." & Environment.NewLine & "Continue?", MessageBoxIcon.Question) Then Return False
                        ClearDiscountAndSpecial()
                    End If
                    _loy = c
                    _loyAsked = True
                    UpdateTotals()
                    Return True
                Case DialogResult.No
                    _loy = Nothing
                    _loyAsked = True
                    Return True
                Case Else
                    Return False
            End Select
        End Using
    End Function

    Private Sub OnChangeLoyalty(sender As Object, e As LinkLabelLinkClickedEventArgs)
        ResetLoyalty()
        AskLoyalty()
        UpdatePayDisplay()
    End Sub

    Private Function PointsValueNow() As Decimal
        If _loy Is Nothing OrElse _loy.Mode <> "use" Then Return 0D
        Return Math.Min(_totals.Total, PosMath.Round2(_loy.PointsToUse * _peso))
    End Function

    ' what the customer pays in cash: the total minus what points cover
    Private Function CashDue() As Decimal
        Return Math.Max(0D, _totals.Total - PointsValueNow())
    End Function

    Private Sub OpenPay()
        _received.SetValue(0D)
        _lblPayMsg.Text = ""
        UpdatePayDisplay()
        _payOpen = True
        Me.ActiveControl = Nothing
        _slide.SlideIn(_payPanel, _right)
    End Sub

    Private Sub OnPayBack(sender As Object, e As EventArgs)
        ClosePay()
    End Sub

    Private Sub ClosePay()
        If Not _payOpen Then Return
        _payOpen = False
        _slide.SlideOut(_payPanel, _right)
        _search.FocusInput()
    End Sub

    Private Sub OnPayKey(key As String)
        _received.Press(key)
        _lblPayMsg.Text = ""
        UpdatePayDisplay()
    End Sub

    Private Sub OnQuickAmount(sender As Object, e As EventArgs)
        Dim b As Button = TryCast(sender, Button)
        If b Is Nothing Then Return
        Dim amount As Decimal = Convert.ToDecimal(b.Tag)
        If amount = 0D Then
            _received.SetValue(CashDue())
        Else
            _received.SetValue(_received.Value + amount)
        End If
        UpdatePayDisplay()
    End Sub

    Private Sub UpdatePayDisplay()
        Dim usingPoints As Boolean = (_loy IsNot Nothing AndAlso _loy.Mode = "use")
        Dim due As Decimal = CashDue()

        _lblPayCap.Text = If(usingPoints, "CASH TO PAY (AFTER POINTS)", "TOTAL TO PAY")
        _lblPayTotal.Text = Currency.Format(due)
        _lblReceived.Text = Currency.Peso & If(_received.Text.Length = 0, "0", _received.Text)

        ' the loyalty line
        If _loy Is Nothing Then
            _lblPayLoy.Text = ""
            _lnkLoy.Text = "Add a loyalty card"
        ElseIf usingPoints Then
            _lblPayLoy.Text = "Member: " & _loy.Name & Environment.NewLine & "Using " & Loyalty.Fmt(_loy.PointsToUse) & " pts (-" & Currency.Format(PointsValueNow()) & ")"
            _lnkLoy.Text = "Change loyalty choice"
        Else
            _lblPayLoy.Text = "Member: " & _loy.Name & Environment.NewLine & "Will earn +" & Loyalty.Fmt(Loyalty.PointsFor(_totals.Total, _spend)) & " pts"
            _lnkLoy.Text = "Change loyalty choice"
        End If

        Dim got As Decimal = _received.Value
        If usingPoints AndAlso due <= 0D Then
            _lblChange.ForeColor = Color.FromArgb(22, 130, 60)
            _lblChange.Text = "Paid in full with points"
        ElseIf got >= due AndAlso got > 0D Then
            _lblChange.ForeColor = Color.FromArgb(22, 130, 60)
            _lblChange.Text = "Change  " & Currency.Format(got - due)
        ElseIf got > 0D Then
            _lblChange.ForeColor = Theme.AlertRed
            _lblChange.Text = "Short by  " & Currency.Format(due - got)
        Else
            _lblChange.Text = ""
        End If
        _btnPayDone.Enabled = (got >= due AndAlso _totals.Total > 0D)
    End Sub

    Private Sub OnPayDone(sender As Object, e As EventArgs)
        DoComplete()
    End Sub

    Private Sub DoComplete()
        If _items.Count = 0 OrElse Not _btnPayDone.Enabled Then Return

        Dim req As New SaleRequest()
        For Each it As CartItem In _items
            Dim l As New SaleLine()
            l.ProductId = it.ProductId
            l.Qty = it.Qty
            l.ExpectedPrice = it.UnitPrice
            req.Lines.Add(l)
        Next
        req.DiscountId = CurrentDiscountId()
        req.SpecialAmount = _totals.SpecialAmount
        req.ApproverId = _approverId
        req.AmountPaid = _received.Value
        If _loy IsNot Nothing Then
            req.CustomerId = _loy.CustomerId
            req.PointsMode = _loy.Mode
            req.PointsToUse = _loy.PointsToUse
        End If
        req.CashierId = AppSession.UserId
        req.CashierName = AppSession.FullName

        _btnPayDone.Enabled = False
        Cursor = Cursors.WaitCursor
        Dim outcome As SaleOutcome = Nothing
        Try
            outcome = PosRepository.CompleteSale(req)
        Catch ex As Exception
            outcome = New SaleOutcome()
            outcome.Result = SaleResult.Failed
            outcome.Message = "Could not save the sale: " & ex.Message
        Finally
            Cursor = Cursors.Default
        End Try

        Select Case outcome.Result
            Case SaleResult.Ok
                FinishSale(outcome)
            Case SaleResult.OutOfStock, SaleResult.PriceChanged
                ClosePay()
                RefreshCartFromDb()
                Toast(outcome.Message)
                Notify(Me, "POS", outcome.Message & Environment.NewLine & "The cart was updated. Check it and try again.", MessageBoxIcon.Warning)
            Case Else
                _lblPayMsg.Text = outcome.Message
                UpdatePayDisplay()
        End Select
    End Sub

    Private Sub FinishSale(outcome As SaleOutcome)
        Dim itemCount As Integer = _items.Count
        Dim special As Decimal = _totals.SpecialAmount
        Dim approver As String = _approverName
        Dim total As Decimal = _totals.Total

        AuditLog.Write("SALE", "POS", "Sale " & outcome.ReceiptNo & ": " & itemCount.ToString() & " item(s), total " & Currency.Format(total))
        If special > 0D Then
            AuditLog.Write("SPECIAL_PRICE", "POS", "Special price on " & outcome.ReceiptNo & ": " & Currency.Format(special) &
                           " off, approved by " & approver & " (cashier " & AppSession.FullName & ")")
        End If

        ' loyalty: record it and send the customer a text message about their points
        If outcome.CustomerName.Length > 0 Then
            AuditLog.Write("LOYALTY", "POS", outcome.ReceiptNo & ": " & outcome.CustomerName & If(outcome.PointsUsed > 0D,
                           " used " & Loyalty.Fmt(outcome.PointsUsed) & " pts", " earned " & Loyalty.Fmt(outcome.PointsEarned) & " pts") &
                           " (balance " & Loyalty.Fmt(outcome.PointsBalance) & ")")
            Try
                Dim text As String = ""
                If outcome.PointsUsed > 0D Then
                    text = SmsService.UsedText(outcome.CustomerName, outcome.PointsUsed, outcome.PointsBalance, outcome.ReceiptNo)
                ElseIf outcome.PointsEarned > 0D Then
                    text = SmsService.EarnedText(outcome.CustomerName, outcome.Total, outcome.PointsEarned, outcome.PointsBalance, outcome.ReceiptNo)
                End If
                If text.Length > 0 AndAlso outcome.CustomerPhone.Length > 0 Then SmsService.QueueSms(outcome.CustomerPhone, text)
            Catch
                ' the sale is saved; a message that cannot be queued must not stop the cashier
            End Try
        End If

        Try
            _lastReceipt = PosRepository.LoadReceipt(outcome.SaleId)
        Catch
            _lastReceipt = Nothing
        End Try

        _paper.Model = _lastReceipt
        If _lastReceipt IsNot Nothing Then
            _lblRecChange.Text = "Change  " & Currency.Format(_lastReceipt.Change)
        Else
            _lblRecChange.Text = outcome.ReceiptNo
        End If
        _lblRecMsg.Text = ""

        ResetSale()
        LoadTiles()
        LoadStats()

        _receiptOpen = True
        _slide.SlideIn(_receiptPanel, _right)
    End Sub

    Private Sub OnPrintClicked(sender As Object, e As EventArgs)
        If _lastReceipt Is Nothing Then Return
        _lblRecMsg.Text = ""
        Try
            ReceiptRenderer.PrintReceipt(_lastReceipt)
            _lblRecMsg.ForeColor = Color.FromArgb(22, 130, 60)
            _lblRecMsg.Text = "Sent to the printer."
        Catch ex As Exception
            _lblRecMsg.ForeColor = Theme.AlertRed
            _lblRecMsg.Text = "Could not print: " & ex.Message
        End Try
    End Sub

    Private Sub OnNewSale(sender As Object, e As EventArgs)
        NewSale()
    End Sub

    Private Sub NewSale()
        If Not _receiptOpen Then Return
        _receiptOpen = False
        _payOpen = False
        _payPanel.Visible = False
        _payPanel.Left = _right.ClientSize.Width
        _slide.SlideOut(_receiptPanel, _right)
        _search.FocusInput()
    End Sub

    ''' <summary>After a price or stock problem: bring the cart back in line with the database.</summary>
    Private Sub RefreshCartFromDb()
        For i As Integer = _items.Count - 1 To 0 Step -1
            Dim it As CartItem = _items(i)
            Dim row As DataRow = Nothing
            Try
                row = PosRepository.GetSellableOne(it.ProductId)
            Catch
                row = Nothing
            End Try
            If row Is Nothing Then
                _items.RemoveAt(i)
            Else
                it.UnitPrice = Convert.ToDecimal(row("selling_price"))
                it.CostPrice = Convert.ToDecimal(row("cost_price"))
                it.Stock = Convert.ToInt32(row("qty_on_hand"))
                If it.Qty > it.Stock Then it.Qty = it.Stock
            End If
        Next
        ClearSpecial()
        RenderCart()
        UpdateTotals()
        LoadTiles()
    End Sub

    ' ------------------------------------------------------------------ keyboard
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If _receiptOpen Then
            If keyData = Keys.Enter Then
                NewSale()
                Return True
            End If
        ElseIf _payOpen AndAlso Not TypeOf Me.ActiveControl Is TextBoxBase Then
            Dim k As String = PadKeys.FromKeyboard(keyData)
            If k.Length > 0 Then
                OnPayKey(k)
                Return True
            End If
            If keyData = Keys.Enter Then
                DoComplete()
                Return True
            End If
            If keyData = Keys.Escape Then
                ClosePay()
                Return True
            End If
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' ------------------------------------------------------------------ exit
    Private Sub OnExit(sender As Object, e As EventArgs)
        If _items.Count > 0 Then
            If Not Confirm(Me, "POS", "There is an unfinished sale. Leave and discard it?", MessageBoxIcon.Warning) Then Return
        End If
        RaiseEvent ExitRequested()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _tick.Stop()
            _tick.Dispose()
            _searchTimer.Dispose()
            _liveTimer.Dispose()
            _msgTimer.Dispose()
            _slide.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
