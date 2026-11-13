Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

Public Class MaroonColorTable
    Inherits ProfessionalColorTable

    Public Overrides ReadOnly Property MenuStripGradientBegin As Color
        Get
            Return Theme.Maroon
        End Get
    End Property
    Public Overrides ReadOnly Property MenuStripGradientEnd As Color
        Get
            Return Theme.Maroon
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemSelected As Color
        Get
            Return Theme.MaroonLight
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemSelectedGradientBegin As Color
        Get
            Return Theme.MaroonLight
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemSelectedGradientEnd As Color
        Get
            Return Theme.MaroonLight
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemPressedGradientBegin As Color
        Get
            Return Theme.MaroonDark
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemPressedGradientMiddle As Color
        Get
            Return Theme.MaroonDark
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemPressedGradientEnd As Color
        Get
            Return Theme.MaroonDark
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemBorder As Color
        Get
            Return Theme.MaroonLight
        End Get
    End Property
    Public Overrides ReadOnly Property MenuBorder As Color
        Get
            Return Theme.BorderColor
        End Get
    End Property
    Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
        Get
            Return Color.White
        End Get
    End Property
    Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
        Get
            Return Color.White
        End Get
    End Property
    Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
        Get
            Return Color.White
        End Get
    End Property
    Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
        Get
            Return Color.White
        End Get
    End Property
    Public Overrides ReadOnly Property SeparatorDark As Color
        Get
            Return Theme.BorderColor
        End Get
    End Property
    Public Overrides ReadOnly Property SeparatorLight As Color
        Get
            Return Color.White
        End Get
    End Property
    Public Overrides ReadOnly Property StatusStripGradientBegin As Color
        Get
            Return Theme.MaroonDark
        End Get
    End Property
    Public Overrides ReadOnly Property StatusStripGradientEnd As Color
        Get
            Return Theme.MaroonDark
        End Get
    End Property
End Class

''' <summary>Flat maroon look for the menu bar, its drop-downs and the status bar.</summary>
Public Class MaroonMenuRenderer
    Inherits ToolStripProfessionalRenderer

    Public Sub New()
        MyBase.New(New MaroonColorTable())
        Me.RoundedEdges = False
    End Sub

    Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
        If TypeOf e.ToolStrip Is MenuStrip OrElse TypeOf e.ToolStrip Is StatusStrip Then
            e.TextColor = Color.White
        ElseIf e.Item.Selected OrElse e.Item.Pressed Then
            e.TextColor = Color.White
        ElseIf e.Item.Enabled Then
            e.TextColor = Theme.TextDark
        Else
            e.TextColor = Theme.TextMuted
        End If
        MyBase.OnRenderItemText(e)
    End Sub

    Protected Overrides Sub OnRenderArrow(e As ToolStripArrowRenderEventArgs)
        e.ArrowColor = If(e.Item.Selected, Color.White, Theme.Maroon)
        MyBase.OnRenderArrow(e)
    End Sub

End Class
