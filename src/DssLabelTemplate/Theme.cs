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

    // ---- 2.0.0: the "Maestro" skin (Daan, 2026-11-21). The customer works all day with the SCM machine software (Maestro),
    //      so the window takes over its colours: a light grey frame, white panels with blue titles, navy buttons, a grey
    //      work area behind the label, and a blue bar at the bottom with round light-blue buttons. The colours come from
    //      the mockup Daan approved (claude.ai artifact "DssLabelTemplate Maestro skin"); the brand colours above stay for
    //      the XML editor, the busy layer and the logo.

    public static readonly Color Frame = ColorTranslator.FromHtml("#D9DADC");        // the window around the panels
    public static readonly Color TopBar = ColorTranslator.FromHtml("#DCDDDF");       // the bar at the top
    public static readonly Color WorkArea = ColorTranslator.FromHtml("#B8BCC0");     // behind the label, the board, and the bottom bar's right end
    public static readonly Color Field = ColorTranslator.FromHtml("#EEEFF1");        // path boxes, label size, table titles
    public static readonly Color TabIdle = ColorTranslator.FromHtml("#DCDFE3");      // a tab that is not chosen
    public static readonly Color XmlTabIdle = ColorTranslator.FromHtml("#C9CDD3");   // "Template (xml)" when not chosen; disabled buttons
    public static readonly Color ButtonNavy = ColorTranslator.FromHtml("#0B2C6B");   // Browse, save, the shown label on the board
    public static readonly Color ButtonNavyHover = ColorTranslator.FromHtml("#173F86");
    public static readonly Color ButtonNavyPressed = ColorTranslator.FromHtml("#052F61"); // also the ring of a chosen round button
    public static readonly Color Text = ColorTranslator.FromHtml("#1D4486");         // titles and text (9:1 on white)
    public static readonly Color TextSoft = ColorTranslator.FromHtml("#3A5A8F");     // the status line, the subtitle
    public static readonly Color DisabledText = ColorTranslator.FromHtml("#5C6B82"); // text on a disabled (grey) button
    public static readonly Color Selection = ColorTranslator.FromHtml("#8EB3E4");    // the chosen table row, the tab underline
    public static readonly Color OtherLabel = ColorTranslator.FromHtml("#7FA8DE");   // the other labels on the sheet map / board
    public static readonly Color GridLine = ColorTranslator.FromHtml("#E1E4E8");     // the thin line below the left panel's title
    public static readonly Color TableLine = ColorTranslator.FromHtml("#C9CED6");    // the lines between the rows and columns of the csv table
    public static readonly Color Separator = ColorTranslator.FromHtml("#B9BDC3");    // the short line before the credit
    public static readonly Color Shadow = ColorTranslator.FromHtml("#9EA2A6");       // under the label, the sheet and the title tab
    public static readonly Color TileValue = ColorTranslator.FromHtml("#4E7FC6");    // the big numbers of the tiles (bold, large: 4:1)
    public static readonly Color TileLine = ColorTranslator.FromHtml("#3F6FB5");     // the small lines of the tiles (5:1)
    public static readonly Color BarTop = ColorTranslator.FromHtml("#4C7BC0");       // the blue bottom bar, top ...
    public static readonly Color BarBottom = ColorTranslator.FromHtml("#3561A6");    // ... and bottom of its gradient
    public static readonly Color Pill = ColorTranslator.FromHtml("#E4E7EC");         // the print button at the bottom right

    // The round buttons of the bottom bar: a light-blue disc (lighter at the top left) with a pale ring. DiscSoftLight is
    // the light spot at rest (Daan, 2026-11-21: "make the white gradient fade less visible"); DiscLight under the mouse.
    public static readonly Color DiscSoftLight = ColorTranslator.FromHtml("#DCE7F6");
    public static readonly Color DiscLight = ColorTranslator.FromHtml("#F4F8FD");
    public static readonly Color DiscMiddle = ColorTranslator.FromHtml("#C9DAF0");
    public static readonly Color DiscEdge = ColorTranslator.FromHtml("#9DB9E2");
    public static readonly Color DiscRing = ColorTranslator.FromHtml("#E8EFF9");

    // The window's own minimise / maximise / close buttons and the clock in the top bar (Daan's Maestro screenshot,
    // 2026-11-21): dark grey squares with a white sign (white on #3A3D42 is 11:1).
    public static readonly Color WindowButton = ColorTranslator.FromHtml("#3A3D42");
    public static readonly Color WindowButtonHover = ColorTranslator.FromHtml("#55595F");
    public static readonly Color WindowButtonPressed = ColorTranslator.FromHtml("#202225");

    // The edge around the window, now that it has no Windows frame: the room where it can be resized.
    public static readonly Color WindowEdge = ColorTranslator.FromHtml("#9EA2A6");

    // The round navy buttons (the user guide "?") and the round blue disc behind the logo in the title tab.
    public static readonly Color DarkDiscLight = ColorTranslator.FromHtml("#3D72C4");
    // The disc behind the logo uses the blues of the bottom bar (Daan, 2026-11-21: "the same color blue as the bar below"),
    // with a slightly lighter spot at the top left so it still looks round. The app icon's square uses the bar's gradient.
    public static readonly Color LogoDiscLight = ColorTranslator.FromHtml("#6E97D2");
    public static readonly Color LogoDiscMiddle = BarTop;
    public static readonly Color LogoDiscEdge = BarBottom;

    /// <summary>
    /// The font of the window: Verdana, like the Maestro screens (Daan's mockup, 2026-11-21); it is on every Windows PC.
    /// 8.5 pt: Verdana is wider than Segoe UI, so this is about as big on the screen as the 9 pt Segoe UI of 1.x.
    /// </summary>
    public static Font CreateBodyFont()
    {
        using var installed = new System.Drawing.Text.InstalledFontCollection();
        bool hasVerdana = installed.Families.Any(family => family.Name == "Verdana");
        return hasVerdana ? new Font("Verdana", 8.5F) : Control.DefaultFont;
    }

    /// <summary>A font of the window's family in another size and style (titles, tiles, the counter).</summary>
    public static Font CreateFont(Font body, float points, FontStyle style = FontStyle.Regular)
    {
        return new Font(body.FontFamily, points, style, GraphicsUnit.Point);
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
    /// The navy button of the Maestro skin (2.0.0): Browse and "Save template". White text on navy (13:1), almost square
    /// corners like the Maestro "+" buttons; a button that cannot be pressed (the csv line next to a label-xml) is grey.
    /// </summary>
    public static void StyleNavyButton(RoundedButton button)
    {
        button.NormalFill = ButtonNavy;
        button.HoverFill = ButtonNavyHover;
        button.PressedFill = ButtonNavyPressed;
        button.BorderColor = Color.Empty;
        button.NormalText = White;
        button.HoverText = White;
        button.DisabledFill = XmlTabIdle;
        button.DisabledText = DisabledText;
        button.Radius = 2;
        button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// A text box or big control in the Maestro skin: a flat light-grey field without a border (the editor and the table
    /// get their own background). The dark line at the bottom while it has the focus stays.
    /// </summary>
    public static void StyleField(RoundedFrame frame, Color fill)
    {
        frame.FillColor = fill;
        frame.BorderColor = Color.Empty;
        frame.Radius = 0;
        frame.FocusColor = ButtonNavy;
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

    /// <summary>
    /// The gray layer over the window while a big file is opened (<see cref="BusyOverlay"/>; Daan, 2026-10-26, asked for
    /// gray). Not part of the icon palette. It is see-through (45%), the "loading" box on it is white with navy text.
    /// </summary>
    public static readonly Color BusyShade = ColorTranslator.FromHtml("#808080");

    /// <summary>
    /// Over the window while the Magic parameters popup is open (2.0.1, Daan, the same as TsDssConverter 2.0.1): the window
    /// stays sharp under this see-through navy (ButtonNavy at 35%). The popup itself is WorkArea, the dark grey of the bar.
    /// </summary>
    public static readonly Color DimLayer = Color.FromArgb(89, 0x0B, 0x2C, 0x6B);
    public static readonly Color PopupShadow = Color.FromArgb(90, 0, 0, 0); // the soft shadow under the popup

    public static readonly Color XmlTag = DarkBlue;         // <PrintLabel>, </Text>, the < > and /> around them (bold)
    public static readonly Color XmlAttributeName = DarkBlue; // X=, FontSize=
    public static readonly Color XmlValue = Navy;            // "70", "BARCODE128A" (on a very pale blue)
    public static readonly Color XmlValueBack = VeryPaleBlue;
    public static readonly Color XmlText = Navy;             // the text between the tags
    public static readonly Color XmlComment = DarkBlue;      // <!-- ... --> (italic)
    public static readonly Color XmlLineNumber = DarkBlue;   // the numbers in the margin, on very pale blue
    public static readonly Color XmlVariableBox = Brand;     // $VARIABLE$
}
