using ScintillaNET;

namespace DssLabelTemplate;

/// <summary>
/// The editor in the left column: Scintilla (the editor of Notepad++) set up for xml, in the colours of the theme:
/// tags in bold dark blue, attribute names dark blue, values navy on very pale blue, comments italic, line numbers in the
/// margin (the preview names a line when a print object is wrong), and every label variable ($ORDERNO$) in a brand-blue box.
/// For the rest of the program it is a normal control with Text and TextChanged.
/// </summary>
internal class XmlEditor : Scintilla
{
    // Indicators 0 – 7 are used by the lexers; 8 and up are free for the program.
    private const int VariableIndicator = 8;
    private const int LineNumberMargin = 0;

    public XmlEditor()
    {
        BorderStyle = ScintillaNET.BorderStyle.None; // the rounded ContentFrame around it is the border

        // The default style first, then copied to all styles; after that only what differs.
        StyleResetDefault();
        Styles[Style.Default].Font = "Consolas";
        Styles[Style.Default].SizeF = 10F;
        Styles[Style.Default].ForeColor = Theme.XmlText;
        Styles[Style.Default].BackColor = Theme.XmlBackground;
        StyleClearAll();

        LexerName = "xml";
        SetStyle(Style.Xml.Tag, Theme.XmlTag, bold: true);
        SetStyle(Style.Xml.TagEnd, Theme.XmlTag, bold: true);
        SetStyle(Style.Xml.TagUnknown, Theme.XmlTag, bold: true);
        SetStyle(Style.Xml.XmlStart, Theme.XmlTag);
        SetStyle(Style.Xml.XmlEnd, Theme.XmlTag);
        SetStyle(Style.Xml.Attribute, Theme.XmlAttributeName);
        SetStyle(Style.Xml.AttributeUnknown, Theme.XmlAttributeName);
        SetStyle(Style.Xml.DoubleString, Theme.XmlValue, back: Theme.XmlValueBack);
        SetStyle(Style.Xml.SingleString, Theme.XmlValue, back: Theme.XmlValueBack);
        SetStyle(Style.Xml.Number, Theme.XmlValue);
        SetStyle(Style.Xml.Comment, Theme.XmlComment, italic: true);
        SetStyle(Style.Xml.CData, Theme.XmlText);
        SetStyle(Style.Xml.Entity, Theme.XmlAttributeName);

        // The line numbers; the other margins (symbols, folding) are not used.
        Styles[Style.LineNumber].ForeColor = Theme.XmlLineNumber;
        Styles[Style.LineNumber].BackColor = Theme.VeryPaleBlue;
        Margins[LineNumberMargin].Type = MarginType.Number;
        for (int margin = 1; margin < Margins.Count; margin++)
        {
            Margins[margin].Width = 0;
        }

        SelectionBackColor = Theme.PaleBlue;
        CaretForeColor = Theme.Navy;
        EolMode = Eol.CrLf;           // the files are read with CR LF (TextFile); Save writes the file's own line endings
        WrapMode = WrapMode.None;     // like before: the lines of a template stay as they are
        ScrollWidth = 1;              // the horizontal scroll bar only as wide as the longest line
        ScrollWidthTracking = true;

        Indicator variable = Indicators[VariableIndicator];
        variable.Style = IndicatorStyle.RoundBox;
        variable.ForeColor = Theme.XmlVariableBox;
        variable.Alpha = 60;          // a light fill, so the text stays readable
        variable.OutlineAlpha = 200;
        variable.Under = true;
    }

    /// <summary>Puts the text of a file in the editor; "undo" cannot go back to before the file was opened.</summary>
    public void LoadText(string text)
    {
        Text = text;
        EmptyUndoBuffer();
        MarkVariables();
    }

    /// <summary>Draws the box around every $VARIABLE$ again (after opening a file and after typing).</summary>
    public void MarkVariables()
    {
        IndicatorCurrent = VariableIndicator;
        IndicatorClearRange(0, TextLength);
        foreach (System.Text.RegularExpressions.Match match in LabelVariables.Variable.Matches(Text))
        {
            IndicatorFillRange(match.Index, match.Length);
        }

        FitLineNumberMargin();
    }

    /// <summary>The margin is as wide as the biggest line number needs (at least 3 digits), at the scaling of the screen.</summary>
    private void FitLineNumberMargin()
    {
        if (!IsHandleCreated)
        {
            return; // TextWidth needs the window of the editor; done again when it is made
        }

        int digits = Math.Max(3, Lines.Count.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
        Margins[LineNumberMargin].Width = TextWidth(Style.LineNumber, new string('9', digits)) + LogicalToDeviceUnits(4);
        Margins.Left = LogicalToDeviceUnits(6); // white room between the line numbers and the text
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        FitLineNumberMargin();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        FitLineNumberMargin(); // the numbers have another width on a screen with another scaling
    }

    private void SetStyle(int style, Color fore, Color? back = null, bool bold = false, bool italic = false)
    {
        Styles[style].ForeColor = fore;
        Styles[style].BackColor = back ?? Theme.XmlBackground;
        Styles[style].Bold = bold;
        Styles[style].Italic = italic;
    }
}
