Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Globalization

Public Enum ReportPeriod
    Daily = 0
    Weekly = 1
    Monthly = 2
End Enum

''' <summary>A report period: From (inclusive) to ToExclusive, with a readable label.</summary>
Public Class DateRange
    Public Property From As DateTime
    Public Property ToExclusive As DateTime
    Public Property Label As String = ""
    Public ReadOnly Property Days As Integer
        Get
            Return CInt((ToExclusive - From).TotalDays)
        End Get
    End Property
End Class

Public Module ReportPeriods

    Private ReadOnly Inv As CultureInfo = CultureInfo.InvariantCulture

    Public Function RangeFor(p As ReportPeriod, anchor As DateTime) As DateRange
        Dim d As DateTime = anchor.Date
        Dim r As New DateRange()
        Select Case p
            Case ReportPeriod.Daily
                r.From = d
                r.ToExclusive = d.AddDays(1)
                r.Label = d.ToString("dddd, MMMM d, yyyy", Inv)
            Case ReportPeriod.Weekly
                Dim offset As Integer = (CInt(d.DayOfWeek) + 6) Mod 7        ' weeks run Monday to Sunday
                r.From = d.AddDays(-offset)
                r.ToExclusive = r.From.AddDays(7)
                Dim last As DateTime = r.ToExclusive.AddDays(-1)
                r.Label = r.From.ToString("MMM d", Inv) & " - " & last.ToString("MMM d, yyyy", Inv)
            Case Else
                r.From = New DateTime(d.Year, d.Month, 1)
                r.ToExclusive = r.From.AddMonths(1)
                r.Label = d.ToString("MMMM yyyy", Inv)
        End Select
        Return r
    End Function

    ''' <summary>Short text for the date button: "Fri, Nov 13, 2026" / "Nov 9 - Nov 15, 2026" / "November 2026".</summary>
    Public Function ShortLabel(p As ReportPeriod, anchor As DateTime) As String
        Dim r As DateRange = RangeFor(p, anchor)
        Select Case p
            Case ReportPeriod.Daily
                Return r.From.ToString("ddd, MMM d, yyyy", Inv)
            Case ReportPeriod.Weekly
                Return r.From.ToString("MMM d", Inv) & " - " & r.ToExclusive.AddDays(-1).ToString("MMM d, yyyy", Inv)
            Case Else
                Return r.From.ToString("MMMM yyyy", Inv)
        End Select
    End Function

    Public Function Shift(p As ReportPeriod, anchor As DateTime, direction As Integer) As DateTime
        Select Case p
            Case ReportPeriod.Daily
                Return anchor.AddDays(direction)
            Case ReportPeriod.Weekly
                Return anchor.AddDays(7 * direction)
            Case Else
                Return anchor.AddMonths(direction)
        End Select
    End Function

    Public Function PeriodName(p As ReportPeriod) As String
        Select Case p
            Case ReportPeriod.Daily
                Return "Daily"
            Case ReportPeriod.Weekly
                Return "Weekly"
            Case Else
                Return "Monthly"
        End Select
    End Function

    ''' <summary>"yesterday", "last week", "last month" (the period before this one).</summary>
    Public Function PreviousName(p As ReportPeriod) As String
        Select Case p
            Case ReportPeriod.Daily
                Return "the day before"
            Case ReportPeriod.Weekly
                Return "the week before"
            Case Else
                Return "the month before"
        End Select
    End Function

End Module

Public Enum ColKind
    Text = 0
    Money = 1
    Number = 2
    Percent = 3
    Stamp = 4        ' date and time
    DayOnly = 5      ' date only
End Enum

Public Class ReportColumn
    Public Property Header As String = ""
    Public Property Field As String = ""
    Public Property Kind As ColKind = ColKind.Text
    Public Property Weight As Single = 10.0F
    Public Property MinWidth As Integer = 90

    Public Sub New(header As String, field As String, kind As ColKind, weight As Single, Optional minWidth As Integer = 90)
        Me.Header = header
        Me.Field = field
        Me.Kind = kind
        Me.Weight = weight
        Me.MinWidth = minWidth
    End Sub

    Public ReadOnly Property RightAligned As Boolean
        Get
            Return Kind = ColKind.Money OrElse Kind = ColKind.Number OrElse Kind = ColKind.Percent
        End Get
    End Property

    ''' <summary>Header used in PDF / Excel / print (money columns say PHP).</summary>
    Public ReadOnly Property ExportHeader As String
        Get
            Return If(Kind = ColKind.Money, Header & " (PHP)", Header)
        End Get
    End Property
End Class

Public Class ReportKpi
    Public Property Caption As String = ""
    Public Property Value As String = ""
    Public Property Warn As Boolean

    Public Sub New(caption As String, value As String, Optional warn As Boolean = False)
        Me.Caption = caption
        Me.Value = value
        Me.Warn = warn
    End Sub
End Class

Public Class ChartSeries
    Public Property Name As String = ""
    Public Property Color As Color = Theme.Maroon
    Public ReadOnly Property Values As New List(Of Decimal)()

    Public Sub New(name As String, color As Color)
        Me.Name = name
        Me.Color = color
    End Sub
End Class

