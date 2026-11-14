Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Shows a past sale's receipt exactly as it printed and lets you print a copy. The copy is stamped
''' "REPRINT / COPY" so it cannot be mistaken for the original, and every reprint is written to the audit trail.
''' </summary>
Public Class ReceiptCopyDialog
    Inherits Form

    ' the receipt drawn on screen with the same code that prints it
    Private Class PaperPanel
        Inherits Control

        Private ReadOnly _model As ReceiptModel
        Private ReadOnly _k As Single
        Private ReadOnly _pad As Integer = Theme.S(16)

        Public Sub New(model As ReceiptModel, scale As Single)
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)
            _model = model
            _k = scale
            BackColor = Color.White

            Dim h As Single
            Using bmp As New Bitmap(1, 1)
                Using g As Graphics = Graphics.FromImage(bmp)
                    h = ReceiptRenderer.Render(g, model, ReceiptRenderer.PaperWidth, False)
                End Using
            End Using
            Size = New Size(CInt(ReceiptRenderer.PaperWidth * _k) + _pad * 2, CInt((h + 6.0F) * _k) + _pad * 2)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            g.Clear(Color.White)
            g.TranslateTransform(_pad, _pad)
            g.ScaleTransform(_k, _k)
            ReceiptRenderer.Render(g, _model, ReceiptRenderer.PaperWidth, True)
        End Sub
    End Class

    Private ReadOnly _model As ReceiptModel
    Private ReadOnly _top As New Panel()
    Private ReadOnly _title As New Label()
    Private ReadOnly _note As New Label()
    Private ReadOnly _scroll As New Panel()
    Private ReadOnly _paper As PaperPanel
    Private ReadOnly _bottom As New Panel()
    Private ReadOnly _status As New Label()
    Private ReadOnly _btnPrint As New MaroonButton()
    Private ReadOnly _btnClose As New MaroonButton()

    Public Sub New(model As ReceiptModel)
        _model = model
        _model.IsReprint = True

        Me.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        Text = "Receipt copy"
        BackColor = Color.White
        Font = Theme.FontBase
        ClientSize = New Size(Theme.S(480), Theme.S(740))

        _top.Dock = DockStyle.Top
        _top.Height = Theme.S(96)
        _top.BackColor = Color.White
        _title.AutoSize = True
        _title.Font = Theme.FontHeading
        _title.ForeColor = Theme.Maroon
        _title.Text = "Receipt " & model.ReceiptNo
        _title.Location = New Point(Theme.S(24), Theme.S(14))
        _note.AutoSize = False
        _note.Font = Theme.FontSmall
        _note.ForeColor = Theme.TextMuted
        _note.Text = "A copy of a past sale. The printout is marked REPRINT / COPY and the reprint is recorded." & vbCrLf &
                     "Prints on: " & If(LocalDevices.ReceiptPrinter.Length > 0, LocalDevices.ReceiptPrinter, "the Windows default printer")
        _note.Location = New Point(Theme.S(24), Theme.S(44))
        _note.Size = New Size(Theme.S(432), Theme.S(44))
        _top.Controls.Add(_title)
        _top.Controls.Add(_note)

        _bottom.Dock = DockStyle.Bottom
        _bottom.Height = Theme.S(76)
        _bottom.BackColor = Color.White
        _btnPrint.Text = "Print copy"
        _btnPrint.Size = New Size(Theme.S(190), Theme.S(46))
        _btnPrint.Location = New Point(Theme.S(24), Theme.S(16))
        _btnClose.Text = "Close"
        _btnClose.MakeSecondary()
        _btnClose.Size = New Size(Theme.S(120), Theme.S(46))
        _btnClose.Location = New Point(Theme.S(226), Theme.S(16))
        _status.AutoSize = False
        _status.Font = Theme.FontSmall
        _status.ForeColor = Theme.TextMuted
        _status.TextAlign = ContentAlignment.MiddleLeft
        _status.Location = New Point(Theme.S(358), Theme.S(16))
        _status.Size = New Size(Theme.S(110), Theme.S(46))
        _bottom.Controls.Add(_btnPrint)
        _bottom.Controls.Add(_btnClose)
        _bottom.Controls.Add(_status)
        AddHandler _btnPrint.Click, AddressOf OnPrintClick
        AddHandler _btnClose.Click, AddressOf OnCloseClick

        _scroll.Dock = DockStyle.Fill
        _scroll.AutoScroll = True
        _scroll.BackColor = Color.FromArgb(240, 236, 236)
        _paper = New PaperPanel(model, 1.7F * Theme.LayoutScale)
        _scroll.Controls.Add(_paper)
        AddHandler _scroll.Resize, AddressOf OnScrollResize
        OnScrollResize(Me, EventArgs.Empty)

        Controls.Add(_scroll)
        Controls.Add(_bottom)
        Controls.Add(_top)
        CancelButton = _btnClose
    End Sub

    Private Sub OnScrollResize(sender As Object, e As EventArgs)
        _paper.Location = New Point(Math.Max(Theme.S(12), (_scroll.ClientSize.Width - _paper.Width) \ 2) + _scroll.AutoScrollPosition.X, Theme.S(16) + _scroll.AutoScrollPosition.Y)
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        WindowStyle.ApplyMaroonTitleBar(Me)
    End Sub

    Private Sub OnCloseClick(sender As Object, e As EventArgs)
        Close()
    End Sub

    Private Sub OnPrintClick(sender As Object, e As EventArgs)
        Try
            ReceiptRenderer.PrintReceipt(_model)
        Catch ex As Exception
            MessageBox.Show(Me, "The receipt could not be printed." & vbCrLf & vbCrLf & ex.Message & vbCrLf & vbCrLf &
                            "Check the receipt printer in Utilities > Devices.", "Receipt copy", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try
        Try
            AuditLog.Write("REPRINT", "Sales Report", "Receipt copy printed: " & _model.ReceiptNo)
        Catch
            ' the audit trail must never stop a reprint
        End Try
        _status.Text = "Sent to the printer."
    End Sub

End Class
