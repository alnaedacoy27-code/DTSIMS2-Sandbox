Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.IO.Ports
Imports System.Threading
Imports MySql.Data.MySqlClient

Public Class ModemStatus
    Public Property PortName As String = ""
    Public Property PortPresent As Boolean
    Public Property Responds As Boolean
    Public Property Model As String = ""
    Public Property Sim As String = ""
    Public Property NetworkName As String = ""
    Public Property Registered As Boolean
    Public Property Rssi As Integer = -1
    Public Property Problem As String = ""
End Class

Public Class BalanceConfig
    ' "text" = send a keyword to a number and read the reply; "ussd" = dial a USSD code
    Public Property Method As String = "text"
    Public Property Number As String = "222"
    Public Property Keyword As String = "BAL"
    Public Property UssdCode As String = "*143#"
    Public Property CostPerText As Decimal = 1D
End Class

''' <summary>
''' Text messages through the GSM modem (ported from the old system).
'''  - ANY PC calls QueueSms(): the message is dropped into the sms_queue table.
'''  - ONLY the PC that has the modem runs the worker: every 5 seconds it sends each Pending message
'''    through the modem and DELETES it once the network accepted it.
''' The modem port is the setting "sms_modem_port" (COM9 by default).
''' </summary>
Public Module SmsService

    Private Const MaxAttempts As Integer = 3

    Private ReadOnly modemLock As New Object()
    Private isProcessing As Integer = 0
    Private queueTimer As System.Threading.Timer
    Private portName As String = ""

    ' ------------------------------------------------------------------ messages
    Private Function Cut(text As String, max As Integer) As String
        text = If(text, "").Trim()
        Return If(text.Length <= max, text, text.Substring(0, max).Trim())
    End Function

    Public Function WelcomeText(name As String, card As String, points As Decimal) As String
        Return "DARWIN TRADING: Welcome " & Cut(name, 30) & "! Loyalty card no. " & card & " is ready. Show it on every purchase to earn points."
    End Function

    Public Function EarnedText(name As String, total As Decimal, earned As Decimal, balance As Decimal, receipt As String) As String
        Return "DARWIN TRADING: Thanks " & Cut(name, 30) & "! +" & Loyalty.Fmt(earned) & " pts on PHP " &
               total.ToString("N2", Globalization.CultureInfo.InvariantCulture) & ". Balance: " & Loyalty.Fmt(balance) & " pts. Rcpt " & receipt
    End Function

    Public Function UsedText(name As String, used As Decimal, balance As Decimal, receipt As String) As String
        Return "DARWIN TRADING: " & Cut(name, 30) & ", you used " & Loyalty.Fmt(used) & " pts (PHP " &
               used.ToString("N2", Globalization.CultureInfo.InvariantCulture) & "). Balance: " & Loyalty.Fmt(balance) & " pts. Rcpt " & receipt
    End Function

    ' ------------------------------------------------------------------ modem on this PC?
    Private _lastDetect As DateTime = DateTime.MinValue
    Private _workerStarting As Boolean

    ''' <summary>
    ''' Reads the saved modem port. The slow part (opening every COM port and talking to it, about
    ''' 1-2 seconds PER port) only runs when detect = True, and at most once every 10 minutes
    ''' unless force = True (the Devices screen's own check). Normal screens never scan.
    ''' </summary>
    Private Sub LoadPortSetting(Optional detect As Boolean = False, Optional force As Boolean = False)

        Try
            Dim o As Object = Db.Scalar(
                "SELECT setting_value FROM settings WHERE setting_key = 'sms_modem_port'")

            If o IsNot Nothing AndAlso o IsNot DBNull.Value Then
                portName = o.ToString().Trim()
            End If

        Catch
        End Try

        ' Check whether the saved port still exists.
        If String.IsNullOrWhiteSpace(portName) OrElse
           Array.IndexOf(SerialPort.GetPortNames(), portName) < 0 Then

            If Not detect Then Return
            If Not force AndAlso (DateTime.UtcNow - _lastDetect).TotalMinutes < 10 Then Return
            _lastDetect = DateTime.UtcNow

            Dim detected As String = DetectModemPort()

            If Not String.IsNullOrWhiteSpace(detected) Then
                portName = detected

                Try
                    SaveSetting("sms_modem_port", portName)
                Catch
                End Try
            Else
                portName = ""
            End If
        End If

    End Sub

    Public Function ModemPort() As String
        Return portName
    End Function

    Public Function HasModem() As Boolean
        LoadPortSetting()

        Return Not String.IsNullOrWhiteSpace(portName) AndAlso
           Array.IndexOf(SerialPort.GetPortNames(), portName) >= 0
    End Function

    ''' <summary>A short line for the screen: is the modem here and how many messages wait.</summary>
    Public Function StatusText() As String
        Dim waiting As Integer = 0
        Try
            waiting = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM sms_queue WHERE status IN ('Pending','Sending')"))
        Catch
            ' ignore
        End Try
        Dim where As String = If(HasModem(), "Modem found on " & portName & " (this PC sends the messages)", "No modem on this PC (" & portName & " not found)")
        Return "SMS: " & where & "  |  " & waiting.ToString() & " waiting"
    End Function

    ' ------------------------------------------------------------------ the worker
    ''' <summary>Call once when the main window opens. Does nothing on a PC without the modem; safe to call again.</summary>
    Public Sub StartQueueWorker()
        If queueTimer IsNot Nothing OrElse _workerStarting Then Return
        _workerStarting = True

        ' the port check can take seconds (it talks to every COM port), so never on the screen thread
        System.Threading.Tasks.Task.Run(
            Sub()
                Try
                    LoadPortSetting(True)

                    If String.IsNullOrWhiteSpace(portName) OrElse Array.IndexOf(SerialPort.GetPortNames(), portName) < 0 Then
                        LogSms("Worker NOT started: " & portName & " not found. Ports on this PC: " & PortList())
                        Return
                    End If

                    LogSms("Worker started on " & portName)
                    ResetStuckMessages()
                    queueTimer = New System.Threading.Timer(Sub(state) ProcessQueue(), Nothing, 0, 5000)
                Catch ex As Exception
                    LogSms("StartQueueWorker error: " & ex.Message)
                Finally
                    _workerStarting = False
                End Try
            End Sub)
    End Sub

    Public Sub StopQueueWorker()
        If queueTimer IsNot Nothing Then
            queueTimer.Dispose()
            queueTimer = Nothing
        End If
    End Sub

    ''' <summary>Any PC calls this instead of sending directly. Returns False when the message could not be queued.</summary>
    Public Function QueueSms(phone As String, message As String) As Boolean
        If String.IsNullOrWhiteSpace(phone) OrElse String.IsNullOrWhiteSpace(message) Then Return False
        Try
            Db.NonQuery("INSERT INTO sms_queue (phone_number, message, status) VALUES (@p, @m, 'Pending')",
                        Db.Params("@p", phone.Trim(), "@m", message))
            LogSms("Queued SMS for " & phone.Trim())
            Return True
        Catch ex As Exception
            LogSms("QueueSms error: " & ex.Message)
            Return False
        End Try
    End Function

    ' anything left as 'Sending' means the app closed in the middle of a send
    Public Sub ResetStuckMessages()
        Try
            Db.NonQuery("UPDATE sms_queue SET status = 'Pending' WHERE status = 'Sending'")
        Catch ex As Exception
            LogSms("ResetStuckMessages error: " & ex.Message)
        End Try
    End Sub

    ' runs on a background thread: no message boxes in here
    Public Sub ProcessQueue()
        If Interlocked.Exchange(isProcessing, 1) = 1 Then Return
        Try
            ' do not let failed messages pile up
            Db.NonQuery("DELETE FROM sms_queue WHERE status = 'Failed' AND created_at < (NOW() - INTERVAL 1 DAY)")

            Dim dt As DataTable = Db.Table("SELECT id, phone_number, message FROM sms_queue WHERE status = 'Pending' ORDER BY id LIMIT 10")
            For Each row As DataRow In dt.Rows
                Dim id As Integer = Convert.ToInt32(row("id"))
                Dim phone As String = row("phone_number").ToString()
                Dim message As String = row("message").ToString()

                ' claim the row so it can never be sent twice
                If Db.NonQuery("UPDATE sms_queue SET status = 'Sending' WHERE id = @id AND status = 'Pending'", Db.Params("@id", id)) = 0 Then Continue For

                Dim errorMsg As String = ""
                If SendViaModem(phone, message, errorMsg) Then
                    Db.NonQuery("DELETE FROM sms_queue WHERE id = @id", Db.Params("@id", id))
                    LogSms("SMS #" & id.ToString() & " sent to " & phone & " and removed from queue")
                Else
                    LogSms("SMS #" & id.ToString() & " to " & phone & " FAILED: " & errorMsg)
                    ' status first, then attempts: MySQL reads the SET list left to right
                    Db.NonQuery("UPDATE sms_queue SET status = IF(attempts + 1 >= @max, 'Failed', 'Pending'), attempts = attempts + 1 WHERE id = @id",
                                Db.Params("@max", MaxAttempts, "@id", id))
                End If
            Next
        Catch ex As Exception
            LogSms("ProcessQueue error: " & ex.Message)
        Finally
            Interlocked.Exchange(isProcessing, 0)
        End Try
    End Sub

    ' ------------------------------------------------------------------ the modem (same AT steps as before)
    Private Function SendViaModem(phone As String, text As String, ByRef errorMsg As String) As Boolean
        SyncLock modemLock
            Try
                Using modem As New SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
                    modem.ReadTimeout = 5000
                    modem.WriteTimeout = 10000
                    modem.Open()
                    Thread.Sleep(1500)
                    modem.DiscardInBuffer()
                    modem.DiscardOutBuffer()

                    Dim resp As String = ""
                    If Not AtCommand(modem, "AT", "OK", 3000, resp) Then
                        errorMsg = "Modem did not respond: " & resp
                        Return False
                    End If
                    AtCommand(modem, "AT+CMEE=2", "OK", 2000, resp)
                    If Not AtCommand(modem, "AT+CSCS=""GSM""", "OK", 3000, resp) Then
                        errorMsg = "Could not set GSM charset: " & resp
                        Return False
                    End If
                    If Not AtCommand(modem, "AT+CMGF=1", "OK", 3000, resp) Then
                        errorMsg = "Could not enable text mode: " & resp
                        Return False
                    End If
                    If Not AtCommand(modem, "AT+CMGS=""" & phone & """", ">", 10000, resp) Then
                        errorMsg = "No > prompt: " & resp
                        Return False
                    End If

                    modem.Write(text & Convert.ToChar(26).ToString())

                    ' +CMGS: <n> means the network accepted the message
                    ' (do not wait for a bare "OK": the modem echoes the text back and a name like JOKER contains OK)
                    If Not WaitFor(modem, "+CMGS:", 30000, resp) Then
                        errorMsg = "Send not confirmed: " & resp
                        Return False
                    End If
                    Return True
                End Using
            Catch ex As Exception
                errorMsg = ex.Message
                Return False
            End Try
        End SyncLock
    End Function

    ' ------------------------------------------------------------------ Devices screen: modem health, balance, direct test
    Private Function EndsWithOk(buffer As String) As Boolean
        Return buffer.TrimEnd().EndsWith("OK")
    End Function

    ' like WaitFor, but for long answers (a list of texts): wait for the final OK at the end
    Private Function WaitForEnd(modem As SerialPort, timeoutMs As Integer, ByRef buffer As String) As Boolean
        buffer = ""
        Dim sw As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
        While sw.ElapsedMilliseconds < timeoutMs
            If modem.BytesToRead > 0 Then
                buffer &= modem.ReadExisting()
                If EndsWithOk(buffer) Then Return True
                If buffer.Contains("ERROR") Then Return False
            End If
            Thread.Sleep(50)
        End While
        Return False
    End Function


    Private Function OpenModem() As SerialPort

        Dim detectedPort As String = DetectModemPort()

        If String.IsNullOrEmpty(detectedPort) Then
            Throw New Exception("SIM800C modem not found. Please check the USB connection.")
        End If

        portName = detectedPort

        Dim modem As New SerialPort(portName, 9600, Parity.None, 8, StopBits.One)

        modem.ReadTimeout = 5000
        modem.WriteTimeout = 10000
        modem.Handshake = Handshake.None

        modem.Open()
        Thread.Sleep(1500)

        modem.DiscardInBuffer()
        modem.DiscardOutBuffer()

        Return modem

    End Function



    Private Function DetectModemPort() As String
        For Each port As String In SerialPort.GetPortNames()
            Try
                Using modem As New SerialPort(port, 9600, Parity.None, 8, StopBits.One)
                    modem.ReadTimeout = 2000
                    modem.WriteTimeout = 2000
                    modem.Handshake = Handshake.None
                    modem.Open()
                    Thread.Sleep(500)
                    modem.DiscardInBuffer()
                    modem.Write("AT" & vbCr)
                    Thread.Sleep(500)
                    Dim response As String = modem.ReadExisting()
                    If response.Contains("OK") Then

                        modem.DiscardInBuffer()
                        modem.Write("ATI" & vbCr)

                        Thread.Sleep(700)

                        response = modem.ReadExisting()

                        If response.IndexOf("SIM800", StringComparison.OrdinalIgnoreCase) >= 0 Then
                            Return port
                        End If

                    End If

                End Using

            Catch
                ' Port unavailable or not a modem. Try the next one.
            End Try

        Next

        Return Nothing

    End Function


    ''' <summary>Is the modem here, does it answer, is the SIM in, is there a network and how strong is the signal.</summary>
    Public Function QueryStatus() As ModemStatus
        Dim st As New ModemStatus()
        LoadPortSetting(True, True)
        st.PortName = portName
        st.PortPresent = HasModem()
        If Not st.PortPresent Then
            st.Problem = "The modem is not plugged into this PC (" & portName & " was not found). Ports found: " & PortList() & "."
            Return st
        End If

        SyncLock modemLock
            Try
                Using modem As SerialPort = OpenModem()
                    Dim resp As String = ""
                    If Not AtCommand(modem, "AT", "OK", 3000, resp) Then
                        st.Problem = "The modem is on " & portName & " but did not answer."
                        Return st
                    End If
                    st.Responds = True

                    If AtCommand(modem, "AT+CGMM", "OK", 3000, resp) Then st.Model = ModemParsers.ModelName(resp)
                    AtCommand(modem, "AT+CPIN?", "OK", 3000, resp)
                    st.Sim = ModemParsers.SimStatus(resp)
                    AtCommand(modem, "AT+CSQ", "OK", 3000, resp)
                    st.Rssi = ModemParsers.RssiFromCsq(resp)
                    AtCommand(modem, "AT+CREG?", "OK", 3000, resp)
                    st.Registered = ModemParsers.IsRegistered(resp)
                    AtCommand(modem, "AT+COPS?", "OK", 5000, resp)
                    st.NetworkName = ModemParsers.OperatorName(resp)
                End Using
            Catch ex As Exception
                st.Problem = "Could not talk to the modem: " & ex.Message
            End Try
        End SyncLock
        Return st
    End Function

    ''' <summary>Sends a message right now through the modem on this PC (for the test). Use QueueSms from other PCs.</summary>
    Public Function SendNow(phone As String, text As String, ByRef errorMsg As String) As Boolean
        LoadPortSetting()
        If Not HasModem() Then
            errorMsg = "The modem is not plugged into this PC (" & portName & " was not found)."
            Return False
        End If
        Return SendViaModem(phone, text, errorMsg)
    End Function

    Private Function SettingText(key As String, fallback As String) As String
        Try
            Dim o As Object = Db.Scalar("SELECT setting_value FROM settings WHERE setting_key = @k", Db.Params("@k", key))
            If o IsNot Nothing AndAlso o IsNot DBNull.Value AndAlso o.ToString().Trim().Length > 0 Then Return o.ToString().Trim()
        Catch
            ' default
        End Try
        Return fallback
    End Function

    Private Sub SaveSetting(key As String, value As String)
        Db.NonQuery("INSERT INTO settings (setting_key, setting_value) VALUES (@k, @v) ON DUPLICATE KEY UPDATE setting_value = @v",
                    Db.Params("@k", key, "@v", value))
    End Sub

    Public Function GetBalanceConfig() As BalanceConfig
        Dim c As New BalanceConfig()
        c.Method = SettingText("sms_balance_method", "text")
        c.Number = SettingText("sms_balance_number", "222")
        c.Keyword = SettingText("sms_balance_keyword", "BAL")
        c.UssdCode = SettingText("sms_balance_ussd", "*143#")
        Dim v As Decimal
        If Decimal.TryParse(SettingText("sms_cost_per_text", "1"), Globalization.NumberStyles.AllowDecimalPoint, Globalization.CultureInfo.InvariantCulture, v) Then c.CostPerText = v
        Return c
    End Function

    Public Sub SaveBalanceConfig(c As BalanceConfig)
        SaveSetting("sms_balance_method", c.Method)
        SaveSetting("sms_balance_number", c.Number)
        SaveSetting("sms_balance_keyword", c.Keyword)
        SaveSetting("sms_balance_ussd", c.UssdCode)
        SaveSetting("sms_cost_per_text", c.CostPerText.ToString("0.####", Globalization.CultureInfo.InvariantCulture))
    End Sub

    Public Function PendingCount() As Integer
        Try
            Return Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM sms_queue WHERE status IN ('Pending','Sending')"))
        Catch
            Return 0
        End Try
    End Function

    ' all texts stored on the SIM

    Private Function ReadInbox(ByRef errorMsg As String,
                           Optional status As String = "ALL") As List(Of ModemMessage)

        Dim result As New List(Of ModemMessage)()
        errorMsg = ""

        SyncLock modemLock
            Try
                Using modem As SerialPort = OpenModem()

                    Dim resp As String = ""

                    If Not AtCommand(modem, "AT", "OK", 3000, resp) Then
                        errorMsg = "Modem did not respond to AT: " & resp
                        Return result
                    End If

                    If Not AtCommand(modem, "AT+CMEE=2", "OK", 3000, resp) Then
                        errorMsg = "Could not enable modem error reporting: " & resp
                        Return result
                    End If

                    If Not AtCommand(modem, "AT+CMGF=1", "OK", 3000, resp) Then
                        errorMsg = "Could not enable SMS text mode: " & resp
                        Return result
                    End If

                    ' Use GSM text encoding.
                    AtCommand(modem, "AT+CSCS=""GSM""", "OK", 3000, resp)

                    ' Store incoming SMS and notify the serial port with +CMTI.
                    ' Some modem firmware may not support this exact setting.
                    AtCommand(modem, "AT+CNMI=2,1,0,0,0", "OK", 3000, resp)

                    modem.DiscardInBuffer()
                    modem.Write("AT+CMGL=""" & status & """" & Convert.ToChar(13).ToString())

                    If Not WaitForEnd(modem, 15000, resp) Then
                        errorMsg = "Could not finish reading SMS inbox. Modem response: " & resp
                        Return result
                    End If

                    result = ModemParsers.ParseCmgl(resp)
                End Using

            Catch ex As Exception
                errorMsg = "Inbox read error: " & ex.Message
            End Try
        End SyncLock

        Return result
    End Function


    Private Sub DeleteFromSim(index As Integer)
        SyncLock modemLock
            Try
                Using modem As SerialPort = OpenModem()
                    Dim resp As String = ""
                    If AtCommand(modem, "AT", "OK", 3000, resp) Then AtCommand(modem, "AT+CMGD=" & index.ToString(), "OK", 5000, resp)
                End Using
            Catch
                ' not important
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' Asks the SIM provider for the load balance, using the settings (a text to a number, or a USSD code),
    ''' and finds the amount in the answer. rawReply is always filled with what the provider answered.
    ''' </summary>
    Public Function CheckBalance(cfg As BalanceConfig, ByRef rawReply As String, ByRef amount As Decimal, ByRef errorMsg As String) As Boolean
        rawReply = ""
        amount = 0D
        errorMsg = ""
        LoadPortSetting()
        If Not HasModem() Then
            errorMsg = "The modem is not plugged into this PC (" & portName & " was not found)."
            Return False
        End If
        If cfg.Method = "ussd" Then Return BalanceByUssd(cfg.UssdCode, rawReply, amount, errorMsg)
        Return BalanceByText(cfg.Number, cfg.Keyword, rawReply, amount, errorMsg)
    End Function


    Private Function BalanceByText(number As String,
                               keyword As String,
                               ByRef rawReply As String,
                               ByRef amount As Decimal,
                               ByRef errorMsg As String) As Boolean

        rawReply = ""
        amount = 0D
        errorMsg = ""

        If String.IsNullOrWhiteSpace(number) OrElse
       String.IsNullOrWhiteSpace(keyword) Then

            errorMsg = "Set the inquiry number and SMS keyword."
            Return False
        End If

        SyncLock modemLock
            Try
                ' Read existing messages first. This also helps clear old unread
                ' messages so they are not mistaken for the provider's new reply.
                Dim before As New HashSet(Of String)(StringComparer.Ordinal)

                Dim readError As String = ""
                Dim oldMessages As List(Of ModemMessage) = ReadInbox(readError, "ALL")

                If readError <> "" Then
                    errorMsg = "Could not read the modem inbox before sending: " & readError
                    Return False
                End If

                For Each msg As ModemMessage In oldMessages
                    before.Add(msg.Index.ToString() & "|" & msg.Sender & "|" & msg.Text)
                Next

                ' Send the balance inquiry.
                Dim sendError As String = ""

                If Not SendViaModem(number.Trim(), keyword.Trim(), sendError) Then
                    errorMsg = "Balance inquiry SMS could not be sent: " & sendError
                    Return False
                End If

                ' Wait up to 90 seconds for the network's reply.
                Dim sw As System.Diagnostics.Stopwatch =
                System.Diagnostics.Stopwatch.StartNew()

                Dim lastInboxError As String = ""

                While sw.ElapsedMilliseconds < 90000

                    Thread.Sleep(3000)

                    Dim inboxError As String = ""

                    ' First try unread messages only.
                    Dim messages As List(Of ModemMessage) =
                    ReadInbox(inboxError, "REC UNREAD")

                    If inboxError <> "" Then
                        lastInboxError = inboxError

                        ' Fallback: read all messages if unread filtering fails.
                        inboxError = ""
                        messages = ReadInbox(inboxError, "ALL")

                        If inboxError <> "" Then
                            lastInboxError = inboxError
                            Continue While
                        End If
                    End If

                    Dim newest As ModemMessage = Nothing

                    For Each msg As ModemMessage In messages

                        Dim key As String =
                        msg.Index.ToString() & "|" & msg.Sender & "|" & msg.Text

                        If Not before.Contains(key) Then
                            newest = msg
                        End If
                    Next

                    If newest IsNot Nothing Then

                        rawReply = newest.Text

                        ' Remove the received message after saving its contents.
                        DeleteFromSim(newest.Index)

                        If ModemParsers.ExtractBalance(rawReply, amount) Then
                            Return True
                        End If

                        errorMsg =
                        "The provider replied, but the balance amount could not be extracted. " &
                        "Please check the raw provider response."

                        Return False
                    End If
                End While

                If lastInboxError <> "" Then
                    errorMsg =
                    "The inquiry SMS was sent, but the modem could not read its inbox: " &
                    lastInboxError
                Else
                    errorMsg =
                    "The inquiry SMS was sent, but no new provider reply was detected " &
                    "within 90 seconds. Check the inquiry number, keyword, SIM signal, " &
                    "and whether the SIM can receive SMS."
                End If

                Return False

            Catch ex As Exception
                errorMsg = "Balance check error: " & ex.Message
                Return False
            End Try
        End SyncLock
    End Function


    Private Function BalanceByUssd(code As String, ByRef rawReply As String, ByRef amount As Decimal, ByRef errorMsg As String) As Boolean
        If String.IsNullOrWhiteSpace(code) Then
            errorMsg = "Type the USSD code (for example *143#)."
            Return False
        End If
        SyncLock modemLock
            Try
                Using modem As SerialPort = OpenModem()
                    Dim resp As String = ""
                    If Not AtCommand(modem, "AT", "OK", 3000, resp) Then
                        errorMsg = "The modem did not answer."
                        Return False
                    End If
                    AtCommand(modem, "AT+CMEE=2", "OK", 2000, resp)
                    modem.DiscardInBuffer()
                    modem.Write("AT+CUSD=1,""" & code.Trim() & """,15" & Convert.ToChar(13).ToString())
                    If Not WaitFor(modem, "+CUSD:", 30000, resp) Then
                        errorMsg = "No answer to the USSD code. Not every modem or provider supports it: try the text method."
                        Return False
                    End If
                    Thread.Sleep(800)
                    If modem.BytesToRead > 0 Then resp &= modem.ReadExisting()
                    AtCommand(modem, "AT+CUSD=2", "OK", 3000, resp & "")
                    rawReply = ModemParsers.DecodeCusd(resp)
                    If rawReply.Length = 0 Then
                        errorMsg = "The provider answered without any text."
                        Return False
                    End If
                    If ModemParsers.ExtractBalance(rawReply, amount) Then Return True
                    errorMsg = "The provider answered, but no amount was found (shown below). A menu needs a code that goes straight to the balance."
                    Return False
                End Using
            Catch ex As Exception
                errorMsg = ex.Message
                Return False
            End Try
        End SyncLock
    End Function

    ' ------------------------------------------------------------------ log file (sms_log.txt next to the .exe)
    Private ReadOnly logLock As New Object()

    Private Sub LogSms(text As String)
        Try
            SyncLock logLock
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sms_log.txt"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "  " & text & Environment.NewLine)
            End SyncLock
        Catch
            ' logging must never crash the app
        End Try
    End Sub

    Private Function PortList() As String
        Try
            Dim ports As String() = SerialPort.GetPortNames()
            Return If(ports.Length = 0, "(none)", String.Join(", ", ports))
        Catch
            Return "(could not read ports)"
        End Try
    End Function

    Private Function AtCommand(modem As SerialPort, command As String, expected As String, timeoutMs As Integer, ByRef response As String) As Boolean
        modem.DiscardInBuffer()
        modem.Write(command & Convert.ToChar(13).ToString())
        Return WaitFor(modem, expected, timeoutMs, response)
    End Function

    Private Function WaitFor(modem As SerialPort, expected As String, timeoutMs As Integer, ByRef buffer As String) As Boolean
        buffer = ""
        Dim sw As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
        While sw.ElapsedMilliseconds < timeoutMs
            If modem.BytesToRead > 0 Then
                buffer &= modem.ReadExisting()
                If buffer.Contains(expected) Then Return True
                If buffer.Contains("ERROR") Then Return False
            End If
            Thread.Sleep(50)
        End While
        Return False
    End Function

End Module
