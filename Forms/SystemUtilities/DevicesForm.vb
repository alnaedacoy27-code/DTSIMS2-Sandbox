Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>A rounded card with a title and a status pill (green / red / amber / grey).</summary>
Friend Class DeviceCard
    Inherits Panel

    Private ReadOnly _title As New Label()
    Private ReadOnly _pill As New PillLabel()

    Public Sub New(title As String)
        DoubleBuffered = True
        BackColor = Color.White
        _title.AutoSize = True
        _title.Font = Theme.FontHeading
        _title.ForeColor = Theme.Maroon
        _title.Text = title
        _title.Location = New Point(Theme.S(20), Theme.S(16))
        Controls.Add(_title)
        Controls.Add(_pill)
        SetStatus("Not checked", "idle")
    End Sub

    Public Sub SetStatus(text As String, kind As String)
        Dim back As Color = Color.FromArgb(236, 233, 233)
        Dim fore As Color = Theme.TextMuted
        Select Case kind
            Case "ok"
                back = Color.FromArgb(220, 245, 228)
                fore = Color.FromArgb(22, 130, 60)
            Case "bad"
                back = Color.FromArgb(253, 224, 224)
                fore = Color.FromArgb(176, 32, 32)
            Case "warn"
                back = Color.FromArgb(255, 240, 214)
                fore = Color.FromArgb(150, 95, 0)
        End Select
        _pill.SetPill(text, back, fore)
        PlacePill()
    End Sub

    Private Sub PlacePill()
        _pill.Location = New Point(Math.Max(Theme.S(200), Width - Theme.S(20) - _pill.Width), Theme.S(18))
    End Sub

    Protected Overrides Sub OnResize(eventargs As EventArgs)
        MyBase.OnResize(eventargs)
        PlacePill()
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using path As GraphicsPath = Theme.RoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), Theme.S(12))
            Using pen As New Pen(Theme.BorderColor, 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub
End Class

''' <summary>
''' Utilities > Devices: see whether the SMS modem, the receipt printer and the barcode scanner are
''' connected and working, and test each one.
''' </summary>
Public Class DevicesForm
    Inherits Form

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()
    Private ReadOnly _body As New FlowLayoutPanel()

    ' ---- SMS modem
    Private ReadOnly _sms As New DeviceCard("SMS modem")
    Private ReadOnly _smsInfo As New Label()
    Private ReadOnly _smsReply As New Label()
    Private ReadOnly _btnCheck As New MaroonButton()
    Private ReadOnly _btnBalance As New MaroonButton()
    Private ReadOnly _btnTest As New MaroonButton()
    Private ReadOnly _cfgHead As New Label()
    Private ReadOnly _lnkGlobe As New LinkLabel()
    Private ReadOnly _lnkSmart As New LinkLabel()
    Private ReadOnly _cfgFlow As New FlowLayoutPanel()
    Private ReadOnly _method As New ChoiceField("Check the load by", 230)
    Private ReadOnly _number As New InputField("Send to number", InputKind.Text, 120, False)
    Private ReadOnly _keyword As New InputField("Word to send", InputKind.Text, 120, False)
    Private ReadOnly _ussd As New InputField("USSD code", InputKind.Text, 140, False)
    Private ReadOnly _cost As New InputField("Price of 1 text (PHP)", InputKind.Money, 150, False)
    Private ReadOnly _btnSave As New MaroonButton()

    ' ---- receipt printer
    Private ReadOnly _prn As New DeviceCard("Receipt printer")
    Private ReadOnly _printer As New ChoiceField("Printer", 420)
    Private ReadOnly _prnInfo As New Label()
    Private ReadOnly _btnRefresh As New MaroonButton()
    Private ReadOnly _btnPrintTest As New MaroonButton()
    Private ReadOnly _btnUsePrinter As New MaroonButton()
    Private ReadOnly _printers As New List(Of PrinterInfo)()

    ' ---- barcode scanner
    Private ReadOnly _scn As New DeviceCard("Barcode scanner")
    Private ReadOnly _scanHelp As New Label()
    Private ReadOnly _scanBox As New TextBox()
    Private ReadOnly _scanLog As New Label()
    Private ReadOnly _scanHints As New Label()
    Private ReadOnly _btnClearScan As New MaroonButton()
    Private ReadOnly _scanWatch As New System.Diagnostics.Stopwatch()
    Private ReadOnly _scanLines As New List(Of String)()
    Private _scanChars As Integer

    Private _status As ModemStatus
    Private _balanceText As String = "not checked yet"
    Private _balanceAmount As Decimal = -1D

    Public Sub New()
        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        BackColor = Color.White
        Font = Theme.FontBase

        _header.Dock = DockStyle.Top
        _header.Height = Theme.S(100)
        _header.BackColor = Color.White
        _title.AutoSize = True
        _title.Font = Theme.FontTitle
        _title.ForeColor = Theme.Maroon
        _title.Text = "Devices"
        _title.Location = New Point(Theme.S(32), Theme.S(16))
        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "Check that the SMS modem, the receipt printer and the barcode scanner are connected and working"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))
        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)

        _body.Dock = DockStyle.Fill
        _body.FlowDirection = FlowDirection.TopDown
        _body.WrapContents = False
        _body.AutoScroll = True
        _body.Padding = New Padding(Theme.S(32), 0, Theme.S(16), Theme.S(24))
        _body.BackColor = Color.White
        AddHandler _body.SizeChanged, AddressOf OnBodySize

        BuildSms()
        BuildPrinter()
        BuildScanner()

        For Each c As Control In New Control() {_sms, _prn, _scn}
            c.Margin = New Padding(0, 0, 0, Theme.S(16))
            _body.Controls.Add(c)
        Next

        Controls.Add(_body)
        Controls.Add(_header)
    End Sub

    Private Sub OnBodySize(sender As Object, e As EventArgs)
        Dim w As Integer = Math.Max(Theme.S(640), _body.ClientSize.Width - Theme.S(56))
        For Each c As Control In New Control() {_sms, _prn, _scn}
            c.Width = w
        Next
        _smsInfo.Width = w - Theme.S(40)
        _smsReply.Width = w - Theme.S(40)
        _cfgFlow.Width = w - Theme.S(40)
        _prnInfo.Width = w - Theme.S(40)
        _scanBox.Width = Math.Min(Theme.S(520), w - Theme.S(40))
        _scanLog.Width = w - Theme.S(40)
        _scanHints.Width = w - Theme.S(40)
        _scanHelp.Width = w - Theme.S(40)
    End Sub

    Private Function MakeText(f As Font, color As Color, height As Integer) As Label
        Dim l As New Label()
        l.AutoSize = False
        l.Font = f
        l.ForeColor = color
        l.Height = height
        Return l
    End Function

    ' ------------------------------------------------------------------ SMS modem
    Private Sub BuildSms()
        _sms.Height = Theme.S(566)

        _smsInfo.AutoSize = False
        _smsInfo.Font = New Font("Consolas", 10.5F)
        _smsInfo.ForeColor = Theme.TextDark
        _smsInfo.Location = New Point(Theme.S(20), Theme.S(56))
        _smsInfo.Size = New Size(Theme.S(800), Theme.S(150))
        _smsInfo.Text = "Press 'Check modem' to see if the modem is plugged in and working."

        _btnCheck.Text = "Check modem"
        _btnCheck.Size = New Size(Theme.S(160), Theme.S(44))
        _btnCheck.Location = New Point(Theme.S(20), Theme.S(214))
        AddHandler _btnCheck.Click, AddressOf OnCheckModem

        _btnBalance.Text = "Check load balance"
        _btnBalance.MakeSecondary()
        _btnBalance.Size = New Size(Theme.S(190), Theme.S(44))
        _btnBalance.Location = New Point(Theme.S(192), Theme.S(214))
        AddHandler _btnBalance.Click, AddressOf OnCheckBalance

        _btnTest.Text = "Send test message"
        _btnTest.MakeSecondary()
        _btnTest.Size = New Size(Theme.S(190), Theme.S(44))
        _btnTest.Location = New Point(Theme.S(394), Theme.S(214))
        AddHandler _btnTest.Click, AddressOf OnSendTest

        _smsReply.AutoSize = False
        _smsReply.Font = Theme.FontSmall
        _smsReply.ForeColor = Theme.TextMuted
        _smsReply.Location = New Point(Theme.S(20), Theme.S(266))
        _smsReply.Size = New Size(Theme.S(800), Theme.S(46))

        _cfgHead.AutoSize = True
        _cfgHead.Font = Theme.FontCaption
        _cfgHead.ForeColor = Theme.TextMuted
        _cfgHead.Text = "HOW TO CHECK THE LOAD  (ask the SIM provider if unsure)"
        _cfgHead.Location = New Point(Theme.S(20), Theme.S(322))

        For Each lk As LinkLabel In New LinkLabel() {_lnkGlobe, _lnkSmart}
            lk.AutoSize = True
            lk.Font = Theme.FontSmall
            lk.LinkColor = Theme.Maroon
            lk.ActiveLinkColor = Theme.MaroonDark
            lk.VisitedLinkColor = Theme.Maroon
            lk.LinkBehavior = LinkBehavior.HoverUnderline
            lk.Cursor = Cursors.Hand
        Next
        _lnkGlobe.Text = "Use Globe / TM  (text BAL to 222)"
        _lnkGlobe.Location = New Point(Theme.S(20), Theme.S(346))
        AddHandler _lnkGlobe.LinkClicked, AddressOf OnPresetGlobe
        _lnkSmart.Text = "Use Smart / TNT / Sun  (text 1515 to 214)"
        _lnkSmart.Location = New Point(Theme.S(330), Theme.S(346))
        AddHandler _lnkSmart.LinkClicked, AddressOf OnPresetSmart

        _method.SetChoices(New String() {"Text a word to a number", "Dial a USSD code"})
        _method.Value = "Text a word to a number"
        _cost.MaxLength = 7
        _cost.Value = "1"

        _cfgFlow.Location = New Point(Theme.S(20), Theme.S(374))
        _cfgFlow.Size = New Size(Theme.S(800), Theme.S(170))
        _cfgFlow.WrapContents = True
        _cfgFlow.FlowDirection = FlowDirection.LeftToRight
        _cfgFlow.BackColor = Color.White
        For Each c As Control In New Control() {_method, _number, _keyword, _ussd, _cost}
            c.Margin = New Padding(0, 0, Theme.S(14), Theme.S(4))
            _cfgFlow.Controls.Add(c)
        Next
        _btnSave.Text = "Save"
        _btnSave.Size = New Size(Theme.S(110), Theme.S(44))
        _btnSave.Margin = New Padding(0, Theme.S(22), 0, 0)
        AddHandler _btnSave.Click, AddressOf OnSaveConfig
        _cfgFlow.Controls.Add(_btnSave)

        _sms.Controls.Add(_smsInfo)
        _sms.Controls.Add(_btnCheck)
        _sms.Controls.Add(_btnBalance)
        _sms.Controls.Add(_btnTest)
        _sms.Controls.Add(_smsReply)
        _sms.Controls.Add(_cfgHead)
        _sms.Controls.Add(_lnkGlobe)
        _sms.Controls.Add(_lnkSmart)
        _sms.Controls.Add(_cfgFlow)
    End Sub

    Private Sub OnPresetGlobe(sender As Object, e As LinkLabelLinkClickedEventArgs)
        _method.Value = "Text a word to a number"
        _number.Value = "222"
        _keyword.Value = "BAL"
    End Sub

    Private Sub OnPresetSmart(sender As Object, e As LinkLabelLinkClickedEventArgs)
        _method.Value = "Text a word to a number"
        _number.Value = "214"
        _keyword.Value = "1515"
    End Sub

    Private Sub LoadConfig()
        Try
            Dim c As BalanceConfig = SmsService.GetBalanceConfig()
            _method.Value = If(c.Method = "ussd", "Dial a USSD code", "Text a word to a number")
            _number.Value = c.Number
            _keyword.Value = c.Keyword
            _ussd.Value = c.UssdCode
            _cost.Value = c.CostPerText.ToString("0.##", CultureInfo.InvariantCulture)
        Catch
            ' defaults stay
        End Try
    End Sub

    Private Function ConfigFromFields() As BalanceConfig
        Dim c As New BalanceConfig()
        c.Method = If(_method.Value = "Dial a USSD code", "ussd", "text")
        c.Number = _number.Value.Trim()
        c.Keyword = _keyword.Value.Trim()
        c.UssdCode = _ussd.Value.Trim()
        Dim v As Decimal
        If Decimal.TryParse(_cost.Value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, v) Then c.CostPerText = v
        Return c
    End Function

    Private Sub OnSaveConfig(sender As Object, e As EventArgs)
        Dim c As BalanceConfig = ConfigFromFields()
        If c.Method = "text" AndAlso (c.Number.Length = 0 OrElse c.Keyword.Length = 0) Then
            _smsReply.ForeColor = Theme.AlertRed
            _smsReply.Text = "Type the number and the word to send (for example BAL to 222)."
            Return
        End If
        If c.Method = "ussd" AndAlso c.UssdCode.Length = 0 Then
            _smsReply.ForeColor = Theme.AlertRed
            _smsReply.Text = "Type the USSD code (for example *143#)."
            Return
        End If
        Try
            SmsService.SaveBalanceConfig(c)
            AuditLog.Write("EDIT", "Devices", "Changed how the SMS modem's load balance is checked")
            _smsReply.ForeColor = Color.FromArgb(22, 130, 60)
            _smsReply.Text = "Saved."
            RenderSmsInfo()
        Catch ex As Exception
            _smsReply.ForeColor = Theme.AlertRed
            _smsReply.Text = "Could not save: " & ex.Message
        End Try
    End Sub

    ' ---- running the slow modem jobs without freezing the screen
    Private Sub RunAsync(Of T)(work As Func(Of T), done As Action(Of T))
        Dim ctx As System.Threading.SynchronizationContext = System.Threading.SynchronizationContext.Current
        System.Threading.Tasks.Task.Run(
            Sub()
                Dim result As T = Nothing
                Try
                    result = work()
                Catch
                    result = Nothing
                End Try
                ctx.Post(Sub(state)
                             If Not IsDisposed Then done(result)
                         End Sub, Nothing)
            End Sub)
    End Sub

    Private Sub SetSmsBusy(busy As Boolean, text As String)
        _btnCheck.Enabled = Not busy
        _btnBalance.Enabled = Not busy
        _btnTest.Enabled = Not busy
        If busy Then _sms.SetStatus(text, "idle")
    End Sub

    Private Sub OnCheckModem(sender As Object, e As EventArgs)
        SetSmsBusy(True, "Checking...")
        _smsInfo.Text = "Talking to the modem (this takes a few seconds)..."
        RunAsync(Of ModemStatus)(Function() SmsService.QueryStatus(), AddressOf OnModemChecked)
    End Sub

    Private Sub OnModemChecked(st As ModemStatus)
        SetSmsBusy(False, "")
        If st Is Nothing Then
            _sms.SetStatus("Error", "bad")
            _smsInfo.Text = "Could not check the modem."
            Return
        End If
        _status = st

        If Not st.PortPresent Then
            _sms.SetStatus("Not found", "bad")
        ElseIf Not st.Responds Then
            _sms.SetStatus("No answer", "bad")
        ElseIf st.Sim <> "Ready" Then
            _sms.SetStatus("SIM problem", "bad")
        ElseIf Not st.Registered Then
            _sms.SetStatus("No network", "bad")
        ElseIf ModemParsers.BarsFromRssi(st.Rssi) = 0 Then
            _sms.SetStatus("No signal", "warn")
        Else
            _sms.SetStatus("Working", "ok")
        End If
        RenderSmsInfo()
    End Sub

    Private Function Bars(rssi As Integer) As String
        Dim n As Integer = ModemParsers.BarsFromRssi(rssi)
        Return "[" & New String("#"c, n) & New String("."c, 5 - n) & "]"
    End Function

    Private Sub RenderSmsInfo()
        Dim lines As New List(Of String)()
        Dim cfg As BalanceConfig = ConfigFromFields()

        If _status Is Nothing Then
            lines.Add("Press 'Check modem' to see if the modem is plugged in and working.")
        ElseIf Not _status.PortPresent OrElse Not _status.Responds Then
            lines.Add("Modem:       " & _status.Problem)
        Else
            lines.Add("Modem:       Found on " & _status.PortName & If(_status.Model.Length > 0, "  (" & _status.Model & ")", ""))
            lines.Add("SIM card:    " & _status.Sim)
            lines.Add("Network:     " & If(_status.NetworkName.Length > 0, _status.NetworkName & "  ", "") & If(_status.Registered, "(connected)", "(NOT connected)"))
            lines.Add("Signal:      " & If(_status.Rssi < 0, "unknown", Bars(_status.Rssi) & "  " & ModemParsers.DbmFromRssi(_status.Rssi).ToString() & " dBm"))
        End If

        lines.Add("Load:        " & _balanceText)
        If _balanceAmount >= 0D Then
            If cfg.CostPerText > 0D Then
                lines.Add("Texts left:  about " & ModemParsers.TextsLeft(_balanceAmount, cfg.CostPerText).ToString("N0") & "  (at " & Currency.Format(cfg.CostPerText) & " per text)")
            Else
                lines.Add("Texts left:  type the price of 1 text below to see this")
            End If
        End If
        lines.Add("Waiting:     " & SmsService.PendingCount().ToString() & " message(s) in the queue")
        _smsInfo.Text = String.Join(Environment.NewLine, lines)
    End Sub

    Private Sub OnCheckBalance(sender As Object, e As EventArgs)
        Dim cfg As BalanceConfig = ConfigFromFields()
        SetSmsBusy(True, "Checking load...")
        _smsReply.ForeColor = Theme.TextMuted
        _smsReply.Text = If(cfg.Method = "ussd", "Dialing " & cfg.UssdCode & " ...", "Sent '" & cfg.Keyword & "' to " & cfg.Number & ". Waiting for the provider's reply (up to 90 seconds)...")
        RunAsync(Of String())(
            Function()
                Dim raw As String = ""
                Dim amount As Decimal
                Dim err As String = ""
                Dim ok As Boolean = SmsService.CheckBalance(cfg, raw, amount, err)
                Return New String() {If(ok, "1", "0"), amount.ToString(CultureInfo.InvariantCulture), raw, err}
            End Function,
            AddressOf OnBalanceChecked)
    End Sub

    Private Sub OnBalanceChecked(r As String())
        SetSmsBusy(False, "")
        If _status IsNot Nothing Then
            OnModemChecked(_status)
        Else
            _sms.SetStatus("Not checked", "idle")
        End If
        If r Is Nothing Then
            _smsReply.ForeColor = Theme.AlertRed
            _smsReply.Text = "Could not check the load."
            Return
        End If

        If r(0) = "1" Then
            _balanceAmount = Decimal.Parse(r(1), CultureInfo.InvariantCulture)
            _balanceText = Currency.Format(_balanceAmount) & "   (checked " & DateTime.Now.ToString("h:mm tt") & ")"
            _smsReply.ForeColor = Color.FromArgb(22, 130, 60)
            _smsReply.Text = "Provider's reply: " & r(2)
        Else
            _balanceAmount = -1D
            _balanceText = "could not be read"
            _smsReply.ForeColor = Theme.AlertRed
            _smsReply.Text = r(3) & If(r(2).Length > 0, Environment.NewLine & "Reply: " & r(2), "")
        End If
        RenderSmsInfo()
    End Sub

    Private Sub OnSendTest(sender As Object, e As EventArgs)
        Using dlg As New SmsTestDialog()
            dlg.ShowDialog(OwnerOf(Me))
        End Using
        RenderSmsInfo()
    End Sub

    ' ------------------------------------------------------------------ receipt printer
    Private Sub BuildPrinter()
        _prn.Height = Theme.S(290)

        _printer.Location = New Point(Theme.S(20), Theme.S(56))
        AddHandler _printer.ValueChanged, AddressOf OnPrinterPicked

        _prnInfo.AutoSize = False
        _prnInfo.Font = New Font("Consolas", 10.5F)
        _prnInfo.ForeColor = Theme.TextDark
        _prnInfo.Location = New Point(Theme.S(20), Theme.S(134))
        _prnInfo.Size = New Size(Theme.S(800), Theme.S(76))

        _btnRefresh.Text = "Refresh list"
        _btnRefresh.MakeSecondary()
        _btnRefresh.Size = New Size(Theme.S(150), Theme.S(44))
        _btnRefresh.Location = New Point(Theme.S(20), Theme.S(226))
        AddHandler _btnRefresh.Click, AddressOf OnRefreshPrinters

        _btnPrintTest.Text = "Print test slip"
        _btnPrintTest.Size = New Size(Theme.S(170), Theme.S(44))
        _btnPrintTest.Location = New Point(Theme.S(182), Theme.S(226))
        AddHandler _btnPrintTest.Click, AddressOf OnPrintTest

        _btnUsePrinter.Text = "Use for receipts and labels"
        _btnUsePrinter.MakeSecondary()
        _btnUsePrinter.Size = New Size(Theme.S(260), Theme.S(44))
        _btnUsePrinter.Location = New Point(Theme.S(364), Theme.S(226))
        AddHandler _btnUsePrinter.Click, AddressOf OnUsePrinter

        _prn.Controls.Add(_printer)
        _prn.Controls.Add(_prnInfo)
        _prn.Controls.Add(_btnRefresh)
        _prn.Controls.Add(_btnPrintTest)
        _prn.Controls.Add(_btnUsePrinter)
    End Sub

    Private Function PickedPrinter() As PrinterInfo
        For Each p As PrinterInfo In _printers
            If PrinterLabel(p) = _printer.Value Then Return p
        Next
        Return Nothing
    End Function

    Private Function PrinterLabel(p As PrinterInfo) As String
        Return p.Name & If(p.IsDefault, "   (Windows default)", "")
    End Function

    Private Sub OnRefreshPrinters(sender As Object, e As EventArgs)
        _btnRefresh.Enabled = False
        _prn.SetStatus("Checking...", "idle")
        _prnInfo.Text = "Asking Windows for the printers..."
        RunAsync(Of List(Of PrinterInfo))(Function() PrinterTools.ListPrinters(), AddressOf OnPrintersListed)
    End Sub

    Private Sub OnPrintersListed(list As List(Of PrinterInfo))
        _btnRefresh.Enabled = True
        _printers.Clear()
        If list Is Nothing OrElse list.Count = 0 Then
            _printer.SetChoices(New String() {"(no printer installed)"})
            _prn.SetStatus("No printer", "bad")
            _prnInfo.Text = "Windows has no printer installed. Plug the receipt printer in and install its driver."
            Return
        End If

        _printers.AddRange(list)
        Dim labels As New List(Of String)()
        Dim chosen As String = LocalDevices.ReceiptPrinter
        Dim selectLabel As String = ""
        For Each p As PrinterInfo In _printers
            labels.Add(PrinterLabel(p))
            If chosen.Length > 0 AndAlso String.Equals(p.Name, chosen, StringComparison.OrdinalIgnoreCase) Then selectLabel = PrinterLabel(p)
            If chosen.Length = 0 AndAlso p.IsDefault Then selectLabel = PrinterLabel(p)
        Next
        _printer.SetChoices(labels.ToArray())
        _printer.Value = If(selectLabel.Length > 0, selectLabel, labels(0))
        ShowPrinter()
    End Sub

    Private Sub OnPrinterPicked()
        ShowPrinter()
    End Sub

    Private Sub ShowPrinter()
        Dim p As PrinterInfo = PickedPrinter()
        If p Is Nothing Then Return
        _prn.SetStatus(If(p.StatusText = "Unknown", "Status unknown", p.StatusText), If(p.IsReady, "ok", If(p.StatusText = "Unknown", "warn", "bad")))

        Dim used As String = LocalDevices.ReceiptPrinter
        _prnInfo.Text = "Status:   " & p.StatusText & Environment.NewLine &
                        "Port:     " & If(p.Port.Length > 0, p.Port, "-") & "     Driver: " & If(p.Driver.Length > 0, p.Driver, "-") & Environment.NewLine &
                        "Receipts and labels on this laptop print on: " & If(used.Length > 0, used, "the Windows default printer")
    End Sub

    Private Sub OnPrintTest(sender As Object, e As EventArgs)
        Dim p As PrinterInfo = PickedPrinter()
        If p Is Nothing Then
            Notify(Me, "Devices", "Press 'Refresh list' and pick a printer first.", MessageBoxIcon.Information)
            Return
        End If
        Dim shop As String = "DARWIN TRADING"
        Try
            shop = PosRepository.ShopSetting("shop_name", "DARWIN TRADING")
        Catch
            ' default
        End Try

        _btnPrintTest.Enabled = False
        _prn.SetStatus("Printing...", "idle")
        RunAsync(Of String)(
            Function()
                Try
                    PrinterTools.PrintTestSlip(p.Name, shop)
                    Return ""
                Catch ex As Exception
                    Return ex.Message
                End Try
            End Function,
            Sub(err As String)
                _btnPrintTest.Enabled = True
                ShowPrinter()
                If err Is Nothing OrElse err.Length > 0 Then
                    _prn.SetStatus("Test failed", "bad")
                    Notify(Me, "Devices", "The test slip could not be printed:" & Environment.NewLine & If(err, "unknown error"), MessageBoxIcon.Warning)
                Else
                    _prn.SetStatus("Test sent", "ok")
                    AuditLog.Write("DEVICE_TEST", "Devices", "Printed a test slip on '" & p.Name & "'")
                    Notify(Me, "Devices", "The test slip was sent to '" & p.Name & "'." & Environment.NewLine & "If it came out and you can read everything, the printer works.", MessageBoxIcon.Information)
                End If
            End Sub)
    End Sub

    Private Sub OnUsePrinter(sender As Object, e As EventArgs)
        Dim p As PrinterInfo = PickedPrinter()
        If p Is Nothing Then Return
        Try
            LocalDevices.ReceiptPrinter = p.Name
            AuditLog.Write("EDIT", "Devices", "This laptop prints receipts and labels on '" & p.Name & "'")
            ShowPrinter()
        Catch ex As Exception
            Notify(Me, "Devices", "Could not save: " & ex.Message, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' ------------------------------------------------------------------ barcode scanner
    Private Sub BuildScanner()
        _scn.Height = Theme.S(410)

        _scanHelp.AutoSize = False
        _scanHelp.Font = Theme.FontBase
        _scanHelp.ForeColor = Theme.TextDark
        _scanHelp.Location = New Point(Theme.S(20), Theme.S(54))
        _scanHelp.Size = New Size(Theme.S(800), Theme.S(44))
        _scanHelp.Text = "Click in the box below, then scan any barcode (a product, or a loyalty card)." & Environment.NewLine &
                         "A scanner types the whole code in a split second and presses Enter."

        _scanBox.Font = New Font("Consolas", 16.0F)
        _scanBox.Location = New Point(Theme.S(20), Theme.S(104))
        _scanBox.Size = New Size(Theme.S(520), Theme.S(36))
        AddHandler _scanBox.KeyPress, AddressOf OnScanKeyPress

        _scanLog.AutoSize = False
        _scanLog.Font = New Font("Consolas", 9.5F)
        _scanLog.ForeColor = Theme.TextDark
        _scanLog.Location = New Point(Theme.S(20), Theme.S(152))
        _scanLog.Size = New Size(Theme.S(800), Theme.S(130))
        _scanLog.Text = "No scan yet."

        _scanHints.AutoSize = False
        _scanHints.Font = Theme.FontSmall
        _scanHints.ForeColor = Theme.TextMuted
        _scanHints.Location = New Point(Theme.S(20), Theme.S(290))
        _scanHints.Size = New Size(Theme.S(800), Theme.S(56))
        _scanHints.Text = "Looking at what Windows lists..."

        _btnClearScan.Text = "Clear"
        _btnClearScan.MakeSecondary()
        _btnClearScan.Size = New Size(Theme.S(110), Theme.S(40))
        _btnClearScan.Location = New Point(Theme.S(20), Theme.S(356))
        AddHandler _btnClearScan.Click, AddressOf OnClearScan

        _scn.Controls.Add(_scanHelp)
        _scn.Controls.Add(_scanBox)
        _scn.Controls.Add(_scanLog)
        _scn.Controls.Add(_scanHints)
        _scn.Controls.Add(_btnClearScan)
        _scn.SetStatus("Waiting for a scan", "idle")
    End Sub

    Private Sub OnClearScan(sender As Object, e As EventArgs)
        _scanLines.Clear()
        _scanLog.Text = "No scan yet."
        _scn.SetStatus("Waiting for a scan", "idle")
        _scanBox.Clear()
        _scanBox.Focus()
    End Sub

    Private Sub OnScanKeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = Convert.ToChar(13) Then
            e.Handled = True
            FinishScan()
            Return
        End If
        If Not _scanWatch.IsRunning Then
            _scanWatch.Restart()
            _scanChars = 0
        End If
        If e.KeyChar >= " "c Then _scanChars += 1
    End Sub

    Private Function Describe(code As String) As String
        Try
            Dim pid As Integer = ProductLookup.FindByBarcode(code)
            If pid > 0 Then
                Dim dt As DataTable = Db.Table("SELECT name, measurement FROM products WHERE id = @i", Db.Params("@i", pid))
                If dt.Rows.Count > 0 Then Return "product: " & (dt.Rows(0)("name").ToString() & " " & dt.Rows(0)("measurement").ToString()).Trim()
            End If
            Dim c As DataRow = CustomerRepository.FindByCard(code)
            If c IsNot Nothing Then Return "loyalty card of " & c("name").ToString()
        Catch
            Return "(could not look it up)"
        End Try
        Return "not in the system" & If(Barcodes.IsValidEan13(code), " (a valid EAN-13)", "")
    End Function

    Private Sub FinishScan()
        Dim ms As Double = _scanWatch.Elapsed.TotalMilliseconds
        Dim chars As Integer = _scanChars
        _scanWatch.Reset()
        _scanChars = 0
        Dim code As String = _scanBox.Text.Trim()
        _scanBox.Clear()
        If code.Length = 0 Then Return

        Dim isScanner As Boolean = ScannerTools.LooksLikeScan(chars, ms)
        If isScanner Then
            _scn.SetStatus("Scanner works", "ok")
        Else
            _scn.SetStatus("Typed by hand", "warn")
        End If

        _scanLines.Insert(0, DateTime.Now.ToString("h:mm:ss tt") & "   " & code & "   " & chars.ToString() & " characters in " & ms.ToString("0") & " ms   " &
                             If(isScanner, "[scanner]", "[typed by hand]") & Environment.NewLine & "               -> " & Describe(code))
        While _scanLines.Count > 4
            _scanLines.RemoveAt(_scanLines.Count - 1)
        End While
        _scanLog.Text = String.Join(Environment.NewLine, _scanLines)
    End Sub

    ' ------------------------------------------------------------------ start
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        OnBodySize(Me, EventArgs.Empty)
        LoadConfig()
        RenderSmsInfo()
        OnRefreshPrinters(Me, EventArgs.Empty)
        RunAsync(Of List(Of String))(Function() ScannerTools.LookLikeScanners(),
            Sub(names As List(Of String))
                Dim t As String = "Windows lists these scanner-like devices: " & If(names Is Nothing OrElse names.Count = 0, "none", String.Join(", ", names)) &
                                  "   |   Serial ports: " & ScannerTools.SerialPortsText() & Environment.NewLine &
                                  "Most scanners pose as a plain keyboard, so Windows cannot always tell them apart: scanning a code here is the real test."
                _scanHints.Text = t
            End Sub)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
    End Sub

End Class

''' <summary>Send a short test text to a phone number, to see that the SMS modem works.</summary>
Public Class SmsTestDialog
    Inherits ModalDialog

    Private ReadOnly _phone As New InputField("Mobile number to test", InputKind.MobilePH, 300, True)
    Private ReadOnly _text As New InputField("Message", InputKind.Text, 440, True)
    Private ReadOnly _send As New MaroonButton()
    Private ReadOnly _close As New MaroonButton()

    Public Sub New()
        MyBase.New("Send test message", "Sends a real text, so use your own number.", 520, 440)

        _phone.Location = New Point(Theme.S(40), Theme.S(108))
        _text.Location = New Point(Theme.S(40), Theme.S(182))
        _text.MaxLength = 100
        _text.Value = "DARWIN TRADING: test message " & DateTime.Now.ToString("h:mm tt")

        Message.Location = New Point(Theme.S(40), Theme.S(262))
        Message.Size = New Size(Theme.S(440), Theme.S(60))

        _close.Text = "Close"
        _close.MakeSecondary()
        _close.Size = New Size(Theme.S(130), Theme.S(44))
        _close.Location = New Point(Theme.S(220), Theme.S(340))
        AddHandler _close.Click, AddressOf OnClose

        _send.Text = "Send"
        _send.Size = New Size(Theme.S(130), Theme.S(44))
        _send.Location = New Point(Theme.S(360), Theme.S(340))
        AddHandler _send.Click, AddressOf OnSend

        Controls.Add(_phone)
        Controls.Add(_text)
        Controls.Add(_close)
        Controls.Add(_send)
        ClientSize = New Size(Theme.S(520), Theme.S(340) + Theme.S(44) + Theme.S(28))
        AcceptButton = _send
        CancelButton = _close
    End Sub

    Private Sub OnClose(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnSend(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Dim problem As String = InputField.CheckAll(_phone, _text)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        Dim phone As String = _phone.Value.Trim()
        Dim text As String = _text.Value.Trim()
        Dim hasModem As Boolean = SmsService.HasModem()
        _send.Enabled = False
        ShowMessage(If(hasModem, "Sending through the modem (a few seconds)...", "Queueing the message..."), False)

        Dim ctx As System.Threading.SynchronizationContext = System.Threading.SynchronizationContext.Current
        System.Threading.Tasks.Task.Run(
            Sub()
                Dim err As String = ""
                Dim ok As Boolean
                If hasModem Then
                    ok = SmsService.SendNow(phone, text, err)
                Else
                    ok = SmsService.QueueSms(phone, text)
                    If Not ok Then err = "Could not add the message to the queue."
                End If
                ctx.Post(Sub(state)
                             If IsDisposed Then Return
                             _send.Enabled = True
                             If ok Then
                                 AuditLog.Write("DEVICE_TEST", "Devices", "Sent a test text to " & phone)
                                 ShowMessage(If(hasModem, "Sent! The modem accepted the message. Check the phone.",
                                                "Queued. This PC has no modem: the PC with the modem sends it within a few seconds. Check the phone."), False)
                             Else
                                 ShowMessage("Not sent: " & err, True)
                             End If
                         End Sub, Nothing)
            End Sub)
    End Sub
End Class
