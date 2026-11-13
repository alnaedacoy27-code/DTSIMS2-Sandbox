Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions

Public Class ModemMessage
    Public Property Index As Integer
    Public Property Status As String = ""
    Public Property Sender As String = ""
    Public Property Text As String = ""
End Class

''' <summary>Reads what a GSM modem answers (signal, SIM, network, received texts, balance). No serial port in here, so it can be tested.</summary>
Public Module ModemParsers

    ' ---- signal: AT+CSQ answers "+CSQ: 19,99" (0 to 31; 99 = unknown)
    Public Function RssiFromCsq(response As String) As Integer
        Dim m As Match = Regex.Match(If(response, ""), "\+CSQ:\s*(\d+)\s*,\s*(\d+)")
        If Not m.Success Then Return -1
        Dim v As Integer = Integer.Parse(m.Groups(1).Value, CultureInfo.InvariantCulture)
        Return If(v >= 0 AndAlso v <= 31, v, -1)
    End Function

    Public Function DbmFromRssi(rssi As Integer) As Integer
        Return -113 + 2 * rssi
    End Function

    ''' <summary>0 to 5 bars.</summary>
    Public Function BarsFromRssi(rssi As Integer) As Integer
        If rssi < 2 Then Return 0
        If rssi <= 9 Then Return 1
        If rssi <= 14 Then Return 2
        If rssi <= 19 Then Return 3
        If rssi <= 24 Then Return 4
        Return 5
    End Function

    ' ---- SIM: AT+CPIN? answers "+CPIN: READY", "+CPIN: SIM PIN" or "+CME ERROR: SIM not inserted"
    Public Function SimStatus(response As String) As String
        Dim r As String = If(response, "")
        If r.Contains("+CPIN: READY") Then Return "Ready"
        If r.Contains("SIM PIN") Then Return "Locked: it needs a PIN"
        If r.Contains("SIM PUK") Then Return "Locked: it needs a PUK code"
        If r.ToLowerInvariant().Contains("not inserted") OrElse r.ToLowerInvariant().Contains("sim failure") OrElse r.Contains("ERROR") Then Return "No SIM card"
        Return "Unknown"
    End Function

    ' ---- network: AT+CREG? answers "+CREG: 0,1" (1 = registered at home, 5 = roaming)
    Public Function IsRegistered(response As String) As Boolean
        Dim m As Match = Regex.Match(If(response, ""), "\+CREG:\s*\d+\s*,\s*(\d+)")
        If Not m.Success Then Return False
        Dim stat As Integer = Integer.Parse(m.Groups(1).Value, CultureInfo.InvariantCulture)
        Return stat = 1 OrElse stat = 5
    End Function

    ' ---- AT+COPS? answers +COPS: 0,0,"GLOBE"
    Public Function OperatorName(response As String) As String
        Dim m As Match = Regex.Match(If(response, ""), "\+COPS:\s*\d+(?:\s*,\s*\d+\s*,\s*""([^""]*)"")?")
        If m.Success AndAlso m.Groups(1).Success Then Return m.Groups(1).Value
        Return ""
    End Function

    ' ---- AT+CGMM answers the model on its own line
    Public Function ModelName(response As String) As String
        For Each raw As String In If(response, "").Split(New Char() {Convert.ToChar(13), Convert.ToChar(10)})
            Dim l As String = raw.Trim()
            If l.Length = 0 OrElse l = "OK" OrElse l.StartsWith("AT", StringComparison.OrdinalIgnoreCase) OrElse l.StartsWith("+") Then Continue For
            Return l
        Next
        Return ""
    End Function

    ' ---- received texts: AT+CMGL="ALL" in text mode
    '   +CMGL: 1,"REC UNREAD","222",,"26/11/12,10:20:00+32"
    '   Your balance is P25.50 ...

    Public Function ParseCmgl(response As String) As List(Of ModemMessage)

        Dim list As New List(Of ModemMessage)()

        Dim lines As String() =
    If(response, "").Replace(vbCr.ToString(), "").Split(New Char() {Convert.ToChar(10)})

        Dim current As ModemMessage = Nothing
        Dim body As New StringBuilder()

        For Each raw As String In lines

            Dim line As String = raw.TrimEnd()

            Dim m As Match = Regex.Match(
            line,
            "^\s*\+CMGL:\s*(\d+)\s*,\s*""([^""]*)""\s*,\s*""([^""]*)""",
            RegexOptions.IgnoreCase
        )

            If m.Success Then

                If current IsNot Nothing Then
                    current.Text = body.ToString().Trim()
                    list.Add(current)
                End If

                current = New ModemMessage()
                current.Index = Integer.Parse(
                m.Groups(1).Value,
                CultureInfo.InvariantCulture
            )
                current.Status = m.Groups(2).Value
                current.Sender = m.Groups(3).Value

                body.Clear()

            ElseIf current IsNot Nothing Then

                If line.Trim() = "OK" Then
                    Exit For
                End If

                If line.Trim().Length > 0 Then
                    If body.Length > 0 Then body.AppendLine()
                    body.Append(line)
                End If

            End If
        Next

        If current IsNot Nothing Then
            current.Text = body.ToString().Trim()
            list.Add(current)
        End If

        Return list
    End Function


    ''' <summary>Finds the money amount in a balance text ("Your balance is P25.50", "Bal: PhP 1,250.00", "balance 25.5").</summary>


    Public Function ExtractBalance(text As String,
                               ByRef amount As Decimal) As Boolean

        amount = 0D

        If String.IsNullOrWhiteSpace(text) Then Return False

        Dim styles As NumberStyles =
        NumberStyles.AllowDecimalPoint Or NumberStyles.AllowThousands

        Dim patterns As String() = {
        "(?:balance|bal|remaining\s+(?:load|balance))\s*(?:is|:|=)?\s*(?:PHP|PhP|₱|P)?\s*(\d[\d,]*(?:\.\d{1,2})?)",
        "(?:PHP|PhP|₱)\s*(\d[\d,]*(?:\.\d{1,2})?)",
        "(\d[\d,]*(?:\.\d{1,2})?)\s*(?:PHP|pesos?)"
    }

        For Each pattern As String In patterns

            Dim m As Match = Regex.Match(
            text,
            pattern,
            RegexOptions.IgnoreCase
        )

            If m.Success Then
                If Decimal.TryParse(
                m.Groups(1).Value,
                styles,
                CultureInfo.InvariantCulture,
                amount
            ) Then
                    Return True
                End If
            End If
        Next

        Return False
    End Function



    ''' <summary>The text inside a +CUSD answer. Plain text, or hex when the network answers in UCS2.</summary>
    Public Function DecodeCusd(response As String) As String
        Dim m As Match = Regex.Match(If(response, ""), "\+CUSD:\s*\d+\s*(?:,\s*""([\s\S]*?)""\s*(?:,\s*\d+)?)?")
        If Not m.Success OrElse Not m.Groups(1).Success Then Return ""
        Dim s As String = m.Groups(1).Value
        If s.Length >= 8 AndAlso s.Length Mod 4 = 0 AndAlso Regex.IsMatch(s, "^[0-9A-Fa-f]+$") Then
            Try
                Dim bytes(s.Length \ 2 - 1) As Byte
                For i As Integer = 0 To bytes.Length - 1
                    bytes(i) = Convert.ToByte(s.Substring(i * 2, 2), 16)
                Next
                Dim decoded As String = Encoding.BigEndianUnicode.GetString(bytes)
                Dim printable As Boolean = True
                For Each ch As Char In decoded
                    If Char.IsControl(ch) AndAlso ch <> Convert.ToChar(10) AndAlso ch <> Convert.ToChar(13) Then printable = False
                Next
                If printable Then Return decoded.Trim()
            Catch
                ' keep the original text
            End Try
        End If
        Return s.Trim()
    End Function

    ''' <summary>Texts left = balance divided by the price of one text. 0 when the price is not set.</summary>
    Public Function TextsLeft(balance As Decimal, costPerText As Decimal) As Integer
        If balance <= 0D OrElse costPerText <= 0D Then Return 0
        Return CInt(Math.Floor(balance / costPerText))
    End Function

End Module
