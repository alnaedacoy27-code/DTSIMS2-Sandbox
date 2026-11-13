Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>What the cashier chose for a customer with a loyalty card.</summary>
Public Class LoyaltyChoice
    Public Property CustomerId As Integer
    Public Property Name As String = ""
    Public Property Phone As String = ""
    Public Property Card As String = ""
    Public Property Balance As Decimal
    ''' <summary>"earn" = add points to the card, "use" = pay with points.</summary>
    Public Property Mode As String = "earn"
    Public Property PointsToUse As Decimal
End Class

''' <summary>Type a number on the big number pad (or the keyboard).</summary>
Public Class NumberPadDialog
    Inherits ModalDialog

    Private ReadOnly _max As Decimal
    Private ReadOnly _unit As String
    Private ReadOnly _buffer As New AmountBuffer()
    Private ReadOnly _display As New Label()
    Private ReadOnly _pad As New NumpadPad()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    Public Property Value As Decimal

    Public Sub New(title As String, subtitle As String, initial As Decimal, maxValue As Decimal, allowDecimal As Boolean, okText As String, unit As String)
        MyBase.New(title, subtitle, 440, 660)
        _max = maxValue
        _unit = unit
        _buffer.AllowDecimal = allowDecimal
        _buffer.MaxIntegerDigits = 7
        _buffer.SetValue(initial)

        _display.AutoSize = False
        _display.Location = New Point(Theme.S(40), Theme.S(104))
        _display.Size = New Size(Theme.S(360), Theme.S(70))
        _display.TextAlign = ContentAlignment.MiddleRight
        _display.Font = Theme.MakeFont("Segoe UI Semibold", 30.0F)
        _display.ForeColor = Theme.Maroon
        _display.BackColor = Theme.MaroonTint

        _pad.Location = New Point(Theme.S(40), Theme.S(190))
        _pad.Size = New Size(Theme.S(360), Theme.S(330))
        AddHandler _pad.KeyPressed, AddressOf OnPadKey

        Message.Location = New Point(Theme.S(40), Theme.S(528))
        Message.Size = New Size(Theme.S(360), Theme.S(26))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(170), Theme.S(54))
        _cancel.Location = New Point(Theme.S(40), Theme.S(566))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = okText
        _save.Size = New Size(Theme.S(180), Theme.S(54))
        _save.Location = New Point(Theme.S(220), Theme.S(566))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_display)
        Controls.Add(_pad)
        Controls.Add(_cancel)
        Controls.Add(_save)
        ClientSize = New Size(Theme.S(440), Theme.S(566) + Theme.S(54) + Theme.S(24))
        AcceptButton = _save
        CancelButton = _cancel
        ShowValue()
    End Sub

    Private Sub ShowValue()
        _display.Text = If(_buffer.Text.Length = 0, "0", _buffer.Text) & _unit
    End Sub

    Private Sub OnPadKey(key As String)
        _buffer.Press(key)
        ShowMessage("", False)
        ShowValue()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Dim k As String = PadKeys.FromKeyboard(keyData)
        If k.Length > 0 Then
            OnPadKey(k)
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        Dim v As Decimal = _buffer.Value
        If v <= 0D Then
            ShowMessage("Enter a number more than zero.", True)
            Return
        End If
        If v > _max Then
            ShowMessage("The most allowed is " & _max.ToString("0.##", CultureInfo.InvariantCulture) & ".", True)
            Return
        End If
        Value = v
        DialogResult = DialogResult.OK
    End Sub
End Class

