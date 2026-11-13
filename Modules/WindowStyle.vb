Option Strict On
Option Explicit On

Imports System.Windows.Forms

''' <summary>Maroon title bar on Windows 11 (silently ignored on older Windows).</summary>
Public Module WindowStyle

    Private Declare Function DwmSetWindowAttribute Lib "dwmapi.dll" (hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer

    Public Sub ApplyMaroonTitleBar(frm As Form)
        Try
            Dim caption As Integer = &H80      ' COLORREF 0x00BBGGRR  -> RGB(128,0,0)
            Dim textWhite As Integer = &HFFFFFF
            DwmSetWindowAttribute(frm.Handle, 35, caption, 4)    ' caption color
            DwmSetWindowAttribute(frm.Handle, 34, caption, 4)    ' border color
            DwmSetWindowAttribute(frm.Handle, 36, textWhite, 4)  ' caption text color
        Catch
            ' not supported on this Windows version - harmless
        End Try
    End Sub

End Module
