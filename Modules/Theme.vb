Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Drawing2D

''' <summary>Maroon-on-white look used by every screen. Change colors here only.</summary>
Public Module Theme

    Public ReadOnly Maroon As Color = Color.FromArgb(128, 0, 0)
    Public ReadOnly MaroonDark As Color = Color.FromArgb(96, 0, 0)
    Public ReadOnly MaroonLight As Color = Color.FromArgb(158, 26, 26)
    Public ReadOnly MaroonTint As Color = Color.FromArgb(252, 242, 242)

    Public ReadOnly TextDark As Color = Color.FromArgb(24, 28, 32)
    Public ReadOnly TextMuted As Color = Color.FromArgb(88, 96, 104)
    Public ReadOnly BorderColor As Color = Color.FromArgb(218, 218, 218)

    Public ReadOnly AlertRed As Color = Color.FromArgb(200, 30, 30)
    Public ReadOnly AlertBack As Color = Color.FromArgb(255, 240, 240)

    ''' <summary>Text size multiplier from Settings > Text Size (Default 1.12, Medium 1.26, Large 1.40).</summary>
    Public ReadOnly FontScale As Single = TextSize.CurrentScale()

    ''' <summary>Layout multiplier: 1.00 at Default, 1.125 Medium, 1.25 Large. Containers follow the text size.</summary>
    Public ReadOnly LayoutScale As Single = FontScale / 1.12F

    ''' <summary>Scales a pixel size (container, row, offset) with the text-size setting.</summary>
    Public Function S(px As Integer) As Integer
        Return CInt(Math.Round(px * LayoutScale))
    End Function

    ''' <summary>Family for normal text. Set to "Segoe UI Semibold" for a bolder, less thin look.</summary>
    Public Const BodyFamily As String = "Segoe UI"
    Public Const HeadingFamily As String = "Segoe UI Semibold"

    ''' <summary>Every screen builds its fonts here so FontScale applies everywhere.</summary>
    Public Function MakeFont(family As String, size As Single, Optional style As FontStyle = FontStyle.Regular) As Font
        ' big display numbers (24 pt and up) keep their designed size; the rest follows the setting, capped at 24 pt
        If size >= 24.0F Then Return New Font(family, size, style)
        Return New Font(family, Math.Min(size * FontScale, 24.0F), style)
    End Function

    Public ReadOnly FontBase As Font = MakeFont(BodyFamily, 10.0F)
    Public ReadOnly FontMenu As Font = MakeFont(BodyFamily, 10.5F)
    Public ReadOnly FontSmall As Font = MakeFont(BodyFamily, 9.0F)
    Public ReadOnly FontTitle As Font = MakeFont(HeadingFamily, 20.0F)
    Public ReadOnly FontHeading As Font = MakeFont(HeadingFamily, 12.0F)
    Public ReadOnly FontTileTitle As Font = MakeFont(HeadingFamily, 11.5F)
    Public ReadOnly FontCaption As Font = MakeFont(HeadingFamily, 9.0F)
    Public ReadOnly FontInput As Font = MakeFont(BodyFamily, 11.0F)
    Public ReadOnly FontButton As Font = MakeFont(HeadingFamily, 11.0F)
    Public ReadOnly FontBrand As Font = MakeFont(HeadingFamily, 30.0F)

    ''' <summary>Rounded rectangle path (caller disposes it).</summary>
    Public Function RoundedRect(r As Rectangle, radius As Integer) As GraphicsPath
        Dim d As Integer = radius * 2
        Dim p As New GraphicsPath()
        p.AddArc(r.X, r.Y, d, d, 180, 90)
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

End Module