Public Class ReportChart
    Public Property Title As String = ""
    Public Property Horizontal As Boolean
    Public Property IsMoney As Boolean
    Public ReadOnly Property Labels As New List(Of String)()
    Public ReadOnly Property Series As New List(Of ChartSeries)()

    Public ReadOnly Property HasData As Boolean
        Get
            For Each s As ChartSeries In Series
                For Each v As Decimal In s.Values
                    If v <> 0D Then Return True
                Next
            Next
            Return False
        End Get
    End Property
End Class

''' <summary>Everything one report view shows. The same object feeds the screen, the PDF, Excel and the printout.</summary>
Public Class ReportDoc
    Public Property ShopName As String = "DARWIN TRADING"
    Public Property Title As String = ""
    Public Property PeriodLabel As String = ""
    Public Property Note As String = ""
    Public Property GeneratedBy As String = ""
    Public Property GeneratedAt As DateTime = DateTime.Now
    Public ReadOnly Property Kpis As New List(Of ReportKpi)()
    Public ReadOnly Property Charts As New List(Of ReportChart)()
    Public Property TableTitle As String = ""
    Public ReadOnly Property Columns As New List(Of ReportColumn)()
    Public Property Data As DataTable = New DataTable()
    Public Property EmptyText As String = "Nothing to show for this period."
    Public Property TruncatedNote As String = ""
    ''' <summary>A tip shown under the table on screen only (not in the PDF / printout).</summary>
    Public Property ScreenHint As String = ""

    Public Function FileBaseName() As String
        Dim s As String = Title & " - " & PeriodLabel
        For Each c As Char In System.IO.Path.GetInvalidFileNameChars()
            s = s.Replace(c, "-"c)
        Next
        Return s
    End Function
End Class

Public Module ReportFormat

    Private ReadOnly Inv As CultureInfo = CultureInfo.InvariantCulture
    Public ReadOnly PesoSign As String = ChrW(&H20B1)

    Public Function Money(d As Decimal) As String
        Return d.ToString("N2", Inv)
    End Function

    ''' <summary>"PHP 1,234.00" for print / PDF / Excel text.</summary>
    Public Function Php(d As Decimal) As String
        Return "PHP " & d.ToString("N2", Inv)
    End Function

    ''' <summary>On-screen peso text.</summary>
    Public Function Peso(d As Decimal) As String
        Return PesoSign & d.ToString("N2", Inv)
    End Function

    Public Function Whole(n As Decimal) As String
        Return n.ToString("N0", Inv)
    End Function

    ''' <summary>A cell as plain text for the PDF / printout.</summary>
    Public Function Cell(v As Object, kind As ColKind) As String
        If v Is Nothing OrElse v Is DBNull.Value Then Return ""
        Select Case kind
            Case ColKind.Money
                Return Convert.ToDecimal(v).ToString("N2", Inv)
            Case ColKind.Number
                Dim d As Decimal = Convert.ToDecimal(v)
                Return If(d = Math.Floor(d), d.ToString("N0", Inv), d.ToString("N2", Inv))
            Case ColKind.Percent
                Return Convert.ToDecimal(v).ToString("0.0", Inv) & "%"
            Case ColKind.Stamp
                Return Convert.ToDateTime(v).ToString("MMM d, yyyy h:mm tt", Inv)
            Case ColKind.DayOnly
                Return Convert.ToDateTime(v).ToString("MMM d, yyyy", Inv)
            Case Else
                Return Convert.ToString(v, Inv)
        End Select
    End Function

    ''' <summary>Grid number/date format for a column kind.</summary>
    Public Function GridFormat(kind As ColKind) As String
        Select Case kind
            Case ColKind.Money
                Return "'" & PesoSign & "'#,##0.00"
            Case ColKind.Number
                Return "#,##0"
            Case ColKind.Percent
                Return "0.0'%'"
            Case ColKind.Stamp
                Return "MMM d, yyyy h:mm tt"
            Case ColKind.DayOnly
                Return "MMM d, yyyy"
            Case Else
                Return ""
        End Select
    End Function

    ''' <summary>Short axis label: 1200 -> 1.2K.</summary>
    Public Function AxisLabel(v As Decimal) As String
        Dim a As Decimal = Math.Abs(v)
        If a >= 1000000D Then Return (v / 1000000D).ToString("0.#", Inv) & "M"
        If a >= 1000D Then Return (v / 1000D).ToString("0.#", Inv) & "K"
        Return v.ToString("0.##", Inv)
    End Function

    ''' <summary>A tidy step so that 4 steps cover the largest value (1, 2, 5 times a power of ten).</summary>
    Public Function NiceStep(maxValue As Decimal) As Decimal
        If maxValue <= 0D Then Return 0.25D
        Dim rough As Double = CDbl(maxValue) / 4.0R
        Dim mag As Double = Math.Pow(10.0R, Math.Floor(Math.Log10(rough)))
        Dim frac As Double = rough / mag
        Dim f As Double
        If frac <= 1.0R Then
            f = 1.0R
        ElseIf frac <= 2.0R Then
            f = 2.0R
        ElseIf frac <= 2.5R Then
            f = 2.5R
        ElseIf frac <= 5.0R Then
            f = 5.0R
        Else
            f = 10.0R
        End If
        Return CDec(f * mag)
    End Function

End Module
