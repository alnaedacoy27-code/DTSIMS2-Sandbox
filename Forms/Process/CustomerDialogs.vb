Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>Add or edit a customer: name, mobile number (for the text messages) and address.</summary>
Public Class CustomerEditForm
    Inherits ModalDialog

    Private ReadOnly _isEdit As Boolean
    Private ReadOnly _id As Integer
    Private ReadOnly _name As New InputField("Customer name", InputKind.PersonName, 440, True)
    Private ReadOnly _contact As New InputField("Mobile number (for text messages)", InputKind.MobilePH, 300, True)
    Private ReadOnly _address As New InputField("Address", InputKind.Address, 440, True)
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    ''' <summary>The id of the customer that was just added.</summary>
    Public Property CreatedId As Integer

    Public Sub New()
        MyBase.New("Add customer", "A loyalty card with a barcode is made for every customer.", 520, 500)
        _isEdit = False
        _id = 0
        Build()
    End Sub

    Public Sub New(customerId As Integer)
        MyBase.New("Edit customer", "Change the customer's details. The card number stays the same.", 520, 500)
        _isEdit = True
        _id = customerId
        Build()

        Dim row As DataRow = Nothing
        Try
            row = CustomerRepository.GetOne(customerId)
        Catch ex As Exception
            ShowMessage("Could not load this customer: " & ex.Message, True)
        End Try
        If row Is Nothing Then
            _save.Enabled = False
            If Message.Text.Length = 0 Then ShowMessage("This customer no longer exists.", True)
            Return
        End If
        _name.Value = row("name").ToString()
        _contact.Value = row("contact_number").ToString()
        _address.Value = row("address").ToString()
    End Sub

    Private Sub Build()
        _name.Location = New Point(Theme.S(40), Theme.S(110))
        _contact.Location = New Point(Theme.S(40), Theme.S(184))
        _address.Location = New Point(Theme.S(40), Theme.S(258))

        Message.Location = New Point(Theme.S(40), Theme.S(340))
        Message.Size = New Size(Theme.S(440), Theme.S(34))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(130), Theme.S(44))
        _cancel.Location = New Point(Theme.S(220), Theme.S(384))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = If(_isEdit, "Save changes", "Add customer")
        _save.Size = New Size(Theme.S(130), Theme.S(44))
        _save.Location = New Point(Theme.S(360), Theme.S(384))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_name)
        Controls.Add(_contact)
        Controls.Add(_address)
        Controls.Add(_cancel)
        Controls.Add(_save)
        ClientSize = New Size(Theme.S(520), Theme.S(384) + Theme.S(44) + Theme.S(28))
        AcceptButton = _save
        CancelButton = _cancel
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _name.FocusInput()
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Dim problem As String = InputField.CheckAll(_name, _contact, _address)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        _save.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim result As SaveResult
            If _isEdit Then
                result = CustomerRepository.Update(_id, _name.Value, _contact.Value, _address.Value)
            Else
                Dim newId As Integer
                result = CustomerRepository.Create(_name.Value, _contact.Value, _address.Value, newId)
                CreatedId = newId
            End If

            Select Case result
                Case SaveResult.Ok
                    AuditLog.Write(If(_isEdit, "EDIT", "ADD"), "Customers", If(_isEdit, "Edited", "Added") & " customer '" & _name.Value.Trim() & "'")
                    If Not _isEdit Then SendWelcome(CreatedId)
                    DialogResult = DialogResult.OK
                Case SaveResult.NotFound
                    ShowMessage("This customer no longer exists.", True)
                Case SaveResult.Invalid
                    ShowMessage("Fill in the name, the mobile number and the address.", True)
                Case Else
                    ShowMessage("Could not save this customer.", True)
            End Select
        Catch ex As Exception
            ShowMessage("Could not save: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            If DialogResult <> DialogResult.OK Then _save.Enabled = True
        End Try
    End Sub

    ' the welcome text message: if it cannot be queued the customer is still saved
    Private Sub SendWelcome(id As Integer)
        Try
            Dim row As DataRow = CustomerRepository.GetOne(id)
            If row IsNot Nothing Then
                SmsService.QueueSms(row("contact_number").ToString(),
                                    SmsService.WelcomeText(row("name").ToString(), row("card_number").ToString(), Convert.ToDecimal(row("points_balance"))))
            End If
        Catch
            ' ignore
        End Try
    End Sub
End Class

''' <summary>The loyalty card at its real size: save it as a PDF (or picture) or print it.</summary>
Public Class CardDialog
    Inherits ModalDialog

    Private ReadOnly _model As New CardModel()
    Private ReadOnly _preview As New CardPreview()
    Private ReadOnly _pdf As New MaroonButton()
    Private ReadOnly _print As New MaroonButton()
    Private ReadOnly _close As New MaroonButton()

    Public Sub New(customerId As Integer)
        MyBase.New("Loyalty card", "Real card size: 85.6 x 54 mm", 560, 520)

        Dim row As DataRow = Nothing
        Try
            row = CustomerRepository.GetOne(customerId)
        Catch ex As Exception
            ShowMessage("Could not load this customer: " & ex.Message, True)
        End Try
        If row IsNot Nothing Then
            _model.CustomerName = row("name").ToString()
            _model.CardNumber = row("card_number").ToString()
            _model.ShopName = PosRepository.ShopSetting("shop_name", "DARWIN TRADING")
        End If

        _preview.Location = New Point(Theme.S(40), Theme.S(104))
        _preview.Size = New Size(Theme.S(480), Theme.S(300))
        _preview.Model = _model

        Message.Location = New Point(Theme.S(40), Theme.S(412))
        Message.Size = New Size(Theme.S(480), Theme.S(30))

        _pdf.Text = "Save as PDF"
        _pdf.Size = New Size(Theme.S(170), Theme.S(48))
        _pdf.Location = New Point(Theme.S(40), Theme.S(452))
        AddHandler _pdf.Click, AddressOf OnSave

        _print.Text = "Print card"
        _print.MakeSecondary()
        _print.Size = New Size(Theme.S(150), Theme.S(48))
        _print.Location = New Point(Theme.S(220), Theme.S(452))
        AddHandler _print.Click, AddressOf OnPrintCard

        _close.Text = "Close"
        _close.MakeSecondary()
        _close.Size = New Size(Theme.S(130), Theme.S(48))
        _close.Location = New Point(Theme.S(390), Theme.S(452))
        AddHandler _close.Click, AddressOf OnClose

        Controls.Add(_preview)
        Controls.Add(_pdf)
        Controls.Add(_print)
        Controls.Add(_close)
        ClientSize = New Size(Theme.S(560), Theme.S(452) + Theme.S(48) + Theme.S(24))
        CancelButton = _close

        If row Is Nothing Then
            _pdf.Enabled = False
            _print.Enabled = False
        End If
    End Sub

    Private Sub OnClose(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Function SafeFileName(name As String) As String
        Dim bad As Char() = Path.GetInvalidFileNameChars()
        Dim sb As New System.Text.StringBuilder()
        For Each c As Char In name
            sb.Append(If(Array.IndexOf(bad, c) >= 0, "_"c, c))
        Next
        Return sb.ToString().Trim()
    End Function

    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Using dlg As New SaveFileDialog()
            dlg.Title = "Save the loyalty card"
            dlg.Filter = "PDF file (*.pdf)|*.pdf|PNG picture (*.png)|*.png"
            dlg.FileName = "Loyalty Card - " & SafeFileName(_model.CustomerName)
            dlg.DefaultExt = "pdf"
            dlg.AddExtension = True
            dlg.OverwritePrompt = True
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Cursor = Cursors.WaitCursor
            Try
                If Path.GetExtension(dlg.FileName).ToLowerInvariant() = ".png" Then
                    CardRenderer.SavePng(_model, dlg.FileName)
                Else
                    CardRenderer.SavePdf(_model, dlg.FileName)
                End If
                AuditLog.Write("CARD_SAVED", "Customers", "Saved the loyalty card of '" & _model.CustomerName & "' to a file")
                ShowMessage("Saved: " & dlg.FileName, False)
            Catch ex As Exception
                ShowMessage("Could not save the file: " & ex.Message, True)
            Finally
                Cursor = Cursors.Default
            End Try
        End Using
    End Sub

    Private Sub OnPrintCard(sender As Object, e As EventArgs)
        ShowMessage("", False)
        Cursor = Cursors.WaitCursor
        Try
            CardRenderer.PrintCard(_model)
            AuditLog.Write("CARD_PRINTED", "Customers", "Printed the loyalty card of '" & _model.CustomerName & "'")
            ShowMessage("Sent to the printer (card size 85.6 x 54 mm).", False)
        Catch ex As Exception
            ShowMessage("Could not print: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub
End Class
