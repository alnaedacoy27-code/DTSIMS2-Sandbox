Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Type a quantity on the number pad (or the keyboard).</summary>
Public Class QtyDialog
    Inherits ModalDialog

    Private ReadOnly _max As Integer
    Private ReadOnly _buffer As New AmountBuffer()
    Private ReadOnly _display As New Label()
    Private ReadOnly _pad As New NumpadPad()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    Public Property Quantity As Integer

    Public Sub New(itemLabel As String, currentQty As Integer, maxQty As Integer)
        MyBase.New("Quantity", itemLabel & "  -  " & maxQty.ToString() & " in stock", 440, 660)
        _max = maxQty
        _buffer.AllowDecimal = False
        _buffer.MaxIntegerDigits = 6
        _buffer.SetValue(currentQty)

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

        _save.Text = "Set quantity"
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
        Refresh2()
    End Sub

    Private Sub Refresh2()
        _display.Text = If(_buffer.Text.Length = 0, "0", _buffer.Text)
    End Sub

    Private Sub OnPadKey(key As String)
        _buffer.Press(key)
        ShowMessage("", False)
        Refresh2()
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
        Dim q As Integer = CInt(_buffer.Value)
        If q <= 0 Then
            ShowMessage("Enter a quantity of 1 or more.", True)
            Return
        End If
        If q > _max Then
            ShowMessage("Only " & _max.ToString() & " in stock.", True)
            Return
        End If
        Quantity = q
        DialogResult = DialogResult.OK
    End Sub
End Class

