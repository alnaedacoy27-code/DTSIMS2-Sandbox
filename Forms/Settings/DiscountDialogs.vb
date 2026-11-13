Option Strict On
Option Explicit On

Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>Add or edit a discount: just a name and a percentage.</summary>
Public Class DiscountEditForm
    Inherits ModalDialog

    Private ReadOnly _isEdit As Boolean
    Private ReadOnly _id As Integer
    Private ReadOnly _name As New InputField("Discount name", InputKind.Title, 440, True)
    Private ReadOnly _percent As New InputField("Discount percentage", InputKind.Money, 200, True)
    Private ReadOnly _hint As New Label()
    Private ReadOnly _save As New MaroonButton()
    Private ReadOnly _cancel As New MaroonButton()

    Public Sub New()
        MyBase.New("Add discount", "A name the cashier will see and how much it takes off.", 520, 400)
        _isEdit = False
        _id = 0
        Build()
    End Sub

    Public Sub New(discountId As Integer)
        MyBase.New("Edit discount", "Change the name or the percentage.", 520, 400)
        _isEdit = True
        _id = discountId
        Build()

        Dim row As DataRow = Nothing
        Try
            row = DiscountRepository.GetOne(discountId)
        Catch ex As Exception
            ShowMessage("Could not load this discount: " & ex.Message, True)
        End Try
        If row Is Nothing Then
            _save.Enabled = False
            If Message.Text.Length = 0 Then ShowMessage("This discount no longer exists.", True)
            Return
        End If
        _name.Value = row("name").ToString()
        _percent.Value = Convert.ToDecimal(row("percentage")).ToString("0.##", CultureInfo.InvariantCulture)
    End Sub

    Private Sub Build()
        _name.Location = New Point(Theme.S(40), Theme.S(110))
        _percent.Location = New Point(Theme.S(40), Theme.S(184))
        _percent.MinNumber = 0.01D
        _percent.MaxNumber = DiscountRepository.MaxPercent

        _hint.AutoSize = False
        _hint.Size = New Size(Theme.S(220), Theme.S(40))
        _hint.Location = New Point(Theme.S(260), Theme.S(208))
        _hint.Font = Theme.FontSmall
        _hint.ForeColor = Theme.TextMuted
        _hint.Text = "Type 10 for 10% off. The most allowed is 50%, with at most 2 decimals."

        Message.Location = New Point(Theme.S(40), Theme.S(262))
        Message.Size = New Size(Theme.S(440), Theme.S(34))

        _cancel.Text = "Cancel"
        _cancel.MakeSecondary()
        _cancel.Size = New Size(Theme.S(130), Theme.S(44))
        _cancel.Location = New Point(Theme.S(220), Theme.S(306))
        AddHandler _cancel.Click, AddressOf OnCancel

        _save.Text = If(_isEdit, "Save changes", "Add discount")
        _save.Size = New Size(Theme.S(130), Theme.S(44))
        _save.Location = New Point(Theme.S(360), Theme.S(306))
        AddHandler _save.Click, AddressOf OnSave

        Controls.Add(_name)
        Controls.Add(_percent)
        Controls.Add(_hint)
        Controls.Add(_cancel)
        Controls.Add(_save)
        ClientSize = New Size(Theme.S(520), Theme.S(306) + Theme.S(44) + Theme.S(28))
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

        Dim problem As String = InputField.CheckAll(_name, _percent)
        If problem.Length > 0 Then
            ShowMessage(problem, True)
            Return
        End If

        Dim pct As Decimal
        If Not Decimal.TryParse(_percent.Value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, pct) Then
            ShowMessage("Enter the percentage as a number.", True)
            Return
        End If

        _save.Enabled = False
        Cursor = Cursors.WaitCursor
        Try
            Dim result As SaveResult = If(_isEdit, DiscountRepository.Update(_id, _name.Value, pct), DiscountRepository.Create(_name.Value, pct))
            Select Case result
                Case SaveResult.Ok
                    AuditLog.Write(If(_isEdit, "EDIT", "ADD"), "Discounts",
                                   If(_isEdit, "Edited", "Added") & " discount '" & _name.Value.Trim() & "' (" & Percent.Format(pct) & ")")
                    DialogResult = DialogResult.OK
                Case SaveResult.Duplicate
                    ShowMessage("A discount with this name already exists.", True)
                    _name.FocusInput()
                Case SaveResult.NotFound
                    ShowMessage("This discount no longer exists.", True)
                Case SaveResult.Invalid
                    ShowMessage("Enter a name and a percentage between 0.01 and 50.", True)
                Case Else
                    ShowMessage("Could not save this discount.", True)
            End Select
        Catch ex As Exception
            ShowMessage("Could not save: " & ex.Message, True)
        Finally
            Cursor = Cursors.Default
            If DialogResult <> DialogResult.OK Then _save.Enabled = True
        End Try
    End Sub
End Class
