Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Globalization

''' <summary>
''' Daily time record (DTR) built from user_sessions: when each person logged in, logged out, and for how long.
'''  - a session ends at its logout time;
'''  - if the person is still logged in (fresh heartbeat) it runs until now;
'''  - if there is no logout (app closed, crash, PC switched off) the last heartbeat time (last_seen) is used.
''' Time is split at midnight so each day gets its own share, and overlapping sessions (two PCs at once) are counted once.
''' </summary>
Public Module DutyData

    Private ReadOnly Inv As CultureInfo = CultureInfo.InvariantCulture
    Private Const MaxRows As Integer = 3000
    Private Const LiveSeconds As Integer = 30

    Private Class Seg
        Public SessionId As String = ""
        Public UserId As Integer
        Public Name As String = ""
        Public Role As String = ""
        Public Device As String = ""
        Public LoginAt As DateTime
        Public EndAt As DateTime
        Public Status As Integer            ' 0 logged out, 1 still logged in, 2 no logout recorded
    End Class

    Private Class Iv
        Public S As DateTime
        Public E As DateTime
    End Class

    Private Class DayRec
        Public UserId As Integer
        Public Name As String = ""
        Public Role As String = ""
        Public Day As DateTime
        Public ReadOnly Intervals As New List(Of Iv)()
        Public ReadOnly Merged As New List(Of Iv)()
        Public ReadOnly SessionIds As New HashSet(Of String)()
        Public AnyActive As Boolean
        Public AnyOpen As Boolean
        Public Continued As Boolean         ' the session started the day before
        Public RunsOn As Boolean            ' the session goes on past midnight
        Public Seconds As Long
        Public FirstIn As DateTime
        Public LastOut As DateTime
    End Class

    ' ------------------------------------------------------------------ helpers
    Private Function Txt(o As Object) As String
        If o Is Nothing OrElse o Is DBNull.Value Then Return ""
        Return Convert.ToString(o, Inv)
    End Function

    Private Function RoleText(role As String) As String
        Dim r As String = Permissions.NormalizeRole(role)
        If r.Length = 0 Then Return ""
        Return Char.ToUpperInvariant(r(0)) & r.Substring(1)
    End Function

    ''' <summary>"8 h 12 min", "35 min".</summary>
    Public Function Duration(seconds As Long) As String
        Dim mins As Long = CLng(Math.Round(seconds / 60.0R))
        If mins <= 0 Then Return If(seconds > 0, "under 1 min", "0 min")
        Dim h As Long = mins \ 60
        Dim m As Long = mins Mod 60
        If h = 0 Then Return m.ToString(Inv) & " min"
        Return h.ToString(Inv) & " h " & m.ToString(Inv) & " min"
    End Function

    Private Function Hours(seconds As Long) As Decimal
        Return Math.Round(CDec(seconds) / 3600D, 2)
    End Function

    Private Function TimeText(t As DateTime) As String
        Return t.ToString("h:mm tt", Inv)
    End Function

    Private Function StatusText(code As Integer) As String
        Select Case code
            Case 1
                Return "Still logged in"
            Case 2
                Return "No logout recorded"
            Case Else
                Return "Logged out"
        End Select
    End Function

    Private Function NewDoc(title As String, r As DateRange) As ReportDoc
        Dim d As New ReportDoc()
        d.ShopName = PosRepository.ShopSetting("shop_name", "DARWIN TRADING")
        d.Title = title
        d.PeriodLabel = r.Label
        d.GeneratedBy = AppSession.FullName
        d.GeneratedAt = DateTime.Now
        Return d
    End Function

    ' ------------------------------------------------------------------ reading the sessions
    Private Function LoadSegs(r As DateRange) As List(Of Seg)
        Dim t As DataTable = Db.Table(
            "SELECT s.session_id, s.user_id, u.username, u.full_name, u.role, s.device_name, s.login_at, s.last_seen, s.logout_at, s.is_active, " &
            "NOW() AS server_now " &
            "FROM user_sessions s INNER JOIN users u ON u.id = s.user_id " &
            "WHERE s.login_at < @t AND COALESCE(s.logout_at, s.last_seen) >= @f ORDER BY s.login_at",
            Db.Params("@f", r.From, "@t", r.ToExclusive))

        Dim list As New List(Of Seg)()
        For Each row As DataRow In t.Rows
            Dim sg As New Seg()
            sg.SessionId = Txt(row("session_id"))
            sg.UserId = Convert.ToInt32(row("user_id"))
            sg.Name = Txt(row("full_name"))
            If sg.Name.Length = 0 Then sg.Name = Txt(row("username"))
            sg.Role = RoleText(Txt(row("role")))
            sg.Device = Txt(row("device_name"))
            sg.LoginAt = Convert.ToDateTime(row("login_at"))

            Dim lastSeen As DateTime = Convert.ToDateTime(row("last_seen"))
            Dim serverNow As DateTime = Convert.ToDateTime(row("server_now"))
            If row("logout_at") IsNot DBNull.Value Then
                sg.EndAt = Convert.ToDateTime(row("logout_at"))
                sg.Status = 0
            ElseIf Convert.ToInt32(row("is_active")) = 1 AndAlso (serverNow - lastSeen).TotalSeconds <= LiveSeconds Then
                sg.EndAt = serverNow
                sg.Status = 1
            Else
                sg.EndAt = lastSeen
                sg.Status = 2
            End If
            If sg.EndAt < sg.LoginAt Then sg.EndAt = sg.LoginAt
            list.Add(sg)
        Next
        Return list
    End Function

    ''' <summary>One record per person per calendar day, with overlapping time merged.</summary>
    Private Function BuildDays(segs As List(Of Seg), r As DateRange) As List(Of DayRec)
        Dim map As New Dictionary(Of String, DayRec)()
        Dim lastDay As DateTime = r.ToExclusive.Date.AddDays(-1)

        For Each sg As Seg In segs
            Dim d As DateTime = If(sg.LoginAt.Date > r.From.Date, sg.LoginAt.Date, r.From.Date)
            Dim stopDay As DateTime = If(sg.EndAt.Date < lastDay, sg.EndAt.Date, lastDay)
            While d <= stopDay
                Dim dayStart As DateTime = d
                Dim dayEnd As DateTime = d.AddDays(1)
                Dim fromT As DateTime = If(sg.LoginAt > dayStart, sg.LoginAt, dayStart)
                Dim toT As DateTime = If(sg.EndAt < dayEnd, sg.EndAt, dayEnd)

                ' a session that ended exactly at midnight leaves nothing for the next day
                If toT <= fromT AndAlso sg.LoginAt < dayStart Then
                    d = d.AddDays(1)
                    Continue While
                End If

                Dim key As String = sg.UserId.ToString(Inv) & "|" & d.ToString("yyyyMMdd", Inv)
                Dim rec As DayRec = Nothing
                If Not map.TryGetValue(key, rec) Then
                    rec = New DayRec()
                    rec.UserId = sg.UserId
                    rec.Name = sg.Name
                    rec.Role = sg.Role
                    rec.Day = d
                    map(key) = rec
                End If

                Dim piece As New Iv()
                piece.S = fromT
                piece.E = toT
                rec.Intervals.Add(piece)
                rec.SessionIds.Add(sg.SessionId)
                If sg.LoginAt < dayStart Then rec.Continued = True
                If sg.EndAt >= dayEnd Then rec.RunsOn = True
                If d = sg.EndAt.Date Then
                    If sg.Status = 1 Then rec.AnyActive = True
                    If sg.Status = 2 Then rec.AnyOpen = True
                End If
                d = d.AddDays(1)
            End While
        Next

        Dim result As New List(Of DayRec)(map.Values)
        For Each rec As DayRec In result
            rec.Intervals.Sort(Function(a As Iv, b As Iv) a.S.CompareTo(b.S))
            Dim cur As Iv = Nothing
            For Each piece As Iv In rec.Intervals
                If cur Is Nothing Then
                    cur = New Iv()
                    cur.S = piece.S
                    cur.E = piece.E
                ElseIf piece.S <= cur.E Then
                    If piece.E > cur.E Then cur.E = piece.E
                Else
                    rec.Merged.Add(cur)
                    cur = New Iv()
                    cur.S = piece.S
                    cur.E = piece.E
                End If
            Next
            If cur IsNot Nothing Then rec.Merged.Add(cur)
            For Each m As Iv In rec.Merged
                rec.Seconds += CLng((m.E - m.S).TotalSeconds)
            Next
            rec.FirstIn = rec.Merged(0).S
            rec.LastOut = rec.Merged(rec.Merged.Count - 1).E
        Next
        Return result
    End Function

    Private Function ByTimeIn(a As DayRec, b As DayRec) As Integer
        Return a.FirstIn.CompareTo(b.FirstIn)
    End Function

    Private Function ByPersonThenDay(a As DayRec, b As DayRec) As Integer
        Dim c As Integer = String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
        If c <> 0 Then Return c
        Return a.Day.CompareTo(b.Day)
    End Function

    ' ------------------------------------------------------------------ status figures
    Private Sub AddKpis(doc As ReportDoc, recs As List(Of DayRec))
        Dim people As New HashSet(Of Integer)()
        Dim sessions As New HashSet(Of String)()
        Dim total As Long = 0
        Dim longest As Long = 0
        Dim openCount As Integer = 0
        For Each rec As DayRec In recs
            people.Add(rec.UserId)
            For Each sid As String In rec.SessionIds
                sessions.Add(sid)
            Next
            total += rec.Seconds
            If rec.Seconds > longest Then longest = rec.Seconds
            If rec.AnyOpen Then openCount += 1
        Next
        Dim avg As Long = If(recs.Count > 0, total \ recs.Count, 0L)

        doc.Kpis.Add(New ReportKpi("People on duty", people.Count.ToString(Inv)))
        doc.Kpis.Add(New ReportKpi("Total time logged in", Duration(total)))
        doc.Kpis.Add(New ReportKpi("Average per person per day", Duration(avg)))
        doc.Kpis.Add(New ReportKpi("Longest day", Duration(longest)))
        doc.Kpis.Add(New ReportKpi("Sessions", sessions.Count.ToString(Inv)))
        doc.Kpis.Add(New ReportKpi("No logout recorded", openCount.ToString(Inv), openCount > 0))

        doc.Note = "Time is counted from login to logout." &
                   If(openCount > 0, " Where there is no logout, the last time the PC reported in is used.", "")
    End Sub

    Private Function PersonChart(recs As List(Of DayRec)) As ReportChart
        Dim secs As New Dictionary(Of Integer, Long)()
        Dim names As New Dictionary(Of Integer, String)()
        For Each rec As DayRec In recs
            Dim cur As Long = 0
            secs.TryGetValue(rec.UserId, cur)
            secs(rec.UserId) = cur + rec.Seconds
            names(rec.UserId) = rec.Name
        Next
        Dim ids As New List(Of Integer)(secs.Keys)
        ids.Sort(Function(a As Integer, b As Integer) secs(b).CompareTo(secs(a)))

        Dim ch As New ReportChart()
        ch.Title = "Hours logged in per person"
        ch.Horizontal = True
        Dim s As New ChartSeries("Hours", Theme.Maroon)
        For i As Integer = 0 To Math.Min(9, ids.Count - 1)
            ch.Labels.Add(names(ids(i)))
            s.Values.Add(Hours(secs(ids(i))))
        Next
        ch.Series.Add(s)
        Return ch
    End Function

    ''' <summary>Daily: how many people were logged in during each hour. Weekly / monthly: hours logged in per day.</summary>
    Private Function TrendChart(p As ReportPeriod, r As DateRange, recs As List(Of DayRec)) As ReportChart
        Dim ch As New ReportChart()
        If p = ReportPeriod.Daily Then
            ch.Title = "People logged in per hour"
            Dim lo As Integer = 8
            Dim hi As Integer = 18
            For Each rec As DayRec In recs
                If rec.FirstIn.Hour < lo Then lo = rec.FirstIn.Hour
                If rec.LastOut.Hour > hi Then hi = rec.LastOut.Hour
            Next
            Dim s As New ChartSeries("People", Theme.Maroon)
            For h As Integer = lo To hi
                Dim hStart As DateTime = r.From.Date.AddHours(h)
                Dim hEnd As DateTime = hStart.AddHours(1)
                Dim present As New HashSet(Of Integer)()
                For Each rec As DayRec In recs
                    For Each m As Iv In rec.Merged
                        If m.S < hEnd AndAlso m.E > hStart Then present.Add(rec.UserId)
                    Next
                Next
                Dim h12 As Integer = h Mod 12
                If h12 = 0 Then h12 = 12
                ch.Labels.Add(h12.ToString(Inv) & If(h < 12, "a", "p"))
                s.Values.Add(present.Count)
            Next
            ch.Series.Add(s)
        Else
            ch.Title = "Hours logged in per day"
            Dim perDay As New Dictionary(Of DateTime, Long)()
            For Each rec As DayRec In recs
                Dim cur As Long = 0
                perDay.TryGetValue(rec.Day, cur)
                perDay(rec.Day) = cur + rec.Seconds
            Next
            Dim s As New ChartSeries("Hours", Theme.Maroon)
            Dim d As DateTime = r.From
            While d < r.ToExclusive
                ch.Labels.Add(If(p = ReportPeriod.Weekly, d.ToString("ddd d", Inv), d.Day.ToString(Inv)))
                Dim v As Long = 0
                perDay.TryGetValue(d, v)
                s.Values.Add(Hours(v))
                d = d.AddDays(1)
            End While
            ch.Series.Add(s)
        End If
        Return ch
    End Function

    ' =====================================================================================
    '  VIEW 1: duty records (one row per person per day)
    ' =====================================================================================
    Public Function Records(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Duty Records", r)
        Dim recs As List(Of DayRec) = BuildDays(LoadSegs(r), r)
        AddKpis(doc, recs)

        If p = ReportPeriod.Daily Then
            recs.Sort(AddressOf ByTimeIn)
        Else
            recs.Sort(AddressOf ByPersonThenDay)
        End If

        doc.Charts.Add(TrendChart(p, r, recs))
        doc.Charts.Add(PersonChart(recs))

        Dim dt As New DataTable()
        dt.Columns.Add("day", GetType(DateTime))
        dt.Columns.Add("name", GetType(String))
        dt.Columns.Add("role", GetType(String))
        dt.Columns.Add("time_in", GetType(String))
        dt.Columns.Add("time_out", GetType(String))
        dt.Columns.Add("total", GetType(String))
        dt.Columns.Add("hours", GetType(Decimal))
        dt.Columns.Add("sessions", GetType(Integer))
        dt.Columns.Add("status", GetType(String))
        For Each rec As DayRec In recs
            Dim timeIn As String = If(rec.Continued, "Continued", TimeText(rec.FirstIn))
            Dim timeOut As String = If(rec.AnyActive, "Still in", If(rec.RunsOn, "Continues", TimeText(rec.LastOut)))
            Dim status As String = If(rec.AnyActive, "Still logged in", If(rec.AnyOpen, "No logout recorded", "Complete"))
            dt.Rows.Add(rec.Day, rec.Name, rec.Role, timeIn, timeOut, Duration(rec.Seconds), Hours(rec.Seconds), rec.SessionIds.Count, status)
        Next

        doc.TableTitle = "Daily time record"
        doc.Columns.Add(New ReportColumn("DATE", "day", ColKind.DayOnly, 12, 120))
        doc.Columns.Add(New ReportColumn("PERSON", "name", ColKind.Text, 20, 150))
        doc.Columns.Add(New ReportColumn("ROLE", "role", ColKind.Text, 10, 90))
        doc.Columns.Add(New ReportColumn("TIME IN", "time_in", ColKind.Text, 10, 90))
        doc.Columns.Add(New ReportColumn("TIME OUT", "time_out", ColKind.Text, 10, 90))
        doc.Columns.Add(New ReportColumn("TOTAL TIME", "total", ColKind.Text, 13, 110))
        doc.Columns.Add(New ReportColumn("HOURS", "hours", ColKind.Number, 8, 80))
        doc.Columns.Add(New ReportColumn("SESSIONS", "sessions", ColKind.Number, 9, 90))
        doc.Columns.Add(New ReportColumn("STATUS", "status", ColKind.Text, 15, 130))
        doc.Data = dt
        doc.EmptyText = "Nobody logged in during this period."
        Return doc
    End Function

    ' =====================================================================================
    '  VIEW 2: per person (the totals a payroll needs)
    ' =====================================================================================
    Private Class Person
        Public Name As String = ""
        Public Role As String = ""
        Public Days As Integer
        Public Seconds As Long
        Public ReadOnly Sessions As New HashSet(Of String)()
        Public FirstIn As DateTime = DateTime.MaxValue
        Public LastOut As DateTime = DateTime.MinValue
    End Class

    Public Function PerPerson(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Duty Records - Per person", r)
        Dim recs As List(Of DayRec) = BuildDays(LoadSegs(r), r)
        AddKpis(doc, recs)
        doc.Charts.Add(TrendChart(p, r, recs))
        doc.Charts.Add(PersonChart(recs))

        Dim people As New Dictionary(Of Integer, Person)()
        For Each rec As DayRec In recs
            Dim pr As Person = Nothing
            If Not people.TryGetValue(rec.UserId, pr) Then
                pr = New Person()
                pr.Name = rec.Name
                pr.Role = rec.Role
                people(rec.UserId) = pr
            End If
            pr.Days += 1
            pr.Seconds += rec.Seconds
            For Each sid As String In rec.SessionIds
                pr.Sessions.Add(sid)
            Next
            If rec.FirstIn < pr.FirstIn Then pr.FirstIn = rec.FirstIn
            If rec.LastOut > pr.LastOut Then pr.LastOut = rec.LastOut
        Next

        Dim list As New List(Of Person)(people.Values)
        list.Sort(Function(a As Person, b As Person) b.Seconds.CompareTo(a.Seconds))

        Dim dt As New DataTable()
        dt.Columns.Add("name", GetType(String))
        dt.Columns.Add("role", GetType(String))
        dt.Columns.Add("days", GetType(Integer))
        dt.Columns.Add("total", GetType(String))
        dt.Columns.Add("hours", GetType(Decimal))
        dt.Columns.Add("avg_hours", GetType(Decimal))
        dt.Columns.Add("sessions", GetType(Integer))
        dt.Columns.Add("first_in", GetType(DateTime))
        dt.Columns.Add("last_out", GetType(DateTime))
        For Each pr As Person In list
            Dim avg As Decimal = If(pr.Days > 0, Math.Round(Hours(pr.Seconds) / pr.Days, 2), 0D)
            dt.Rows.Add(pr.Name, pr.Role, pr.Days, Duration(pr.Seconds), Hours(pr.Seconds), avg, pr.Sessions.Count, pr.FirstIn, pr.LastOut)
        Next

        doc.TableTitle = "Time logged in per person"
        doc.Columns.Add(New ReportColumn("PERSON", "name", ColKind.Text, 20, 150))
        doc.Columns.Add(New ReportColumn("ROLE", "role", ColKind.Text, 10, 90))
        doc.Columns.Add(New ReportColumn("DAYS PRESENT", "days", ColKind.Number, 10, 100))
        doc.Columns.Add(New ReportColumn("TOTAL TIME", "total", ColKind.Text, 13, 110))
        doc.Columns.Add(New ReportColumn("HOURS", "hours", ColKind.Number, 8, 80))
        doc.Columns.Add(New ReportColumn("AVG HOURS / DAY", "avg_hours", ColKind.Number, 11, 110))
        doc.Columns.Add(New ReportColumn("SESSIONS", "sessions", ColKind.Number, 9, 90))
        doc.Columns.Add(New ReportColumn("FIRST IN", "first_in", ColKind.Stamp, 16, 160))
        doc.Columns.Add(New ReportColumn("LAST OUT", "last_out", ColKind.Stamp, 16, 160))
        doc.Data = dt
        doc.EmptyText = "Nobody logged in during this period."
        Return doc
    End Function

    ' =====================================================================================
    '  VIEW 3: every login session
    ' =====================================================================================
    Public Function Sessions(p As ReportPeriod, r As DateRange) As ReportDoc
        Dim doc As ReportDoc = NewDoc("Duty Records - Sessions", r)
        Dim segs As List(Of Seg) = LoadSegs(r)
        AddKpis(doc, BuildDays(segs, r))

        segs.Sort(Function(a As Seg, b As Seg) b.LoginAt.CompareTo(a.LoginAt))

        Dim dt As New DataTable()
        dt.Columns.Add("name", GetType(String))
        dt.Columns.Add("role", GetType(String))
        dt.Columns.Add("device", GetType(String))
        dt.Columns.Add("login_at", GetType(DateTime))
        dt.Columns.Add("ended_at", GetType(DateTime))
        dt.Columns.Add("total", GetType(String))
        dt.Columns.Add("hours", GetType(Decimal))
        dt.Columns.Add("status", GetType(String))
        Dim shown As Integer = 0
        For Each sg As Seg In segs
            If shown >= MaxRows Then
                doc.TruncatedNote = "Showing the latest " & MaxRows.ToString("N0", Inv) & " sessions of this period."
                Exit For
            End If
            Dim secs As Long = CLng((sg.EndAt - sg.LoginAt).TotalSeconds)
            dt.Rows.Add(sg.Name, sg.Role, sg.Device, sg.LoginAt, sg.EndAt, Duration(secs), Hours(secs), StatusText(sg.Status))
            shown += 1
        Next

        doc.TableTitle = "Every login (from login to logout)"
        doc.Columns.Add(New ReportColumn("PERSON", "name", ColKind.Text, 17, 140))
        doc.Columns.Add(New ReportColumn("ROLE", "role", ColKind.Text, 9, 90))
        doc.Columns.Add(New ReportColumn("PC", "device", ColKind.Text, 12, 110))
        doc.Columns.Add(New ReportColumn("LOGGED IN", "login_at", ColKind.Stamp, 16, 160))
        doc.Columns.Add(New ReportColumn("LOGGED OUT / LAST SEEN", "ended_at", ColKind.Stamp, 16, 170))
        doc.Columns.Add(New ReportColumn("TIME", "total", ColKind.Text, 11, 100))
        doc.Columns.Add(New ReportColumn("HOURS", "hours", ColKind.Number, 7, 80))
        doc.Columns.Add(New ReportColumn("STATUS", "status", ColKind.Text, 14, 130))
        doc.Data = dt
        doc.EmptyText = "Nobody logged in during this period."
        Return doc
    End Function

End Module
