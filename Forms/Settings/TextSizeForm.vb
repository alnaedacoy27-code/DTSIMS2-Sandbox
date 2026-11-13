Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' Settings > Text Size. Pick Default, Medium or Large for THIS computer. Only text changes
''' (cards, inputs and buttons keep their size). The app restarts to apply it.
''' </summary>
Public Class TextSizeForm
    Inherits Form

    ' ---------------------------------------------------------------- one selectable size card
    Private Class SizeOption
        Inherits Control

        Public Property OptionName As String = ""
        Public Property Caption As String = ""
        Public Property Factor As Single = 1.0F
        Public Property Selected As Boolean

        Private _hover As Boolean

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                     ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            Size = New Size(Theme.S(210), Theme.S(116))
            Cursor = Cursors.Hand
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            _hover = True
            Invalidate()
            MyBase.OnMouseEnter(e)
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            _hover = False
            Invalidate()
            MyBase.OnMouseLeave(e)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(If(Parent IsNot Nothing, Parent.BackColor, Color.White))

            Dim rect As New Rectangle(1, 1, Width - Theme.S(3), Height - Theme.S(3))
            Using path As GraphicsPath = Theme.RoundedRect(rect, 12)
                Using fill As New SolidBrush(If(Selected OrElse _hover, Theme.MaroonTint, Color.White))
                    g.FillPath(fill, path)
                End Using
                Using pen As New Pen(If(Selected, Theme.Maroon, If(_hover, Theme.MaroonLight, Theme.BorderColor)), If(Selected, 2.0F, 1.0F))
                    g.DrawPath(pen, path)
                End Using
            End Using

            Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding
            Using f1 As New Font("Segoe UI Semibold", 12.0F * Factor)
                TextRenderer.DrawText(g, OptionName, f1, New Rectangle(Theme.S(18), Theme.S(14), Width - Theme.S(36), Theme.S(36)), Theme.Maroon, flags)
            End Using
            Using f2 As New Font("Segoe UI", 10.0F * Factor)
                TextRenderer.DrawText(g, Caption, f2, New Rectangle(Theme.S(18), Theme.S(56), Width - Theme.S(36), Theme.S(44)), Theme.TextMuted, flags)
            End Using
        End Sub
    End Class

    ' ---------------------------------------------------------------- screen
    Private ReadOnly _header As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _subtitle As New Label()

    Private ReadOnly _body As New Panel()
    Private ReadOnly _options As New List(Of SizeOption)()
    Private ReadOnly _previewCaption As New Label()
    Private ReadOnly _previewCard As New Panel()
    Private ReadOnly _flow As New FlowLayoutPanel()
    Private ReadOnly _pvHead As New Label()
    Private ReadOnly _pvBody As New Label()
    Private ReadOnly _pvMuted As New Label()
    Private ReadOnly _btnApply As New MaroonButton()
    Private ReadOnly _msg As New Label()
    Private ReadOnly _note As New Label()

    Private _selected As String = "Default"

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
        _title.Text = "Text Size"
        _title.Location = New Point(Theme.S(32), Theme.S(16))

        _subtitle.AutoSize = True
        _subtitle.Font = Theme.FontBase
        _subtitle.ForeColor = Theme.TextMuted
        _subtitle.Text = "Make the text bigger or smaller on this computer"
        _subtitle.Location = New Point(Theme.S(35), Theme.S(62))

        _header.Controls.Add(_title)
        _header.Controls.Add(_subtitle)

        _body.Dock = DockStyle.Fill
        _body.BackColor = Color.White
        _body.AutoScroll = True

        Dim captions As String() = {"Standard size", "A little bigger", "The biggest"}
        For i As Integer = 0 To TextSize.Names.Length - 1
            Dim opt As New SizeOption()
            opt.OptionName = TextSize.Names(i)
            opt.Caption = captions(i)
            opt.Factor = TextSize.ScaleFor(TextSize.Names(i))
            opt.Location = New Point(Theme.S(32) + i * Theme.S(226), Theme.S(12))
            AddHandler opt.Click, AddressOf OnOptionClick
            _options.Add(opt)
            _body.Controls.Add(opt)
        Next

        _previewCaption.AutoSize = True
        _previewCaption.Font = Theme.FontCaption
        _previewCaption.ForeColor = Theme.TextMuted
        _previewCaption.Text = "PREVIEW"
        _previewCaption.Location = New Point(Theme.S(34), Theme.S(152))

        _previewCard.Location = New Point(Theme.S(32), Theme.S(178))
        _previewCard.Size = New Size(Theme.S(662), Theme.S(160))
        _previewCard.BackColor = Color.White
        AddHandler _previewCard.Paint, AddressOf OnPreviewPaint

        _flow.Dock = DockStyle.Fill
        _flow.FlowDirection = FlowDirection.TopDown
        _flow.WrapContents = False
        _flow.Padding = New Padding(Theme.S(22), Theme.S(16), Theme.S(10), Theme.S(10))
        _flow.BackColor = Color.White

        For Each l As Label In New Label() {_pvHead, _pvBody, _pvMuted}
            l.AutoSize = True
            l.Margin = New Padding(0, 0, 0, Theme.S(8))
            _flow.Controls.Add(l)
        Next
        _pvHead.ForeColor = Theme.Maroon
        _pvHead.Text = "Products and Inventory"
        _pvBody.ForeColor = Theme.TextDark
        _pvBody.Text = "burger 10mm     " & ChrW(&H20B1) & "120.00     16 left"
        _pvMuted.ForeColor = Theme.TextMuted
        _pvMuted.Text = "Stock value (at cost)  |  Last delivery Nov 30, 2026"
        _previewCard.Controls.Add(_flow)

        _btnApply.Text = "Apply and restart"
        _btnApply.Size = New Size(Theme.S(200), Theme.S(44))
        _btnApply.Location = New Point(Theme.S(32), Theme.S(356))
        AddHandler _btnApply.Click, AddressOf OnApply

        _msg.AutoSize = False
        _msg.Size = New Size(Theme.S(440), Theme.S(44))
        _msg.Font = Theme.FontSmall
        _msg.ForeColor = Theme.TextMuted
        _msg.TextAlign = ContentAlignment.MiddleLeft
        _msg.Location = New Point(Theme.S(248), Theme.S(356))

        _note.AutoSize = False
        _note.Location = New Point(Theme.S(34), Theme.S(420))
        _note.Size = New Size(Theme.S(660), Theme.S(120))
        _note.Font = Theme.FontBase
        _note.ForeColor = Theme.TextMuted
        _note.Text = "This changes the text only. Cards, buttons and boxes keep their size." &
                     Environment.NewLine & Environment.NewLine &
                     "It applies to this computer only, so each laptop can use the size that suits its screen. " &
                     "The system restarts to apply it and you log in again."

        _body.Controls.Add(_previewCaption)
        _body.Controls.Add(_previewCard)
        _body.Controls.Add(_btnApply)
        _body.Controls.Add(_msg)
        _body.Controls.Add(_note)

        Controls.Add(_body)
        Controls.Add(_header)

        _selected = TextSize.Current()
        UpdateSelection()
    End Sub

    Private Sub OnOptionClick(sender As Object, e As EventArgs)
        Dim opt As SizeOption = TryCast(sender, SizeOption)
        If opt Is Nothing Then Return
        _selected = opt.OptionName
        UpdateSelection()
    End Sub

    Private Sub UpdateSelection()
        For Each o As SizeOption In _options
            o.Selected = String.Equals(o.OptionName, _selected, StringComparison.Ordinal)
            o.Invalidate()
        Next

        Dim s As Single = TextSize.ScaleFor(_selected)
        SetOwnFont(_pvHead, "Segoe UI Semibold", 12.0F * s)
        SetOwnFont(_pvBody, "Segoe UI", 10.0F * s)
        SetOwnFont(_pvMuted, "Segoe UI", 9.0F * s)

        Dim changed As Boolean = Not String.Equals(_selected, TextSize.Current(), StringComparison.Ordinal)
        _btnApply.Enabled = changed
        _msg.Text = If(changed, "Selected: " & _selected & ". Click Apply to use it.", "This is the size in use now (" & TextSize.Current() & ").")
    End Sub

    ''' <summary>Sets a font on a label and disposes the one it replaces.</summary>
    Private Shared Sub SetOwnFont(lbl As Label, family As String, size As Single)
        Dim old As Font = TryCast(lbl.Tag, Font)
        Dim f As New Font(family, size)
        lbl.Font = f
        lbl.Tag = f
        If old IsNot Nothing Then old.Dispose()
    End Sub

    Private Sub OnPreviewPaint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, _previewCard.Width - 1, _previewCard.Height - 1)
        Using path As GraphicsPath = Theme.RoundedRect(rect, 14)
            Using pen As New Pen(Theme.BorderColor, 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    Private Sub OnApply(sender As Object, e As EventArgs)
        If String.Equals(_selected, TextSize.Current(), StringComparison.Ordinal) Then Return

        If Not Confirm(Me, "Text Size", "Change the text size to " & _selected & "?" & Environment.NewLine & Environment.NewLine &
                       "The system will restart and you will need to log in again.", MessageBoxIcon.Question) Then Return

        Try
            TextSize.Save(_selected)
        Catch ex As Exception
            _msg.ForeColor = Theme.AlertRed
            _msg.Text = "Could not save the setting: " & ex.Message
            Return
        End Try

        Try
            AuditLog.Write("EDIT", "Text Size", "Text size on this computer set to " & _selected)
        Catch
            ' the audit trail must never stop the change
        End Try

        Application.Restart()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            For Each l As Label In New Label() {_pvHead, _pvBody, _pvMuted}
                Dim f As Font = TryCast(l.Tag, Font)
                If f IsNot Nothing Then f.Dispose()
            Next
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class
