Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

''' <summary>What a text field is for. The kind decides which characters can be typed and what counts as valid.</summary>
Public Enum InputKind
    ''' <summary>Anything except control characters (notes, free text).</summary>
    Text
    ''' <summary>A person's name: letters, space, apostrophe, dot, dash.</summary>
    PersonName
    ''' <summary>Login name: letters, numbers, dot, dash, underscore (3 to 30).</summary>
    Username
    ''' <summary>Philippine mobile number: exactly 11 digits starting with 09.</summary>
    MobilePH
    ''' <summary>Any phone number (mobile or landline): digits, + - ( ) and spaces, 7 to 15 digits.</summary>
    Phone
    Email
    ''' <summary>Street address: letters, numbers and common punctuation.</summary>
    Address
    ''' <summary>Names of things: products, companies, categories.</summary>
    Title
    ''' <summary>New passwords: 8 to 64 characters with at least one letter and one number.</summary>
    Password
    ''' <summary>Whole numbers (quantities).</summary>
    WholeNumber
    ''' <summary>Money / decimals with up to 2 decimal places.</summary>
    Money
    ' Size of a product: 2x2x3mm, 10mm x 6m, 4x8 0.4mm ...
    Measurement
    ' Barcode text: letters, numbers and dash (a scanner types it)
    Barcode
End Enum

''' <summary>The rules behind InputKind. One place, so every form in the system behaves the same.</summary>
Public Module InputRules

    Private ReadOnly MobileLive As New Regex("^(0(9\d{0,9})?)?$")
    Private ReadOnly MoneyLive As New Regex("^\d{0,9}(\.\d{0,2})?$")
    Private ReadOnly UsernameFull As New Regex("^[A-Za-z0-9._\-]{3,30}$")
    Private ReadOnly MobileFull As New Regex("^09\d{9}$")
    Private ReadOnly EmailFull As New Regex("^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$")
    Private ReadOnly BarcodeFull As New Regex("^[A-Za-z0-9\-]{4,20}$")

    Public Function DefaultMaxLength(kind As InputKind) As Integer
        Select Case kind
            Case InputKind.PersonName
                Return 100
            Case InputKind.Username
                Return 30
            Case InputKind.MobilePH
                Return 11
            Case InputKind.Phone
                Return 20
            Case InputKind.Email
                Return 100
            Case InputKind.Address
                Return 255
            Case InputKind.Title
                Return 150
            Case InputKind.Password
                Return 64
            Case InputKind.WholeNumber
                Return 9
            Case InputKind.Money
                Return 12
            Case InputKind.Measurement
                Return 60
            Case InputKind.Barcode
                Return 20
            Case Else
                Return 255
        End Select
    End Function

    Private Function IsDigit(c As Char) As Boolean
        Return c >= "0"c AndAlso c <= "9"c
    End Function

    Private Function IsAsciiLetter(c As Char) As Boolean
        Return (c >= "a"c AndAlso c <= "z"c) OrElse (c >= "A"c AndAlso c <= "Z"c)
    End Function

    ''' <summary>Can this character appear in a field of this kind at all?</summary>
    Public Function AllowedChar(kind As InputKind, c As Char) As Boolean
        Select Case kind
            Case InputKind.PersonName
                Return Char.IsLetter(c) OrElse c = " "c OrElse c = "'"c OrElse c = "."c OrElse c = "-"c
            Case InputKind.Username
                Return IsAsciiLetter(c) OrElse IsDigit(c) OrElse c = "."c OrElse c = "_"c OrElse c = "-"c
            Case InputKind.MobilePH, InputKind.WholeNumber
                Return IsDigit(c)
            Case InputKind.Phone
                Return IsDigit(c) OrElse c = "+"c OrElse c = " "c OrElse c = "-"c OrElse c = "("c OrElse c = ")"c
            Case InputKind.Email
                Return IsAsciiLetter(c) OrElse IsDigit(c) OrElse "._%+-@".IndexOf(c) >= 0
            Case InputKind.Address, InputKind.Title
                Return Char.IsLetterOrDigit(c) OrElse " .,#'/()&-".IndexOf(c) >= 0
            Case InputKind.Money
                Return IsDigit(c) OrElse c = "."c
            Case InputKind.Measurement
                Return Char.IsLetterOrDigit(c) OrElse " .,*/-""'()#".IndexOf(c) >= 0
            Case InputKind.Barcode
                Return IsAsciiLetter(c) OrElse IsDigit(c) OrElse c = "-"c
            Case Else
                Return Not Char.IsControl(c)
        End Select
    End Function

    ''' <summary>Is the text, as it would look after this keystroke, still on its way to being valid?</summary>
    Public Function IsTextAllowed(kind As InputKind, proposed As String) As Boolean
        For Each c As Char In proposed
            If Not AllowedChar(kind, c) Then Return False
        Next
        Select Case kind
            Case InputKind.PersonName, InputKind.Address, InputKind.Title, InputKind.Measurement
                If proposed.StartsWith(" ") Then Return False
                If proposed.Contains("  ") Then Return False
            Case InputKind.MobilePH
                Return MobileLive.IsMatch(proposed)
            Case InputKind.Money
                Return MoneyLive.IsMatch(proposed)
        End Select
        Return True
    End Function

    ''' <summary>Pasted text: keep only what could have been typed.</summary>
    Public Function Sanitize(kind As InputKind, text As String) As String
        If text Is Nothing Then Return ""
        Dim sb As New StringBuilder()
        For Each c As Char In text
            If AllowedChar(kind, c) Then sb.Append(c)
        Next
        Return sb.ToString()
    End Function

    Private Function ShortName(caption As String) As String
        Dim p As Integer = caption.IndexOf("("c)
        Return If(p > 0, caption.Substring(0, p).Trim(), caption.Trim())
    End Function

    Public Function PasswordProblem(password As String) As String
        If password Is Nothing OrElse password.Length < 8 Then Return "Password must be at least 8 characters."
        If password.Length > 64 Then Return "Password can be at most 64 characters."
        Dim hasLetter As Boolean = False
        Dim hasDigit As Boolean = False
        For Each c As Char In password
            If Char.IsLetter(c) Then hasLetter = True
            If Char.IsDigit(c) Then hasDigit = True
        Next
        If Not (hasLetter AndAlso hasDigit) Then Return "Password must contain at least one letter and one number."
        Return ""
    End Function

    ''' <summary>"" when the finished value is fine, otherwise a short message that names the field.</summary>
    Public Function Problem(kind As InputKind, caption As String, text As String, required As Boolean,
                            minNumber As Decimal, maxNumber As Decimal) As String
        Dim name As String = ShortName(caption)
        Dim raw As String = If(text, "")
        Dim t As String = If(kind = InputKind.Password, raw, raw.Trim())

        If t.Length = 0 Then Return If(required, name & " is required.", "")

        Select Case kind
            Case InputKind.PersonName
                Dim letters As Integer = 0
                For Each c As Char In t
                    If Char.IsLetter(c) Then letters += 1
                Next
                If letters < 2 Then Return name & " must have at least 2 letters."

            Case InputKind.Username
                If Not UsernameFull.IsMatch(t) Then
                    Return name & " must be 3 to 30 characters: letters, numbers, dot, dash or underscore."
                End If

            Case InputKind.MobilePH
                If Not MobileFull.IsMatch(t) Then
                    Return name & " must be 11 digits starting with 09, for example 09171234567."
                End If

            Case InputKind.Phone
                Dim digits As Integer = 0
                For Each c As Char In t
                    If IsDigit(c) Then digits += 1
                Next
                If digits < 7 OrElse digits > 15 Then Return name & " must have 7 to 15 digits."

            Case InputKind.Email
                If Not EmailFull.IsMatch(t) Then Return name & " is not a valid email address."

            Case InputKind.Address, InputKind.Title
                If t.Length < 2 Then Return name & " is too short."

            Case InputKind.Measurement
                If t.Length < 1 Then Return name & " is required."

            Case InputKind.Barcode
                If Not BarcodeFull.IsMatch(t) Then Return name & " must be 4 to 20 letters, numbers or dashes."

            Case InputKind.Password
                Dim pw As String = PasswordProblem(t)
                If pw.Length > 0 Then Return pw

            Case InputKind.WholeNumber, InputKind.Money
                Dim v As Decimal
                If Not Decimal.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, v) Then
                    Return name & " must be a number."
                End If
                If v < minNumber Then Return name & " must be at least " & minNumber.ToString(CultureInfo.InvariantCulture) & "."
                If v > maxNumber Then Return name & " must be at most " & maxNumber.ToString(CultureInfo.InvariantCulture) & "."
        End Select
        Return ""
    End Function

