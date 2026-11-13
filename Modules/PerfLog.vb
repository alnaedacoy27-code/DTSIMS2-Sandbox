Option Strict On
Option Explicit On

''' <summary>
''' Tiny timing log: PerfLog.Mark("what just finished") appends "time since start / since last mark"
''' to %APPDATA%\DTSIMS2-Sandbox\perf.log, so a slow step shows up as a big number. Never throws.
''' </summary>
Public Module PerfLog

    Private ReadOnly _clock As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
    Private _last As Long = 0
    Private ReadOnly _lock As New Object()

    Public Sub Mark(label As String)
        Try
            SyncLock _lock
                Dim elapsed As Long = _clock.ElapsedMilliseconds
                Dim line As String = DateTime.Now.ToString("HH:mm:ss.fff") & "  +" & (elapsed - _last).ToString().PadLeft(6) & " ms   (total " &
                                     elapsed.ToString().PadLeft(6) & " ms)   " & label
                _last = elapsed
                Dim dir As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DTSIMS2-Sandbox")
                System.IO.Directory.CreateDirectory(dir)
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "perf.log"), line & Environment.NewLine)
            End SyncLock
        Catch
            ' timing must never break the app
        End Try
    End Sub

End Module
