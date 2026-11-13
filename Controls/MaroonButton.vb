Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Flat maroon button. MakeSecondary() = outlined, MakeDanger() = outlined red.</summary>
Public Class MaroonButton
    Inherits Button

    Private _secondary As Boolean
    Private _back As Color = Theme.Maroon
    Private _fore As Color = Color.White
    Private _border As Color = Theme.Maroon

    Public Sub New()
        FlatStyle = FlatStyle.Flat
        FlatAppearance.BorderSize = 0
        UseVisualStyleBackColor = False
        BackColor = _back
        ForeColor = _fore
        Font = Theme.FontButton
        Cursor = Cursors.Hand
        Height = Theme.S(46)
        FlatAppearance.MouseOverBackColor = Theme.MaroonLight
        FlatAppearance.MouseDownBackColor = Theme.MaroonDark
    End Sub

    Public Sub MakeSecondary()
        Outline(Theme.Maroon, Theme.MaroonTint)
    End Sub

    Public Sub MakeDanger()
        Outline(Theme.AlertRed, Theme.AlertBack)
    End Sub

    Private Sub Outline(accent As Color, hover As Color)
        _secondary = True
        _back = Color.White
        _fore = accent
        _border = accent
        BackColor = _back
        ForeColor = _fore
        FlatAppearance.BorderSize = 1
        FlatAppearance.BorderColor = _border
        FlatAppearance.MouseOverBackColor = hover
        FlatAppearance.MouseDownBackColor = hover
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        If Enabled Then
            BackColor = _back
            ForeColor = _fore
            FlatAppearance.BorderColor = _border
        ElseIf _secondary Then
            BackColor = Color.White
            ForeColor = Color.FromArgb(175, 175, 175)
            FlatAppearance.BorderColor = Color.FromArgb(228, 228, 228)
        Else
            BackColor = Color.FromArgb(214, 190, 190)
            ForeColor = Color.White
        End If
    End Sub

End Class