End Module

''' <summary>A TextBox that refuses characters that do not belong (typed or pasted).</summary>
Friend Class FilteredTextBox
    Inherits TextBox

    Private Const WM_PASTE As Integer = &H302

    Public Property Kind As InputKind = InputKind.Text

    ' When True, Enter does not press the form's default button (scanners send Enter after the code)
    Public Property SwallowEnter As Boolean
    Public Event EnterPressed()

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If SwallowEnter AndAlso e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            e.Handled = True
            RaiseEvent EnterPressed()
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        If Not Char.IsControl(e.KeyChar) Then
            Dim proposed As String = Text.Remove(SelectionStart, SelectionLength).Insert(SelectionStart, e.KeyChar.ToString())
            If Not InputRules.IsTextAllowed(Kind, proposed) Then e.Handled = True
        End If
        MyBase.OnKeyPress(e)
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_PASTE Then
            Dim clip As String = ""
            Try
                If Clipboard.ContainsText() Then clip = Clipboard.GetText()
            Catch
                clip = ""
            End Try

            For Each ch As Char In InputRules.Sanitize(Kind, clip)
                Dim proposed As String = Text.Remove(SelectionStart, SelectionLength).Insert(SelectionStart, ch.ToString())
                If proposed.Length <= MaxLength AndAlso InputRules.IsTextAllowed(Kind, proposed) Then
                    SelectedText = ch.ToString()
                End If
            Next
            Return
        End If
        MyBase.WndProc(m)
    End Sub
End Class

''' <summary>
''' A caption above a rounded text box. Give it a Kind and it only accepts the right characters,
''' has the right maximum length, and can check itself (red border + a message).
''' </summary>
Public Class InputField
    Inherits Panel

    Private ReadOnly _caption As New Label()
    Private ReadOnly _box As New Panel()
    Private ReadOnly _text As New FilteredTextBox()
    Private ReadOnly _tip As New ToolTip()
    Private _captionText As String = ""
    Private _kind As InputKind = InputKind.Text
    Private _required As Boolean
    Private _focused As Boolean
    Private _invalid As Boolean

    Public Property MinNumber As Decimal = 0D
    Public Property MaxNumber As Decimal = 999999999D

    ' Raised when Enter is pressed in this field and SwallowEnter is True (the form is not submitted)
    Public Event EnterPressed()

    ' Raised every time the text changes (typed, pasted or set from code)
    Public Event ValueChanged()

    Public Property SwallowEnter As Boolean
        Get
            Return _text.SwallowEnter
        End Get
        Set(v As Boolean)
            _text.SwallowEnter = v
        End Set
    End Property

    ''' <summary>Plain field (optionally a password box) with no format rules.</summary>
    Public Sub New(caption As String, Optional isPassword As Boolean = False, Optional fieldWidth As Integer = 448)
        Setup(caption, fieldWidth)
        _text.UseSystemPasswordChar = isPassword
    End Sub

    ''' <summary>Field with format rules (see InputKind).</summary>
    Public Sub New(caption As String, kind As InputKind, Optional fieldWidth As Integer = 448, Optional isRequired As Boolean = False)
        Setup(caption, fieldWidth)
        Me.Kind = kind
        Me.Required = isRequired
    End Sub

    Private Sub Setup(caption As String, fieldWidth As Integer)
        _captionText = caption
        Size = New Size(fieldWidth, Theme.S(70))
        BackColor = Color.White

        _caption.AutoSize = True
        _caption.Font = Theme.FontCaption
        _caption.ForeColor = Theme.TextMuted
        _caption.Location = New Point(0, 0)
        UpdateCaption()

        _box.Location = New Point(0, Theme.S(22))
        _box.Size = New Size(fieldWidth, Theme.S(42))
        _box.BackColor = Color.White
        AddHandler _box.Paint, AddressOf OnBoxPaint
        AddHandler _box.Click, AddressOf OnBoxClick

        _text.BorderStyle = BorderStyle.None
        _text.Font = Theme.FontInput
        _text.BackColor = Color.White
        _text.ForeColor = Theme.TextDark
        _text.Width = fieldWidth - Theme.S(24)
        _text.Left = Theme.S(12)
        _text.Top = (_box.Height - _text.Height) \ 2
        AddHandler _text.Enter, AddressOf OnTextEnter
        AddHandler _text.Leave, AddressOf OnTextLeave
        AddHandler _text.TextChanged, AddressOf OnInnerTextChanged
        AddHandler _text.EnterPressed, AddressOf OnInnerEnter

        _box.Controls.Add(_text)
        Controls.Add(_caption)
        Controls.Add(_box)
    End Sub

    ' ------------------------------------------------------------------ settings
    Public Property Kind As InputKind
        Get
            Return _kind
        End Get
        Set(v As InputKind)
            _kind = v
            _text.Kind = v
            _text.MaxLength = InputRules.DefaultMaxLength(v)
            If v = InputKind.Password Then _text.UseSystemPasswordChar = True
            _text.CharacterCasing = If(v = InputKind.Barcode, CharacterCasing.Upper, CharacterCasing.Normal)
        End Set
    End Property

    ''' <summary>Required fields show a * and cannot be left empty.</summary>
    Public Property Required As Boolean
        Get
            Return _required
        End Get
        Set(v As Boolean)
            _required = v
            UpdateCaption()
        End Set
    End Property

    ' The label above the box (change it when the meaning changes, e.g. per piece / per pack)
    Public Property Caption As String
        Get
            Return _captionText
        End Get
        Set(v As String)
            _captionText = v
            UpdateCaption()
        End Set
    End Property

    Private Sub UpdateCaption()
        _caption.Text = _captionText & If(_required, "  *", "")
    End Sub

    Public Property Value As String
        Get
            Return _text.Text
        End Get
        Set(v As String)
            _text.Text = v
        End Set
    End Property

    Public Property IsPassword As Boolean
        Get
            Return _text.UseSystemPasswordChar
        End Get
        Set(v As Boolean)
            _text.UseSystemPasswordChar = v
        End Set
    End Property

    Public Property MaxLength As Integer
        Get
            Return _text.MaxLength
        End Get
        Set(v As Integer)
            _text.MaxLength = v
        End Set
    End Property

    ' Shown but not editable (grey): e.g. a price that is locked
    Public Property IsReadOnly As Boolean
        Get
            Return _text.ReadOnly
        End Get
        Set(v As Boolean)
            _text.ReadOnly = v
            Dim bg As Color = If(v, Color.FromArgb(243, 243, 243), Color.White)
            _text.BackColor = bg
            _box.BackColor = bg
            _text.ForeColor = If(v, Theme.TextMuted, Theme.TextDark)
            _box.Invalidate()
        End Set
    End Property

    Public Sub FocusInput()
        _text.Focus()
    End Sub

    ' ------------------------------------------------------------------ checking
    ''' <summary>Checks the value, shows a red border when wrong, and returns the message ("" when fine).</summary>
    Public Function Check() As String
        Dim msg As String = InputRules.Problem(_kind, _captionText, _text.Text, _required, MinNumber, MaxNumber)
        SetInvalid(msg)
        Return msg
    End Function

    ''' <summary>Checks every field (all show red if wrong), focuses the first bad one, returns its message.</summary>
    Public Shared Function CheckAll(ParamArray fields As InputField()) As String
        Dim firstMessage As String = ""
        Dim firstField As InputField = Nothing
        For Each f As InputField In fields
            Dim m As String = f.Check()
            If m.Length > 0 AndAlso firstField Is Nothing Then
                firstMessage = m
                firstField = f
            End If
        Next
        If firstField IsNot Nothing Then firstField.FocusInput()
        Return firstMessage
    End Function

    Private Sub SetInvalid(message As String)
        _invalid = (message.Length > 0)
        _tip.SetToolTip(_box, message)
        _tip.SetToolTip(_text, message)
        _box.Invalidate()
    End Sub

    ' ------------------------------------------------------------------ events
    Private Sub OnBoxClick(sender As Object, e As EventArgs)
        _text.Focus()
    End Sub

    Private Sub OnInnerTextChanged(sender As Object, e As EventArgs)
        If _invalid Then SetInvalid("")
        RaiseEvent ValueChanged()
    End Sub

    Private Sub OnInnerEnter()
        RaiseEvent EnterPressed()
    End Sub

    Private Sub OnTextEnter(sender As Object, e As EventArgs)
        _focused = True
        _box.Invalidate()
    End Sub

    Private Sub OnTextLeave(sender As Object, e As EventArgs)
        _focused = False

        ' tidy up what was typed
        If Not _text.UseSystemPasswordChar Then
            Dim t As String = _text.Text.Trim()
            If _kind = InputKind.PersonName AndAlso t.Length > 0 Then t = ProperCase(t)
            If _kind = InputKind.Measurement AndAlso t.Length > 0 Then t = MeasurementRules.Normalize(t)
            If t <> _text.Text Then _text.Text = t
        End If
        If _text.Text.Length > 0 Then Check()
        _box.Invalidate()
    End Sub

    ''' <summary>"JUAN DELA CRUZ" or "juan dela cruz" becomes "Juan Dela Cruz"; mixed case is left alone.</summary>
    Private Shared Function ProperCase(t As String) As String
        If t = t.ToLowerInvariant() OrElse t = t.ToUpperInvariant() Then
            Return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.ToLowerInvariant())
        End If
        Return t
    End Function

    Private Sub OnBoxPaint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim rect As New Rectangle(0, 0, _box.Width - 1, _box.Height - 1)
        Dim c As Color = If(_invalid, Theme.AlertRed, If(_focused, Theme.Maroon, Theme.BorderColor))
        Using path As GraphicsPath = Theme.RoundedRect(rect, 8)
            Using pen As New Pen(c, If(_invalid OrElse _focused, 1.8F, 1.0F))
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then _tip.Dispose()
        MyBase.Dispose(disposing)
    End Sub

End Class
