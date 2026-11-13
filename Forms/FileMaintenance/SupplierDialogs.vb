Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Add or edit a supplier (a company branch).</summary>
Public Class SupplierEditForm
    Inherits ModalDialog

    Private ReadOnly _supplierId As Integer
    Private ReadOnly _isEdit As Boolean
    Private ReadOnly _company As New InputField("Company name", InputKind.Title, 480, True)
    Private ReadOnly _person As New InputField("Contact person", InputKind.PersonName, 233, True)
    Private ReadOnly _number As New InputField("Contact number", InputKind.MobilePH, 233, True)
    Private ReadOnly _email As New InputField("Email", InputKind.Email, 480, False)
    Private ReadOnly _address As New InputField("Company address (this branch)", InputKind.Address, 480, True)
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    ''' <summary>Add a new supplier. Pass a template to prefill a new branch of an existing company.</summary>
    Public Sub New(Optional company As String = "", Optional person As String = "",
                   Optional number As String = "", Optional email As String = "")
        MyBase.New("Add supplier", "A company and the person you deal with. Each branch needs its own address.", 560, 540)
        _isEdit = False
        _supplierId = 0
        Build()
        _company.Value = company
        _person.Value = person
        _number.Value = number
        _email.Value = email
    End Sub

    ''' <summary>Edit an existing supplier.</summary>
    Public Sub New(supplierId As Integer)
        MyBase.New("Edit supplier", "Change the details of this supplier.", 560, 540)
        _isEdit = True
        _supplierId = supplierId
        Build()

        Dim row As DataRow = Nothing
        Try
            row = SupplierRepository.GetSupplier(supplierId)
        Catch ex As Exception
            ShowMessage("Could not load this supplier: " & ex.Message, True)
        End Try
        If row Is Nothing Then
            _save.Enabled = False
            If Message.Text.Length = 0 Then ShowMessage("This supplier no longer exists.", True)
            Return
        End If
        _company.Value = row("company_name").ToString()
        _person.Value = row("name").ToString()
        _number.Value = If(row("contact_number") Is DBNull.Value, "", row("contact_number").ToString())
        _email.Value = If(row("email") Is DBNull.Value, "", row("email").ToString())
        _address.Value = row("company_address").ToString()
    End Sub

    Private Sub Build()
        _company.Location = New Point(Theme.S(40), Theme.S(110))
        _person.Location = New Point(Theme.S(40), Theme.S(184))
        _number.Location = New Point(Theme.S(287), Theme.S(184))
        _email.Location = New Point(Theme.S(40), Theme.S(258))
        _address.Location = New Point(Theme.S(40), Theme.S(332))

        Message.Location = New Point(Theme.S(40), Theme.S(412))
        Message.Size = New Size(Theme.S(480), Theme.S(34))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(140), Theme.S(44))
        _cancel.Location = New Point(Theme.S(220), Theme.S(456))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = If(_isEdit, "Save changes", "Add supplier")
        _save.Size = New Size(Theme.S(140), Theme.S(44))
        _save.Location = New Point(Theme.S(380), Theme.S(456))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_company)
        Controls.Add(_person)
        Controls.Add(_number)
        Controls.Add(_email)
        Controls.Add(_address)
        Controls.Add(_cancel)
        Controls.Add(_save)
        ClientSize = New Size(Theme.S(560), Theme.S(456) + Theme.S(44) + Theme.S(28))

        AcceptButton = _save
        CancelButton = _cancel
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _company.Value.Length = 0 Then
            _company.FocusInput()
        ElseIf _address.Value.Length = 0 Then
            _address.FocusInput()
        Else
            _company.FocusInput()
        End If
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        ShowMessage("", False)

        Dim problem As String = InputField.CheckAll(_company, _person, _number, _email, _address)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        _save.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim result As SaveResult
            If _isEdit Then
                result = SupplierRepository.UpdateSupplier(_supplierId, _person.Value, _company.Value, _number.Value, _address.Value, _email.Value)
            Else
                result = SupplierRepository.CreateSupplier(_person.Value, _company.Value, _number.Value, _address.Value, _email.Value)
            End If

            Select Case result
                Case SaveResult.Ok
                    AuditLog.Write(If(_isEdit, "EDIT", "ADD"), "Suppliers",
                                   If(_isEdit, "Edited", "Added") & " supplier '" & _company.Value.Trim() & "' (" & _address.Value.Trim() & ")")
                    DialogResult = DialogResult.OK
                Case SaveResult.Duplicate
                    ShowMessage("Another supplier already uses this address. Branches can share the person, company and phone number, but each needs its own address.", True)
                    _address.FocusInput()
                Case SaveResult.NotFound
                    ShowMessage("This supplier no longer exists.", True)
                Case Else
                    ShowMessage("Could not save this supplier.", True)
            End Select
        Catch ex As Exception
            ShowMessage("Could not save: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            If DialogResult <> DialogResult.OK Then _save.Enabled = True
        End Try
    End Sub
End Class