''' <summary>
''' At the POS: scan the customer's loyalty card (or search by name / phone), see the points,
''' then choose to add points to this sale or to pay with points.
''' OK = a customer was chosen; No = the customer has no card; Cancel = back to the cart.
''' </summary>
Public Class LoyaltyDialog
    Inherits ModalDialog

    Private ReadOnly _total As Decimal
    Private ReadOnly _totalNoDiscount As Decimal
    Private ReadOnly _hasDiscount As Boolean
    Private ReadOnly _scan As New InputField("Scan the card, or type a name / mobile number", InputKind.Text, 500, False)
    Private ReadOnly _list As New ListBox()
    Private ReadOnly _found As New List(Of DataRow)()
    Private ReadOnly _info As New Panel()
    Private ReadOnly _lblName As New Label()
    Private ReadOnly _lblContact As New Label()
    Private ReadOnly _lblPoints As New Label()
    Private ReadOnly _lblWorth As New Label()
    Private ReadOnly _btnAdd As New MaroonButton()
    Private ReadOnly _btnUse As New MaroonButton()
    Private ReadOnly _hint As New Label()
    Private ReadOnly _btnNoCard As New MaroonButton()
    Private ReadOnly _btnBack As New MaroonButton()
    Private ReadOnly _timer As New Timer()

    Private _spend As Decimal = 200D
    Private _peso As Decimal = 1D
    Private _chosen As LoyaltyChoice

    Public ReadOnly Property Choice As LoyaltyChoice
        Get
            Return _chosen
        End Get
    End Property

    Public Sub New(total As Decimal, totalWithoutDiscounts As Decimal, hasDiscountOrSpecial As Boolean)
        MyBase.New("Loyalty card", "Does the customer have a loyalty card? Scan it, or search for the customer.", 580, 640)
        _total = total
        _totalNoDiscount = totalWithoutDiscounts
        _hasDiscount = hasDiscountOrSpecial
        Try
            _spend = Loyalty.SpendPerPoint()
            _peso = Loyalty.PesoPerPoint()
        Catch
            ' defaults
        End Try

        _scan.Location = New Point(Theme.S(40), Theme.S(104))
        _scan.SwallowEnter = True
        AddHandler _scan.EnterPressed, AddressOf OnScanEnter
        AddHandler _scan.ValueChanged, AddressOf OnScanTyped

        _list.Location = New Point(Theme.S(40), Theme.S(184))
        _list.Size = New Size(Theme.S(500), Theme.S(96))
        _list.Font = Theme.MakeFont("Segoe UI", 11.0F)
        _list.BorderStyle = BorderStyle.FixedSingle
        _list.Visible = False
        AddHandler _list.SelectedIndexChanged, AddressOf OnListChanged

        _info.Location = New Point(Theme.S(40), Theme.S(292))
        _info.Size = New Size(Theme.S(500), Theme.S(124))
        _info.BackColor = Color.White
        _info.Visible = False
        AddHandler _info.Paint, AddressOf OnInfoPaint

        _lblName.AutoSize = False
        _lblName.Location = New Point(Theme.S(16), Theme.S(10))
        _lblName.Size = New Size(Theme.S(468), Theme.S(28))
        _lblName.Font = Theme.MakeFont("Segoe UI Semibold", 14.0F)
        _lblName.ForeColor = Theme.TextDark

        _lblContact.AutoSize = False
        _lblContact.Location = New Point(Theme.S(16), Theme.S(40))
        _lblContact.Size = New Size(Theme.S(468), Theme.S(20))
        _lblContact.Font = Theme.FontSmall
        _lblContact.ForeColor = Theme.TextMuted

        _lblPoints.AutoSize = False
        _lblPoints.Location = New Point(Theme.S(16), Theme.S(66))
        _lblPoints.Size = New Size(Theme.S(270), Theme.S(48))
        _lblPoints.Font = Theme.MakeFont("Segoe UI Semibold", 24.0F)
        _lblPoints.ForeColor = Theme.Maroon
        _lblPoints.TextAlign = ContentAlignment.MiddleLeft

        _lblWorth.AutoSize = False
        _lblWorth.Location = New Point(Theme.S(290), Theme.S(66))
        _lblWorth.Size = New Size(Theme.S(194), Theme.S(48))
        _lblWorth.Font = Theme.FontBase
        _lblWorth.ForeColor = Theme.TextMuted
        _lblWorth.TextAlign = ContentAlignment.MiddleRight

        _info.Controls.Add(_lblName)
        _info.Controls.Add(_lblContact)
        _info.Controls.Add(_lblPoints)
        _info.Controls.Add(_lblWorth)

        _btnAdd.Size = New Size(Theme.S(245), Theme.S(60))
        _btnAdd.Location = New Point(Theme.S(40), Theme.S(428))
        _btnAdd.Font = Theme.MakeFont("Segoe UI Semibold", 12.0F)
        _btnAdd.Visible = False
        AddHandler _btnAdd.Click, AddressOf OnAddPoints

        _btnUse.Text = "Use points"
        _btnUse.MakeSecondary()
        _btnUse.Size = New Size(Theme.S(245), Theme.S(60))
        _btnUse.Location = New Point(Theme.S(295), Theme.S(428))
        _btnUse.Font = Theme.MakeFont("Segoe UI Semibold", 12.0F)
        _btnUse.Visible = False
        AddHandler _btnUse.Click, AddressOf OnUsePoints

        _hint.AutoSize = False
        _hint.Location = New Point(Theme.S(40), Theme.S(494))
        _hint.Size = New Size(Theme.S(500), Theme.S(36))
        _hint.Font = Theme.FontSmall
        _hint.ForeColor = Theme.TextMuted

        Message.Location = New Point(Theme.S(40), Theme.S(530))
        Message.Size = New Size(Theme.S(500), Theme.S(24))

        _btnNoCard.Text = "No loyalty card"
        _btnNoCard.MakeSecondary()
        _btnNoCard.Size = New Size(Theme.S(245), Theme.S(50))
        _btnNoCard.Location = New Point(Theme.S(40), Theme.S(562))
        AddHandler _btnNoCard.Click, AddressOf OnNoCard

        _btnBack.Text = "Back to the cart"
        _btnBack.MakeSecondary()
        _btnBack.Size = New Size(Theme.S(245), Theme.S(50))
        _btnBack.Location = New Point(Theme.S(295), Theme.S(562))
        AddHandler _btnBack.Click, AddressOf OnBack

        Controls.Add(_scan)
        Controls.Add(_list)
        Controls.Add(_info)
        Controls.Add(_btnAdd)
        Controls.Add(_btnUse)
        Controls.Add(_hint)
        Controls.Add(_btnNoCard)
        Controls.Add(_btnBack)
        ClientSize = New Size(Theme.S(580), Theme.S(562) + Theme.S(50) + Theme.S(24))
        CancelButton = _btnBack

        _timer.Interval = 350
        AddHandler _timer.Tick, AddressOf OnSearchTimer
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _scan.FocusInput()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        _timer.Stop()
        _timer.Dispose()
        MyBase.OnFormClosed(e)
    End Sub

    Private Sub OnInfoPaint(sender As Object, e As PaintEventArgs)
        Using pen As New Pen(Theme.BorderColor, 1.0F)
            e.Graphics.DrawRectangle(pen, 0, 0, _info.Width - 1, _info.Height - 1)
        End Using
    End Sub

    ' ---- finding the customer
    Private Sub OnScanTyped()
        _timer.Stop()
        _timer.Start()
    End Sub

    Private Sub OnSearchTimer(sender As Object, e As EventArgs)
        _timer.Stop()
        DoSearch(False)
    End Sub

    Private Sub OnScanEnter()
        _timer.Stop()
        DoSearch(True)
    End Sub

    Private Sub DoSearch(fromEnter As Boolean)
        Dim text As String = _scan.Value.Trim()
        ShowMessage("", False)
        If text.Length = 0 Then
            ClearFound()
            Return
        End If

        Try
            ' a scanned card matches exactly
            Dim exact As DataRow = CustomerRepository.FindByCard(text)
            If exact IsNot Nothing Then
                _found.Clear()
                _found.Add(exact)
                _list.Visible = False
                SelectCustomer(exact)
                Return
            End If

            Dim dt As DataTable = CustomerRepository.Search(text)
            _found.Clear()
            _list.Items.Clear()
            For Each r As DataRow In dt.Rows
                _found.Add(r)
                _list.Items.Add(r("name").ToString() & "   |   " & r("contact_number").ToString() & "   |   " &
                                Loyalty.Fmt(Convert.ToDecimal(r("points_balance"))) & " pts")
            Next

            If _found.Count = 0 Then
                HideInfo()
                _list.Visible = False
                If fromEnter OrElse text.Length >= 3 Then ShowMessage("No customer found for '" & text & "'.", True)
            ElseIf _found.Count = 1 Then
                _list.Visible = False
                SelectCustomer(_found(0))
            Else
                HideInfo()
                _list.Visible = True
            End If
        Catch ex As Exception
            ShowMessage("Could not search: " & ex.Message, True)
        End Try
    End Sub

    Private Sub ClearFound()
        _found.Clear()
        _list.Items.Clear()
        _list.Visible = False
        HideInfo()
    End Sub

    Private Sub OnListChanged(sender As Object, e As EventArgs)
        Dim i As Integer = _list.SelectedIndex
        If i >= 0 AndAlso i < _found.Count Then SelectCustomer(_found(i))
    End Sub

    Private Sub HideInfo()
        _info.Visible = False
        _btnAdd.Visible = False
        _btnUse.Visible = False
        _hint.Text = ""
        _chosen = Nothing
    End Sub

    Private Sub SelectCustomer(r As DataRow)
        Dim c As New LoyaltyChoice()
        c.CustomerId = Convert.ToInt32(r("id"))
        c.Name = r("name").ToString()
        c.Phone = r("contact_number").ToString()
        c.Card = r("card_number").ToString()
        c.Balance = Convert.ToDecimal(r("points_balance"))
        _chosen = c

        _lblName.Text = c.Name
        _lblContact.Text = c.Phone & "   |   Card " & c.Card
        _lblPoints.Text = Loyalty.Fmt(c.Balance) & " pts"
        _lblWorth.Text = "worth " & Currency.Format(PosMath.Round2(c.Balance * _peso))
        _info.Visible = True

        Dim earn As Decimal = Loyalty.PointsFor(_total, _spend)
        _btnAdd.Text = "Add points  (+" & Loyalty.Fmt(earn) & ")"
        _btnAdd.Visible = True
        _btnUse.Visible = True
        _btnUse.Enabled = (c.Balance >= 0.01D)
        _hint.Text = "Adding points: every " & Currency.Format(_spend) & " earns 1 point." & Environment.NewLine &
                     "Using points: the discount and the special price are turned off." &
                     If(_hasDiscount, " (the ones on this sale will be removed)", "")
    End Sub

    ' ---- the choices
    Private Sub OnAddPoints(sender As Object, e As EventArgs)
        If _chosen Is Nothing Then Return
        _chosen.Mode = "earn"
        _chosen.PointsToUse = 0D
        DialogResult = DialogResult.OK
    End Sub

    Private Sub OnUsePoints(sender As Object, e As EventArgs)
        If _chosen Is Nothing Then Return
        Dim maxPoints As Decimal = Math.Min(_chosen.Balance, PosMath.Round2(_totalNoDiscount / _peso))
        If maxPoints < 0.01D Then
            ShowMessage("There is nothing to pay with points.", True)
            Return
        End If
        Using dlg As New NumberPadDialog("Use points", _chosen.Name & " has " & Loyalty.Fmt(_chosen.Balance) & " points (1 point = " & Currency.Format(_peso) & ")",
                                         maxPoints, maxPoints, True, "Use these points", " pts")
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            _chosen.Mode = "use"
            _chosen.PointsToUse = dlg.Value
        End Using
        DialogResult = DialogResult.OK
    End Sub

    Private Sub OnNoCard(sender As Object, e As EventArgs)
        DialogResult = DialogResult.No
    End Sub

    Private Sub OnBack(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub
End Class