''' <summary>
''' A special price for a close customer: type the NEW TOTAL, or how much to take OFF.
''' (A cashier then needs an Admin's approval; an Admin / the Superadmin can apply it directly.)
''' </summary>
Public Class SpecialPriceDialog
    Inherits ModalDialog

    Private ReadOnly _current As Decimal
    Private ReadOnly _cost As Decimal
    Private ReadOnly _buffer As New AmountBuffer()
    Private ReadOnly _modeTotal As New MaroonButton()
    Private ReadOnly _modeOff As New MaroonButton()
    Private ReadOnly _caption As New Label()
    Private ReadOnly _display As New Label()
    Private ReadOnly _preview As New Label()
    Private ReadOnly _pad As New NumpadPad()
    Private ReadOnly _apply As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()
    Private _newTotalMode As Boolean = True

    ''' <summary>How much is taken off the current total.</summary>
    Public Property SpecialAmount As Decimal

    Public Sub New(currentTotal As Decimal, costTotal As Decimal)
        MyBase.New("Special price", "For a close customer. The total now is " & Currency.Format(currentTotal) & ".", 480, 760)
        _current = currentTotal
        _cost = costTotal

        _modeTotal.Text = "New total"
        _modeTotal.Size = New Size(Theme.S(170), Theme.S(44))
        _modeTotal.Location = New Point(Theme.S(40), Theme.S(104))
        AddHandler _modeTotal.Click, AddressOf OnModeTotal

        _modeOff.Text = "Amount off"
        _modeOff.Size = New Size(Theme.S(170), Theme.S(44))
        _modeOff.Location = New Point(Theme.S(220), Theme.S(104))
        AddHandler _modeOff.Click, AddressOf OnModeOff

        _caption.AutoSize = False
        _caption.Location = New Point(Theme.S(40), Theme.S(160))
        _caption.Size = New Size(Theme.S(400), Theme.S(20))
        _caption.Font = Theme.FontCaption
        _caption.ForeColor = Theme.TextMuted

        _display.AutoSize = False
        _display.Location = New Point(Theme.S(40), Theme.S(184))
        _display.Size = New Size(Theme.S(400), Theme.S(68))
        _display.TextAlign = ContentAlignment.MiddleRight
        _display.Font = Theme.MakeFont("Segoe UI Semibold", 28.0F)
        _display.ForeColor = Theme.Maroon
        _display.BackColor = Theme.MaroonTint

        _preview.AutoSize = False
        _preview.Location = New Point(Theme.S(40), Theme.S(260))
        _preview.Size = New Size(Theme.S(400), Theme.S(44))
        _preview.Font = Theme.FontBase
        _preview.ForeColor = Theme.TextMuted

        _pad.Location = New Point(Theme.S(40), Theme.S(312))
        _pad.Size = New Size(Theme.S(400), Theme.S(320))
        AddHandler _pad.KeyPressed, AddressOf OnPadKey

        Message.Location = New Point(Theme.S(40), Theme.S(640))
        Message.Size = New Size(Theme.S(400), Theme.S(26))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(190), Theme.S(54))
        _cancel.Location = New Point(Theme.S(40), Theme.S(676))
        AddHandler _cancel.Click, AddressOf OnCancel

        _apply.Text = "Apply"
        _apply.Size = New Size(Theme.S(190), Theme.S(54))
        _apply.Location = New Point(Theme.S(250), Theme.S(676))
        AddHandler _apply.Click, AddressOf OnApply

        Controls.Add(_modeTotal)
        Controls.Add(_modeOff)
        Controls.Add(_caption)
        Controls.Add(_display)
        Controls.Add(_preview)
        Controls.Add(_pad)
        Controls.Add(_cancel)
        Controls.Add(_apply)
        ClientSize = New Size(Theme.S(480), Theme.S(676) + Theme.S(54) + Theme.S(24))
        AcceptButton = _apply
        CancelButton = _cancel
        UpdateModeUi()
    End Sub

    Private Sub OnModeTotal(sender As Object, e As EventArgs)
        _newTotalMode = True
        _buffer.Press("CLEAR")
        UpdateModeUi()
    End Sub

    Private Sub OnModeOff(sender As Object, e As EventArgs)
        _newTotalMode = False
        _buffer.Press("CLEAR")
        UpdateModeUi()
    End Sub

    Private Sub UpdateModeUi()
        If _newTotalMode Then
            _modeTotal.BackColor = Theme.Maroon
            _modeTotal.ForeColor = Color.White
            _modeOff.BackColor = Color.White
            _modeOff.ForeColor = Theme.Maroon
            _caption.Text = "THE CUSTOMER PAYS (NEW TOTAL)"
        Else
            _modeOff.BackColor = Theme.Maroon
            _modeOff.ForeColor = Color.White
            _modeTotal.BackColor = Color.White
            _modeTotal.ForeColor = Theme.Maroon
            _caption.Text = "AMOUNT TAKEN OFF THE TOTAL"
        End If
        UpdateDisplay()
    End Sub

    ' how much comes off, given what was typed
    Private Function AmountOff() As Decimal
        If _buffer.Text.Length = 0 Then Return 0D
        Return If(_newTotalMode, _current - _buffer.Value, _buffer.Value)
    End Function

    Private Sub UpdateDisplay()
        _display.Text = If(_buffer.Text.Length = 0, Currency.Peso & "0", Currency.Peso & _buffer.Text)

        Dim off As Decimal = AmountOff()
        If _buffer.Text.Length = 0 Then
            _preview.Text = ""
            Return
        End If

        Dim newTotal As Decimal = _current - off
        If off <= 0D Then
            _preview.ForeColor = Theme.AlertRed
            _preview.Text = "That is not lower than the current total."
        ElseIf newTotal <= 0D Then
            _preview.ForeColor = Theme.AlertRed
            _preview.Text = "That takes off more than the whole total."
        Else
            Dim pct As Decimal = If(_current > 0D, off / _current * 100D, 0D)
            _preview.ForeColor = Theme.TextDark
            _preview.Text = Currency.Format(off) & " off  (" & pct.ToString("0.0", Globalization.CultureInfo.InvariantCulture) & "%)" &
                            Environment.NewLine & "New total " & Currency.Format(newTotal)
            If newTotal < _cost Then
                _preview.ForeColor = Theme.AlertRed
                _preview.Text &= "   - below your cost (" & Currency.Format(_cost) & ")"
            End If
        End If
    End Sub

    Private Sub OnPadKey(key As String)
        _buffer.Press(key)
        ShowMessage("", False)
        UpdateDisplay()
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

    Private Sub OnApply(sender As Object, e As EventArgs)
        Dim off As Decimal = AmountOff()
        If _buffer.Text.Length = 0 Then
            ShowMessage("Type the new total or the amount to take off.", True)
            Return
        End If
        If off < 0.01D Then
            ShowMessage("The special price must be lower than the current total.", True)
            Return
        End If
        If _current - off < 0.01D Then
            ShowMessage("The new total must be more than zero.", True)
            Return
        End If
        SpecialAmount = PosMath.Round2(off)
        DialogResult = DialogResult.OK
    End Sub
End Class

''' <summary>A cashier cannot give a special price alone: an Admin or the Superadmin types their own login.</summary>
Public Class ApprovalDialog
    Inherits ModalDialog

    Private ReadOnly _username As New InputField("Supervisor username", InputKind.Username, 400, True)
    Private ReadOnly _password As New InputField("Supervisor password", False, 400)
    Private ReadOnly _approve As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    Public Property ApproverId As Integer
    Public Property ApproverName As String = ""

    Public Sub New(summary As String)
        MyBase.New("Supervisor approval", "An Admin or the Superadmin must approve this special price.", 480, 460)

        Dim info As New Label()
        info.AutoSize = False
        info.Location = New Point(Theme.S(40), Theme.S(104))
        info.Size = New Size(Theme.S(400), Theme.S(40))
        info.Font = Theme.FontBase
        info.ForeColor = Theme.TextDark
        info.Text = summary

        _username.Location = New Point(Theme.S(40), Theme.S(156))
        _password.Location = New Point(Theme.S(40), Theme.S(230))
        _password.IsPassword = True
        _password.MaxLength = 64

        Message.Location = New Point(Theme.S(40), Theme.S(308))
        Message.Size = New Size(Theme.S(400), Theme.S(40))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(190), Theme.S(52))
        _cancel.Location = New Point(Theme.S(40), Theme.S(358))
        AddHandler _cancel.Click, AddressOf OnCancel

        _approve.Text = "Approve"
        _approve.Size = New Size(Theme.S(190), Theme.S(52))
        _approve.Location = New Point(Theme.S(250), Theme.S(358))
        AddHandler _approve.Click, AddressOf OnApprove

        Controls.Add(info)
        Controls.Add(_username)
        Controls.Add(_password)
        Controls.Add(_cancel)
        Controls.Add(_approve)
        ClientSize = New Size(Theme.S(480), Theme.S(358) + Theme.S(52) + Theme.S(26))
        AcceptButton = _approve
        CancelButton = _cancel
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _username.FocusInput()
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnApprove(sender As Object, e As EventArgs)
        ShowMessage("", False)
        If _username.Value.Trim().Length = 0 OrElse _password.Value.Length = 0 Then
            ShowMessage("Enter the supervisor's username and password.", True)
            Return
        End If

        _approve.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim id As Integer
            Dim nm As String = ""
            Dim msg As String = ""
            If SupervisorAuth.Verify(_username.Value, _password.Value, id, nm, msg) Then
                ApproverId = id
                ApproverName = nm
                DialogResult = DialogResult.OK
            Else
                ShowMessage(msg, True)
                _password.Value = ""
                _password.FocusInput()
            End If
        Catch ex As Exception
            ShowMessage("Could not check the login: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            If DialogResult <> DialogResult.OK Then _approve.Enabled = True
        End Try
    End Sub
End Class
