Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>Print the barcode of an item on the receipt printer: choose how many labels.</summary>
Public Class PrintBarcodeDialog
    Inherits ModalDialog

    Private ReadOnly _name As String
    Private ReadOnly _size As String
    Private ReadOnly _code As String
    Private ReadOnly _preview As New LabelPreview()
    Private ReadOnly _copies As New InputField("How many labels?", InputKind.WholeNumber, 180, True)
    Private ReadOnly _hint As New Label()
    Private ReadOnly _print As New MaroonButton()
    Private ReadOnly _close As New MaroonButton()

    Public Sub New(itemName As String, itemSize As String, code As String)
        MyBase.New("Print barcode", (itemName & " " & itemSize).Trim() & "  -  " & code, 500, 600)
        _name = itemName
        _size = itemSize
        _code = code

        _preview.Location = New Point(Theme.S(40), Theme.S(108))
        _preview.Size = New Size(Theme.S(420), Theme.S(270))
        _preview.ItemName = itemName
        _preview.ItemSize = itemSize
        _preview.Code = code

        _copies.Location = New Point(Theme.S(40), Theme.S(394))
        _copies.MinNumber = 1D
        _copies.MaxNumber = 500D
        _copies.Value = "1"
        _copies.MaxLength = 3

        _hint.AutoSize = False
        _hint.Location = New Point(Theme.S(240), Theme.S(416))
        _hint.Size = New Size(Theme.S(220), Theme.S(44))
        _hint.Font = Theme.FontSmall
        _hint.ForeColor = Theme.TextMuted
        _hint.Text = "Prints on the default printer (the 58 mm receipt roll). Tear along the dotted line."

        Message.Location = New Point(Theme.S(40), Theme.S(470))
        Message.Size = New Size(Theme.S(420), Theme.S(30))

        _close.Text = "Close"
        _close.MakeSecondary()
        _close.Size = New Size(Theme.S(190), Theme.S(48))
        _close.Location = New Point(Theme.S(40), Theme.S(506))
        AddHandler _close.Click, AddressOf OnClose

        _print.Text = "Print"
        _print.Size = New Size(Theme.S(190), Theme.S(48))
        _print.Location = New Point(Theme.S(270), Theme.S(506))
        AddHandler _print.Click, AddressOf OnPrint2

        Controls.Add(_preview)
        Controls.Add(_copies)
        Controls.Add(_hint)
        Controls.Add(_close)
        Controls.Add(_print)
        ClientSize = New Size(Theme.S(500), Theme.S(506) + Theme.S(48) + Theme.S(24))
        AcceptButton = _print
        CancelButton = _close

        If Not Code128.CanEncode(code) Then
            _print.Enabled = False
            ShowMessage("This item's barcode has characters that cannot be printed.", True)
        End If
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _copies.FocusInput()
    End Sub

    Private Sub OnClose(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnPrint2(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Dim problem As String = InputField.CheckAll(_copies)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        Dim n As Integer = Integer.Parse(_copies.Value.Trim(), CultureInfo.InvariantCulture)
        _print.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            BarcodeLabels.PrintLabels(_name, _size, _code, n)
            AuditLog.Write("PRINT_BARCODE", "Products", "Printed " & n.ToString() & " barcode label(s): " & (_name & " " & _size).Trim() & " (" & _code & ")")
            ShowMessage("Sent " & n.ToString() & " label(s) to the printer.", False)
        Catch ex As Exception
            ShowMessage("Could not print: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            _print.Enabled = True
        End Try
    End Sub
End Class
