Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>
''' Settings > VAT. Shows the current VAT rate and lets the Superadmin update it.
''' Selling prices already include VAT; the rate is only used to show the VAT inside a price.
''' </summary>
Public Class VatForm
    Inherits Form
    Implements ILiveForm

    Private Shared ReadOnly HugeFont As Font = Theme.MakeFont("Segoe UI Semibold", 54.0F)

    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()

    Private ReadOnly _body As New Panel()
    Private ReadOnly _card As New Panel()
    Private ReadOnly _caption As New Label()
    Private ReadOnly _lblPercent As New Label()
    Private ReadOnly _lblName As New Label()
    Private ReadOnly _lblUpdated As New Label()
    Private ReadOnly _input As New InputField("New VAT percentage (0 to 12)", InputKind.Money, 240, True)
    Private ReadOnly _btnUpdate As New MaroonButton()
    Private ReadOnly _msg As New Label()
    Private ReadOnly _note As New Label()

    Private _vatId As Integer
    Private _current As Decimal

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
        _title.Text = "VAT"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "The VAT rate of the system"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)

        _body.Dock = DockStyle.Fill
        _body.AutoScroll = True
        _body.BackColor = Color.White

        _card.Location = New Point(Theme.S(32), Theme.S(10))
        _card.Size = New Size(Theme.S(640), Theme.S(340))
        _card.BackColor = Color.White
        AddHandler _card.Paint, AddressOf OnCardPaint

        _caption.AutoSize = True
        _caption.Font = Theme.FontCaption
        _caption.ForeColor = Theme.TextMuted
        _caption.Text = "CURRENT VAT RATE"
        _caption.Location = New Point(Theme.S(30), Theme.S(24))

        _lblPercent.AutoSize = True
        _lblPercent.Font = HugeFont
        _lblPercent.ForeColor = Theme.Maroon
        _lblPercent.Location = New Point(Theme.S(24), Theme.S(44))

        _lblName.AutoSize = True
        _lblName.Font = Theme.FontHeading
        _lblName.ForeColor = Theme.TextDark
        _lblName.Location = New Point(Theme.S(30), Theme.S(134))

        _lblUpdated.AutoSize = True
        _lblUpdated.Font = Theme.FontSmall
        _lblUpdated.ForeColor = Theme.TextMuted
        _lblUpdated.Location = New Point(Theme.S(30), Theme.S(162))

        _input.Location = New Point(Theme.S(30), Theme.S(206))
        _input.MinNumber = 0D
        _input.MaxNumber = VatRepository.MaxVat
        _input.MaxLength = 5
        _input.SwallowEnter = True
        AddHandler _input.EnterPressed, AddressOf OnUpdate

        _btnUpdate.Text = "Update VAT"
        _btnUpdate.Size = New Size(Theme.S(160), Theme.S(44))
        _btnUpdate.Location = New Point(Theme.S(30) + Theme.S(240) + Theme.S(16), Theme.S(228))
        AddHandler _btnUpdate.Click, AddressOf OnUpdate

        _msg.AutoSize = False
        _msg.Size = New Size(Theme.S(580), Theme.S(36))
        _msg.Font = Theme.FontSmall
        _msg.Location = New Point(Theme.S(30), Theme.S(288))

        _card.Controls.Add(_caption)
        _card.Controls.Add(_lblPercent)
        _card.Controls.Add(_lblName)
        _card.Controls.Add(_lblUpdated)
        _card.Controls.Add(_input)
        _card.Controls.Add(_btnUpdate)
        _card.Controls.Add(_msg)

        _note.AutoSize = False
        _note.Location = New Point(Theme.S(34), Theme.S(366))
        _note.Size = New Size(Theme.S(620), Theme.S(300))
        _note.Font = Theme.FontBase
        _note.ForeColor = Theme.TextMuted
        _note.Text = "The standard VAT in the Philippines is 12%, the highest rate the BIR allows, so the system accepts 0 to 12. Use 0 if the shop does not charge VAT." &
                     Environment.NewLine & Environment.NewLine &
                     "Selling prices you enter in a delivery already include VAT. The system never adds VAT on top; it only shows how much of a price is VAT." &
                     Environment.NewLine & Environment.NewLine &
                     "Changing the rate affects what is shown from now on. Past sales keep the rate they were made with."

        _body.Controls.Add(_card)
        _body.Controls.Add(_note)

        Controls.Add(_body)
        Controls.Add(_header)
    End Sub

    Public ReadOnly Property WatchedTables As String() Implements ILiveForm.WatchedTables
        Get
            Return New String() {"vat_rates"}
        End Get
    End Property

    Public Sub RefreshData() Implements ILiveForm.RefreshData
        LoadVat()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadVat()
    End Sub

    Private Sub OnCardPaint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, _card.Width - 1, _card.Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(rect, 14)
            Using pen As New Pen(Theme.BorderColor, 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using
        Using pen As New Pen(Color.FromArgb(238, 238, 238), 1.0F)
            g.DrawLine(pen, Theme.S(30), Theme.S(192), _card.Width - Theme.S(30), Theme.S(192))
        End Using
    End Sub

    Private Sub LoadVat()
        Try
            Dim r As DataRow = VatRepository.GetActive()
            If r Is Nothing Then
                _vatId = 0
                _current = 0D
                _lblPercent.Text = "Not set"
                _lblName.Text = "No VAT rate yet. Enter it below."
                _lblUpdated.Text = ""
                _btnUpdate.Text = "Set VAT"
            Else
                _vatId = Convert.ToInt32(r("id"))
                _current = Convert.ToDecimal(r("percentage"))
                _lblPercent.Text = Percent.Format(_current)
                _lblName.Text = r("name").ToString() & "  (the rate the system uses)"
                _lblUpdated.Text = "Last changed " & Convert.ToDateTime(r("updated_at")).ToString("MMM d, yyyy  h:mm tt")
                _btnUpdate.Text = "Update VAT"
            End If
        Catch ex As Exception
            _msg.ForeColor = Theme.AlertRed
            _msg.Text = "Could not load the VAT rate: " & ex.Message
        End Try
    End Sub

    Private Sub OnUpdate(sender As Object, e As EventArgs)
        OnUpdate()
    End Sub

    Private Sub OnUpdate()
        _msg.Text = ""
        Dim problem As String = InputField.CheckAll(_input)
        If problem.Length > 0 Then
            _msg.ForeColor = Theme.AlertRed
            _msg.Text = problem
            Return
        End If

        Dim pct As Decimal
        If Not Decimal.TryParse(_input.Value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, pct) Then Return

        If _vatId > 0 AndAlso pct = _current Then
            _msg.ForeColor = Theme.AlertRed
            _msg.Text = "That is already the current rate."
            Return
        End If

        Dim question As String = If(_vatId > 0,
            "Change the VAT rate from " & Percent.Format(_current) & " to " & Percent.Format(pct) & "?",
            "Set the VAT rate to " & Percent.Format(pct) & "?")
        If Not Confirm(Me, "VAT", question & Environment.NewLine & Environment.NewLine &
                       "Past sales keep the rate they were made with.", MessageBoxIcon.Question) Then Return

        Try
            Dim result As SaveResult = If(_vatId > 0, VatRepository.SetPercentage(_vatId, pct), VatRepository.CreateActive(pct))
            If result = SaveResult.Ok Then
                AuditLog.Write("EDIT", "VAT", If(_vatId > 0, "VAT rate changed from " & Percent.Format(_current) & " to " & Percent.Format(pct),
                                                  "VAT rate set to " & Percent.Format(pct)))
                _input.Value = ""
                LoadVat()
                _msg.ForeColor = Color.FromArgb(22, 130, 60)
                _msg.Text = "VAT updated to " & Percent.Format(pct) & "."
            Else
                _msg.ForeColor = Theme.AlertRed
                _msg.Text = "Could not save the VAT rate."
            End If
        Catch ex As Exception
            _msg.ForeColor = Theme.AlertRed
            _msg.Text = "Could not save: " & ex.Message
        End Try
    End Sub

End Class
