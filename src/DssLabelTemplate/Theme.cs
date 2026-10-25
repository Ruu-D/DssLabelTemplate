namespace DssLabelTemplate;

/// <summary>
/// The colour theme of the application, taken from the graphics in the media folder
/// (see "Visual identity" in CLAUDE.md). All colours live here and nowhere else.
/// </summary>
internal static class Theme
{
    public static readonly Color Brand = ColorTranslator.FromHtml("#2CABE2");         // main colour, banner, primary buttons
    public static readonly Color DarkBlue = ColorTranslator.FromHtml("#1480B5");      // hover / pressed, lines
    public static readonly Color LightBlue = ColorTranslator.FromHtml("#7CCAED");     // highlights
    public static readonly Color PaleBlue = ColorTranslator.FromHtml("#A3D7EF");      // selection, borders
    public static readonly Color VeryPaleBlue = ColorTranslator.FromHtml("#D7EFFA");  // panel backgrounds
    public static readonly Color Navy = ColorTranslator.FromHtml("#0A3A56");          // text
    public static readonly Color White = Color.White;

    /// <summary>The one red of the application, only for errors. It is not part of the icon palette.</summary>
    public static readonly Color Error = ColorTranslator.FromHtml("#D6322C");

    /// <summary>The label in the preview is printed black on white, like the real label: not part of the window's palette.</summary>
    public static readonly Color LabelInk = Color.Black;
    public static readonly Color LabelPaper = Color.White;

    /// <summary>
    /// The font of the windows: "Segoe UI Variable Text", the font of Windows 11, if it is installed;
    /// otherwise the normal Windows font. Same size as before (9 pt).
    /// </summary>
    public static Font CreateBodyFont()
    {
        using var installed = new System.Drawing.Text.InstalledFontCollection();
        bool hasVariable = installed.Families.Any(family => family.Name == "Segoe UI Variable Text");
        return hasVariable ? new Font("Segoe UI Variable Text", 9F) : Control.DefaultFont;
    }

    /// <summary>The font of the text editor: Consolas, like Notepad, so the lines of a template line up.</summary>
    public static Font CreateEditorFont()
    {
        return new Font("Consolas", 10F);
    }

    /// <summary>The font of the program name in the banner (drawn by AppIcons.Banner), in pixels of the banner picture.</summary>
    public static Font CreateBannerTitleFont(float pixels)
    {
        return new Font("Segoe UI Semibold", pixels, FontStyle.Regular, GraphicsUnit.Pixel);
    }

    /// <summary>The font of the line below the program name in the banner.</summary>
    public static Font CreateBannerSubtitleFont(float pixels)
    {
        return new Font("Segoe UI", pixels, FontStyle.Regular, GraphicsUnit.Pixel);
    }

    /// <summary>
    /// Main button: brand blue with navy text (white text on brand blue is too pale to read).
    /// On hover it turns dark blue, and then the text turns white.
    /// </summary>
    public static void StylePrimaryButton(RoundedButton button)
    {
        button.NormalFill = Brand;
        button.HoverFill = DarkBlue;
        button.PressedFill = Navy;
        button.BorderColor = Color.Empty;
        button.NormalText = Navy;
        button.HoverText = White;
        button.Font = new Font(button.Font, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    /// <summary>Second button: white with a blue border and navy text.</summary>
    public static void StyleSecondaryButton(RoundedButton button)
    {
        button.NormalFill = White;
        button.HoverFill = VeryPaleBlue;
        button.PressedFill = PaleBlue;
        button.BorderColor = DarkBlue;
        button.NormalText = Navy;
        button.HoverText = Navy;
        button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// One of two buttons that choose a view (template text / csv data): the chosen one is filled with the brand colour,
    /// the other looks like a second button. The font stays regular for both, so the buttons never change size.
    /// </summary>
    public static void StyleToggleButton(RoundedButton button, bool selected)
    {
        StyleSecondaryButton(button);
        if (selected)
        {
            button.NormalFill = Brand;
            button.HoverFill = Brand;
            button.PressedFill = Brand;
            button.BorderColor = Color.Empty;
        }

        button.Invalidate();
    }

    // ---- The colours of the XML editor (Scintilla). Navy and dark blue for text, the pale blues behind it, on a light grey
    //      page. The label variables ($ORDERNO$) get a box in the brand colour.

    /// <summary>
    /// The background of the editor (Daan, 2026-10-25: light grey instead of white). The ONE colour to change for it: the
    /// editor and the rounded frame around it both use it. Not part of the icon palette.
    /// Keep it light: the dark blue tags need a contrast of at least 3:1 (bold text). #E8E8E8 gives 3.6:1 (navy text 9.8:1);
    /// #A0A0A0 was tried and gave only 1.7:1. A test checks the 3:1.
    /// </summary>
    public static readonly Color XmlBackground = ColorTranslator.FromHtml("#E8E8E8");

    public static readonly Color XmlTag = DarkBlue;         // <PrintLabel>, </Text>, the < > and /> around them (bold)
    public static readonly Color XmlAttributeName = DarkBlue; // X=, FontSize=
    public static readonly Color XmlValue = Navy;            // "70", "BARCODE128A" (on a very pale blue)
    public static readonly Color XmlValueBack = VeryPaleBlue;
    public static readonly Color XmlText = Navy;             // the text between the tags
    public static readonly Color XmlComment = DarkBlue;      // <!-- ... --> (italic)
    public static readonly Color XmlLineNumber = DarkBlue;   // the numbers in the margin, on very pale blue
    public static readonly Color XmlVariableBox = Brand;     // $VARIABLE$
}
