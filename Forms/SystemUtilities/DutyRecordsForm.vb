Option Strict On
Option Explicit On

''' <summary>
''' The "Duty records" tab of User Monitoring: a daily time record (DTR) for every person, with the same
''' Daily / Weekly / Monthly tabs, charts, Save PDF, Save Excel and Print as the other reports.
''' </summary>
Public Class DutyRecordsForm
    Inherits ReportFormBase

    Protected Overrides ReadOnly Property ReportTitle As String
        Get
            Return "Duty Records"
        End Get
    End Property

    Protected Overrides ReadOnly Property ReportBlurb As String
        Get
            Return "How long each person was logged in"
        End Get
    End Property

    Protected Overrides ReadOnly Property ViewNames As String()
        Get
            Return New String() {"Duty records", "Per person", "Sessions"}
        End Get
    End Property

    Public Overrides ReadOnly Property WatchedTables As String()
        Get
            Return New String() {"user_sessions"}
        End Get
    End Property

    Protected Overrides Function BuildDoc(view As Integer, range As DateRange, period As ReportPeriod) As ReportDoc
        Select Case view
            Case 1
                Return DutyData.PerPerson(period, range)
            Case 2
                Return DutyData.Sessions(period, range)
            Case Else
                Return DutyData.Records(period, range)
        End Select
    End Function

    Protected Overrides Function SearchEnabled(view As Integer) As Boolean
        Return view <> 1
    End Function

End Class
