Option Strict On
Option Explicit On

Imports System.Collections.Generic

''' <summary>
''' Code 128 barcodes (the kind every scanner reads). Encode() returns the bars as a string of 1 (bar) and 0 (space).
''' Digits are packed in pairs (set C) when there are 4 or more in a row, so a 13-digit code stays narrow.
''' </summary>
Public Module Code128

    ' the 106 symbols (values 0 to 105), 11 modules each
    Private ReadOnly Patterns As String() = {
        "11011001100", "11001101100", "11001100110", "10010011000", "10010001100", "10001001100",
        "10011001000", "10011000100", "10001100100", "11001001000", "11001000100", "11000100100",
        "10110011100", "10011011100", "10011001110", "10111001100", "10011101100", "10011100110",
        "11001110010", "11001011100", "11001001110", "11011100100", "11001110100", "11101101110",
        "11101001100", "11100101100", "11100100110", "11101100100", "11100110100", "11100110010",
        "11011011000", "11011000110", "11000110110", "10100011000", "10001011000", "10001000110",
        "10110001000", "10001101000", "10001100010", "11010001000", "11000101000", "11000100010",
        "10110111000", "10110001110", "10001101110", "10111011000", "10111000110", "10001110110",
        "11101110110", "11010001110", "11000101110", "11011101000", "11011100010", "11011101110",
        "11101011000", "11101000110", "11100010110", "11101101000", "11101100010", "11100011010",
        "11101111010", "11001000010", "11110001010", "10100110000", "10100001100", "10010110000",
        "10010000110", "10000101100", "10000100110", "10110010000", "10110000100", "10011010000",
        "10011000010", "10000110100", "10000110010", "11000010010", "11001010000", "11110111010",
        "11000010100", "10001111010", "10100111100", "10010111100", "10010011110", "10111100100",
        "10011110100", "10011110010", "11110100100", "11110010100", "11110010010", "11011011110",
        "11011110110", "11110110110", "10101111000", "10100011110", "10001011110", "10111101000",
        "10111100010", "11110101000", "11110100010", "10111011110", "10111101110", "11101011110",
        "11110101110", "11010000100", "11010010000", "11010011100"
    }

    Private Const StopPattern As String = "1100011101011"

    Private Function IsDigit(c As Char) As Boolean
        Return c >= "0"c AndAlso c <= "9"c
    End Function

    Private Function DigitRun(text As String, start As Integer) As Integer
        Dim n As Integer = 0
        Dim i As Integer = start
        While i < text.Length AndAlso IsDigit(text(i))
            n += 1
            i += 1
        End While
        Return n
    End Function

    ''' <summary>True when every character can be printed (plain keyboard characters).</summary>
    Public Function CanEncode(text As String) As Boolean
        If String.IsNullOrEmpty(text) Then Return False
        For Each ch As Char In text
            Dim code As Integer = Convert.ToInt32(ch)
            If code < 32 OrElse code > 126 Then Return False
        Next
        Return True
    End Function

    ''' <summary>The bars as "1" (bar) and "0" (space), start and stop included. Empty when the text cannot be encoded.</summary>
    Public Function Encode(text As String) As String
        If Not CanEncode(text) Then Return ""

        Dim values As New List(Of Integer)()
        Dim modeC As Boolean = (DigitRun(text, 0) >= 4)
        values.Add(If(modeC, 105, 104))

        Dim i As Integer = 0
        While i < text.Length
            If modeC Then
                If i + 1 < text.Length AndAlso IsDigit(text(i)) AndAlso IsDigit(text(i + 1)) Then
                    values.Add(Integer.Parse(text.Substring(i, 2)))
                    i += 2
                Else
                    values.Add(100)
                    modeC = False
                End If
            Else
                If DigitRun(text, i) >= 4 Then
                    values.Add(99)
                    modeC = True
                Else
                    values.Add(Convert.ToInt32(text(i)) - 32)
                    i += 1
                End If
            End If
        End While

        Dim sum As Integer = values(0)
        For k As Integer = 1 To values.Count - 1
            sum += values(k) * k
        Next
        values.Add(sum Mod 103)

        Dim sb As New System.Text.StringBuilder()
        For Each v As Integer In values
            sb.Append(Patterns(v))
        Next
        sb.Append(StopPattern)
        Return sb.ToString()
    End Function

End Module
