Option Strict On
Option Explicit On

Imports System.Windows.Forms

Public Module Program

    <STAThread()>
    Public Sub Main()
        PerfLog.Mark("---- app started")
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Runs the whole startup again after "Log out" or when the Superadmin was deleted.
        Do
            If Not EnsureDatabaseReady() Then Exit Do      ' 1. can we reach dtsims2_db?
            PerfLog.Mark("database checked")
            If Not EnsureSuperadmin() Then Exit Do         ' 2. is there a Superadmin? (else: setup form)
            PerfLog.Mark("superadmin checked")
            If Not RunSession() Then Exit Do               ' 3. login -> main window
        Loop
    End Sub

    ''' <returns>True when the startup should run again (logged out, or Superadmin must be recreated).</returns>
    Private Function RunSession() As Boolean
        Using login As New LoginForm()
            If login.ShowDialog() <> DialogResult.OK Then Return False
        End Using

        PerfLog.Mark("login accepted")
        SessionTracker.StartSession()
        PerfLog.Mark("session started")
        Dim mainForm As New MainMenuForm()
        PerfLog.Mark("main window built")
        Application.Run(mainForm)
        Dim again As Boolean = mainForm.LoggedOut OrElse mainForm.RequireSetup
        mainForm.Dispose()
        SessionTracker.EndSession()

        AppSession.Clear()
        Return again
    End Function

    Private Function EnsureDatabaseReady() As Boolean
        Db.Configure(ConnectionSettings.Load())

        Do
            Dim problem As String = ""
            If Db.CheckStatus(problem) = DbStatus.Ok Then Return True

            Using f As New ConnectionSettingsForm(problem)
                If f.ShowDialog() <> DialogResult.OK Then Return False
            End Using
        Loop
    End Function

    Private Function EnsureSuperadmin() As Boolean
        Try
            If AuthService.SuperadminExists() Then Return True
        Catch ex As Exception
            MessageBox.Show("Could not check for the Superadmin account:" & Environment.NewLine & ex.Message,
                            "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try

        Using f As New SetupSuperadminForm()
            Return f.ShowDialog() = DialogResult.OK
        End Using
    End Function

End Module
