Option Strict On
Option Explicit On

Imports System.Windows.Forms

''' <summary>Message boxes and dialog owners for module screens that live inside the main window.</summary>
Public Module FormHelpers

    ''' <summary>The main window (dialogs and messages belong to it, not to the embedded screen).</summary>
    Public Function OwnerOf(screen As Form) As IWin32Window
        Dim f As Form = Nothing
        If screen.Parent IsNot Nothing Then f = screen.Parent.FindForm()
        If f Is Nothing Then Return screen
        Return f
    End Function

    Public Sub Notify(screen As Form, title As String, text As String, icon As MessageBoxIcon)
        MessageBox.Show(OwnerOf(screen), text, title, MessageBoxButtons.OK, icon)
    End Sub

    ''' <summary>Yes/No question; the default answer is No.</summary>
    Public Function Confirm(screen As Form, title As String, text As String, icon As MessageBoxIcon) As Boolean
        Return MessageBox.Show(OwnerOf(screen), text, title, MessageBoxButtons.YesNo, icon,
                               MessageBoxDefaultButton.Button2) = DialogResult.Yes
    End Function

End Module
