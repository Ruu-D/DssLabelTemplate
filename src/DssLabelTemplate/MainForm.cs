using System.Drawing.Printing;

namespace DssLabelTemplate;

/// <summary>
/// The only window of the program. Same look as the settings window of TsDssConverter (banner, language and credit at
/// the top, logo at the bottom left), with in between:
///   row 1 (low):  the two label files, each with a path box and a Browse button: the template or label-xml (shown in the
///                 editor) and the label-csv (the data for a template). Above them what the two kinds of label files are,
///                 below them a status line (which kind is open, how many labels);
///   row 2 (high): left the text of the label-xml or template (a simple Notepad) with "Save template" above it after a
///                 change, right the preview of one label with the label size above it and, at the bottom of the window,
///                 two big arrows to go through the labels and a big print button (made for a touch screen). The line
///                 between the two columns can be dragged.
/// The two kinds of files (DSS Software Interface, "Label"): a label-csv + a label template (the editor shows the template,
/// the csv gives the data, one label per csv line), or a label-xml (all labels of one sheet in one file).
/// The layout is built in code, so it can be read and reviewed like any other code.
/// </summary>
internal class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _saveSettings;
    private readonly string? _fileToOpen;
    private readonly string? _dataFolder; // C:\DssLabelTemplate: the Browse dialogs start in its example folders

    // The banner picture is 540 x 84 pixels. It is shown 1.2 times as big (the same as TsDssConverter).
    private const int BannerWidth = 648;
    private const int BannerHeight = 101;

    // The size of the window when it opens, at 100% (the same as the settings window of TsDssConverter).
    private const int WindowWidth = 940;
    private const int WindowHeight = 780;

    // All buttons have exactly this size (at 100%), so they line up and look the same.
    private static readonly Size ButtonSize = new(100, 28);

    // The two arrows and the print button below the preview: twice the size of the 36 x 28 they had next to the title
    // (Daan, 2026-10-25: easier to hit at the reprint station). This is their SMALLEST size: they grow with the preview
    // column up to NavigationButtonMaximumWidth (Daan, 2026-10-26: wider arrows, print as wide; FitNavigationButtons).
    private static readonly Size NavigationButtonSize = new(72, 56);
    private const int NavigationButtonMaximumWidth = 120;
    private const int NavigationSideRoom = 12; // at least this much room between an arrow and the counter or print

    // Dragging the vertical line: the preview column keeps at least this width (at 100%), so the label size still fits
    // next to a few letters of its title. (For the editor column see MinimumEditorColumnWidth.)
    private const int MinimumColumnWidth = 330;

    // The two fields of the label size: as high as the buttons, room for "1000,5".
    private static readonly Size LabelSizeBoxSize = new(60, 28);

    // The two buttons above the left column (template / csv): wide enough for "CSV d'étiquettes" (a test checks all languages).
    private static readonly Size ViewButtonSize = new(120, 28);

    // The csv table, for a touch screen: its font this many points bigger than the window's (9 → 13 pt), and every row this
    // many pixels (at 100%) higher than its text.
    private const float CsvExtraPoints = 4;
    private const int CsvRowExtraHeight = 14;

    // The save button left of them: wide enough for "Enregistrer le modèle" (a test checks all languages).
    private static readonly Size SaveButtonSize = new(150, 28);

    // The editor column is never narrower than its title buttons (save + the two view buttons); the title itself may
    // then shrink to "…". The preview column keeps at least MinimumColumnWidth.
    private static readonly int MinimumEditorColumnWidth = SaveButtonSize.Width + 4 + 2 * ViewButtonSize.Width + 4;

    // The company logo at the bottom left and the user-guide icon next to the app version (at 100%).
    private static readonly Size LogoSize = new(35, 32);
    private static readonly Size UserGuideIconSize = new(16, 16);

    // The "?" icon before each path caption (at 100%), and the longest line of its hover text (in characters): a WinForms
    // tooltip does not wrap by itself, a paragraph would be one line as wide as the screen.
    private static readonly Size HowToIconSize = new(16, 16);
    internal const int HowToLineLength = 80;

    // The preview follows the text this long after the last key press (not after every letter).
    private const int PreviewDelayMilliseconds = 300;

    // The controls we need to read or fill (internal: the tests check them)
    internal readonly PictureBox BannerPicture = new();
    internal readonly Label LanguageCaption = new();
    internal readonly ComboBox LanguageBox = new();
    internal readonly Label CreditLabel = new();
    internal readonly Label VersionLabel = new();
    internal readonly PictureBox UserGuidePicture = new();
    internal readonly PictureBox XmlHowToPicture = new(); // the "?" before "Template of label-xml:": the two kinds of label files
    internal readonly PictureBox CsvHowToPicture = new(); // the same "?" before "Label-csv:"
    internal readonly Label XmlFileCaption = new();  // "Template of label-xml:"
    internal readonly TextBox XmlFileBox = new();    // the path of the file in the editor
    internal readonly RoundedButton XmlBrowseButton = new();
    internal readonly Label CsvFileCaption = new();  // "Label-csv:"
    internal readonly TextBox CsvFileBox = new();    // the path of the label-csv with the data for a template
    internal readonly RoundedButton CsvBrowseButton = new();
    internal readonly Label FileStatus = new();      // what is open: label-xml / template + csv, and how many labels
    internal readonly Label LabelSizeCaption = new(); // "Labelformaat (mm):", right of the status line
    internal readonly TextBox LabelLengthBox = new(); // the label length in mm (horizontal, X)
    internal readonly TextBox LabelWidthBox = new();  // the label width in mm (vertical, Y)
    internal readonly TableLayoutPanel LabelSizePanel = new(); // the caption and the two fields, right of the preview title
    internal readonly Label EditorCaption = new();
    internal readonly RoundedButton XmlViewButton = new(); // shows the template text (only visible with a label-csv)
    internal readonly RoundedButton CsvViewButton = new(); // shows the csv data as a table
    internal readonly TableLayoutPanel EditorHeader = new(); // the editor title with the two view buttons
    internal readonly Panel EditorArea = new();             // holds the editor and the csv table, one of them visible
    internal readonly DataGridView CsvGrid = new();         // the label-csv, read-only, one row per label
    internal readonly Label PreviewCaption = new();
    internal readonly Label LabelCounter = new();    // "2 / 3", left of the arrows
    internal readonly RoundedButton PreviousButton = new();
    internal readonly RoundedButton NextButton = new();
    internal readonly RoundedButton PrintButton = new(); // right of the arrows: prints the shown label (or more)
    internal readonly TableLayoutPanel PreviewHeader = new(); // the preview title with the label size
    internal readonly TableLayoutPanel PreviewNavigation = new(); // below the preview: the counter, the arrows and print
    internal readonly XmlEditor Editor = new();
    internal readonly LabelPreview Preview = new();
    internal readonly ColumnSplitter ColumnDivider = new(); // the vertical line between the editor and the preview, can be dragged
    internal readonly TableLayoutPanel Workspace = new();   // row 2: the two columns and the line between them
    internal readonly List<Panel> Dividers = new();  // the horizontal lines
    internal readonly PictureBox LogoPicture = new();
    internal readonly RoundedButton SaveButton = new(); // "Template opslaan", left of the view buttons, only after a change

    private readonly ToolTip _toolTip = new();

    // The hover text of the "?" icons is long: it stays up longer than the standard 5 seconds (32767 ms is the maximum).
    internal readonly ToolTip HowToToolTip = new() { AutoPopDelay = 32767, InitialDelay = 200 };
    private readonly System.Windows.Forms.Timer _previewTimer = new() { Interval = PreviewDelayMilliseconds };

    // The open file. Null = nothing opened yet (Save then asks for a name).
    private string? _currentFile;
    private TextFileContent? _loaded;   // how the file was written (encoding, line endings), to save it the same way
    private bool _modified;             // changed since it was opened or saved
    private bool _loading;              // filling the editor from a file: that is not a change by the user
    private LabelCsv? _csv;             // the label-csv that goes with the template in the editor; null for a label-xml
    private bool _isLabelXml;           // the editor holds a valid label-xml: then a label-csv is not used
    private Control? _fileRow;          // the rows with the path boxes: the texts above and below get their width (FitFileTexts)
    private bool _showingCsv;           // the left column shows the csv table instead of the template text
    private bool _selectingCsvRow;      // the program (not the user) selects a row of the csv table

    /// <summary>Internal: the file that is open now (the tests check it).</summary>
    internal string? CurrentFile => _currentFile;

    /// <summary>Internal: true when the left column shows the csv table.</summary>
    internal bool ShowingCsv => _showingCsv;

    /// <summary>Internal: the label-csv that goes with the template, or null.</summary>
    internal string? CurrentCsvFile => _csv?.FilePath;

    /// <summary>Internal: true when the text has changed since it was opened or saved.</summary>
    internal bool IsModified => _modified;

    /// <summary>One line of the language box: the language and the text that is shown for it.</summary>
    internal class LanguageItem
    {
        public AppLanguage Language { get; }
        public string Text { get; }

        public LanguageItem(AppLanguage language, string text)
        {
            Language = language;
            Text = text;
        }

        public override string ToString() => Text; // what the ComboBox shows
    }

    /// <param name="settings">The settings in use (language, last file). The window changes them and calls saveSettings.</param>
    /// <param name="saveSettings">Saves the settings. May throw IOException / UnauthorizedAccessException.</param>
    /// <param name="fileToOpen">A file given on the command line (or dropped on the exe); null = the last file.</param>
    /// <param name="dataFolder">The folder with the example folders (C:\DssLabelTemplate); null = none (the tests).</param>
    public MainForm(AppSettings settings, Action<AppSettings> saveSettings, string? fileToOpen, string? dataFolder = null)
    {
        _settings = settings;
        _saveSettings = saveSettings;
        _fileToOpen = fileToOpen;
        _dataFolder = dataFolder;

        // Like a form made with the Visual Studio designer: build everything between SuspendLayout and
        // ResumeLayout. WinForms applies the automatic scaling (for 125%, 150%, ...) when the layout resumes.
        SuspendLayout();
        BuildWindow();
        ResumeLayout(performLayout: true);

        LanguageBox.SelectedIndex = (int)Localizer.Current; // the list is in the order of the enum
        LanguageBox.SelectedIndexChanged += (sender, e) => ChangeLanguage();
        ApplyTexts();

        // The label size of the last time; the handlers come after, so filling the fields does not save anything.
        LabelLengthBox.Text = LabelXml.Format(_settings.LabelLength);
        LabelWidthBox.Text = LabelXml.Format(_settings.LabelWidth);
        Preview.LabelSize = new SizeF((float)_settings.LabelLength, (float)_settings.LabelWidth);
        Preview.SheetRotation = _settings.SheetRotationInViewer; // only in settings.json, not in the window
        Preview.ReferenceCorner = SheetCorners.Parse(_settings.SheetReferenceCorner) ?? SheetCorner.BottomLeft; // the same
        LabelLengthBox.TextChanged += (sender, e) => OnLabelSizeChanged(LabelLengthBox);
        LabelWidthBox.TextChanged += (sender, e) => OnLabelSizeChanged(LabelWidthBox);

        _previewTimer.Tick += (sender, e) => RefreshPreview(newFile: false);
        RefreshPreview(newFile: true);
    }

    // ------------------------------------------------------------------ building the window

    private void BuildWindow()
    {
        // Scaling: all sizes below are written for a screen at 100% (96 dpi) and are multiplied by the screen
        // scaling (125%, 150%, ...). WinForms only does this if the window is built between SuspendLayout and
        // ResumeLayout (see the constructor).
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.CreateBodyFont(); // the Windows 11 font; all controls below inherit it
        Icon = AppIcons.ForWindow();
        BackColor = Theme.White;
        ForeColor = Theme.Navy;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(WindowWidth, WindowHeight);
        MinimumSize = new Size(BannerWidth + 30, 500);

        // Top: the banner, on the brand colour (the banner picture has the same background).
        var banner = new Panel { Dock = DockStyle.Top, Height = BannerHeight, BackColor = Theme.Brand };
        BannerPicture.Image = AppIcons.Banner();
        BannerPicture.SizeMode = PictureBoxSizeMode.Zoom;
        BannerPicture.Dock = DockStyle.Left;
        BannerPicture.Width = BannerWidth;
        BannerPicture.BackColor = Theme.Brand;
        banner.Controls.Add(BannerPicture);

        // Below: one table with a row per part. Only the two columns (row 2) grow with the window.
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 4, 20, 20), // left, top, right, bottom: the room below the buttons is as wide as at the sides
            ColumnCount = 1,
            BackColor = Theme.White,
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // The top row: the language on the left; the credit, the app version and the user-guide icon on the right.
        var topRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topRow.Controls.Add(BuildLanguageColumn(), 0, 0);
        topRow.Controls.Add(BuildCredits(), 1, 0);
        AddRow(body, ref row, topRow);
        AddDivider(body, ref row, new Padding(0, 12, 0, 12));

        // Row 1: the label files: the path boxes and what is open now. The description of the two kinds of files is no
        // longer a text here but the hover text of the "?" icons before the path captions (Daan, 2026-10-26: more height
        // for row 2). The status line gets its size from the window (FitFileTexts), never from the table: see
        // TsDssConverter's "layout rules learned the hard way".
        _fileRow = BuildFileRows();
        AddRow(body, ref row, _fileRow);

        AddRow(body, ref row, BuildStatusRow());

        _fileRow.SizeChanged += (sender, e) => FitFileTexts();
        FileStatus.FontChanged += (sender, e) => FitFileTexts(); // the window moved to a screen with another scaling
        // (No divider line between row 1 and row 2 any more, Daan 2026-10-26: the status line has room below it instead.)

        // Row 2: the editor and the preview, and below them the logo and the big buttons (there is no footer row any
        // more: Close is gone and Save sits above the editor, Daan 2026-10-25). Takes the rest of the height.
        Control workspace = BuildWorkspace();
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(workspace, 0, row);
        row++;
        body.RowCount = row;

        // Docking: the control that is added LAST sits at the very top.
        Controls.Add(body);
        Controls.Add(banner);

        SetUpTabOrder();
    }

    /// <summary>
    /// Tab only goes through the four fields, in this order (Daan): the template / label-xml path, the label-csv path, the
    /// label length and the label width, then back to the first. Every other control is left out (the buttons work with
    /// the mouse, Ctrl+O and Ctrl+S; Tab in the editor is a tab character). Entering a field selects all its text, so
    /// typing replaces it.
    /// </summary>
    private void SetUpTabOrder()
    {
        var fields = new List<Control> { XmlFileBox, CsvFileBox, LabelLengthBox, LabelWidthBox };
        foreach (Control control in AllControls(this))
        {
            control.TabStop = fields.Contains(control);
        }

        // The order itself is the order in which the rows and fields were added (WinForms numbers the controls as they
        // are added): top to bottom, left to right. A test checks it.
        foreach (Control field in fields)
        {
            SelectAllWhenEntered((TextBox)field);
        }
    }

    /// <summary>Every control in the window, also those inside tables and frames.</summary>
    private static IEnumerable<Control> AllControls(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control grandChild in AllControls(child))
            {
                yield return grandChild;
            }
        }
    }

    /// <summary>
    /// Tab (or a click) into the field selects its whole text. After a click it is done when the mouse button goes up:
    /// the click itself puts the caret somewhere in the text. A drag with the mouse keeps the part the user selected.
    /// </summary>
    private static void SelectAllWhenEntered(TextBox box)
    {
        bool selectOnMouseUp = false;
        box.Enter += (sender, e) =>
        {
            if (MouseButtons == MouseButtons.Left && box.ClientRectangle.Contains(box.PointToClient(Cursor.Position)))
            {
                selectOnMouseUp = true;
            }
            else
            {
                box.BeginInvoke(box.SelectAll); // after Windows has given the field the focus
            }
        };
        box.MouseUp += (sender, e) =>
        {
            if (selectOnMouseUp && box.SelectionLength == 0)
            {
                box.SelectAll();
            }

            selectOnMouseUp = false;
        };
        box.Leave += (sender, e) => selectOnMouseUp = false;
    }

    /// <summary>The left part of the top row: the language title (in all three languages) and the language box.</summary>
    private Control BuildLanguageColumn()
    {
        LanguageCaption.Text = Strings.LanguageTitle;
        LanguageCaption.AutoSize = true;
        LanguageCaption.Font = new Font(Font, FontStyle.Bold);
        LanguageCaption.Margin = new Padding(0, 6, 0, 2);

        LanguageBox.DropDownStyle = ComboBoxStyle.DropDownList; // choose from the list, no typing
        LanguageBox.Items.Add(new LanguageItem(AppLanguage.Dutch, Strings.LanguageDutch));
        LanguageBox.Items.Add(new LanguageItem(AppLanguage.French, Strings.LanguageFrench));
        LanguageBox.Items.Add(new LanguageItem(AppLanguage.English, Strings.LanguageEnglish));
        LanguageBox.Width = 260;
        LanguageBox.Anchor = AnchorStyles.Left;
        LanguageBox.ForeColor = Theme.Navy;
        LanguageBox.Margin = new Padding(0, 0, 0, 8);

        // One AutoSize column (a Percent column in an AutoSize table would shrink the drop-down).
        var column = new TableLayoutPanel { ColumnCount = 1, RowCount = 2, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0) };
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        column.Controls.Add(LanguageCaption, 0, 0);
        column.Controls.Add(LanguageBox, 0, 1);
        return column;
    }

    /// <summary>The right part of the top row, against the right edge: "Dev.: Daan Verhoost | App version: 0.1.0" and the user-guide icon.</summary>
    private Control BuildCredits()
    {
        CreditLabel.Text = Strings.DeveloperCredit;
        CreditLabel.AutoSize = true;
        CreditLabel.Margin = new Padding(0);
        CreditLabel.ForeColor = Theme.Navy;

        var separator = new Label { Text = Strings.CreditSeparator, AutoSize = true, Margin = new Padding(0), ForeColor = Theme.PaleBlue };

        VersionLabel.AutoSize = true;
        VersionLabel.Margin = new Padding(0);
        VersionLabel.ForeColor = Theme.Navy;

        var line = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 6, 0, 8), // the same top margin as the language title, so both sit on one line
        };

        // Not higher than the text line; the top margin puts its middle on the middle of the letters.
        UserGuidePicture.Image = AppIcons.UserGuide();
        UserGuidePicture.SizeMode = PictureBoxSizeMode.Zoom;
        UserGuidePicture.Size = UserGuideIconSize;
        UserGuidePicture.BackColor = Theme.White;
        UserGuidePicture.Margin = new Padding(10, 1, 0, 0);
        UserGuidePicture.Cursor = Cursors.Hand;
        UserGuidePicture.Click += (sender, e) => OpenWebsite(Strings.UserGuideUrl);

        line.Controls.Add(CreditLabel);
        line.Controls.Add(separator);
        line.Controls.Add(VersionLabel);
        line.Controls.Add(UserGuidePicture);
        return line;
    }

    /// <summary>
    /// Gives the status line below the path boxes its size: as wide as that row, one line high (cut off with "…").
    /// </summary>
    private void FitFileTexts()
    {
        if (_fileRow == null || _fileRow.Width <= 0)
        {
            return; // still building the window: fitted when the row gets its width
        }

        int width = _fileRow.Width;

        // The status line has the whole width (the label size moved to the preview title, Daan 2026-10-25).
        var status = new Size(Math.Max(0, width - FileStatus.Margin.Horizontal), FileStatus.PreferredHeight);
        if (FileStatus.Size != status)
        {
            FileStatus.Size = status;
        }
    }

    /// <summary>Below the path boxes: the status line (what is open, how many labels), over the whole width.</summary>
    private Control BuildStatusRow()
    {
        FileStatus.AutoSize = false;     // its size comes from FitFileTexts
        FileStatus.AutoEllipsis = true;  // a long csv path ends in "…" instead of a second line
        FileStatus.Anchor = AnchorStyles.Left;
        FileStatus.Margin = new Padding(0, 6, 0, 12); // the room below it replaces the divider line above row 2
        FileStatus.ForeColor = Theme.Navy;
        return FileStatus;
    }

    /// <summary>
    /// The label size, right of the preview title (Daan, 2026-10-25; it was below the path boxes): "Labelformaat (mm):
    /// [100] × [100]". The label files do not contain the size of the label, so it is set here.
    /// </summary>
    private Control BuildLabelSizePanel()
    {
        LabelSizeCaption.AutoSize = true;
        LabelSizeCaption.Anchor = AnchorStyles.Left;
        LabelSizeCaption.Margin = new Padding(0, 0, 6, 0);
        LabelSizeCaption.ForeColor = Theme.Navy;

        var times = new Label { Text = Strings.LabelSizeTimes, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 0, 4, 0), ForeColor = Theme.Navy };

        // The size panel: AutoSize columns only (the frames have a fixed size).
        LabelSizePanel.ColumnCount = 4;
        LabelSizePanel.RowCount = 1;
        LabelSizePanel.AutoSize = true;
        LabelSizePanel.Anchor = AnchorStyles.Right;
        LabelSizePanel.Margin = new Padding(0);
        for (int i = 0; i < 4; i++)
        {
            LabelSizePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        LabelSizePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        LabelSizePanel.Controls.Add(LabelSizeCaption, 0, 0);
        LabelSizePanel.Controls.Add(LabelSizeFrame(LabelLengthBox), 1, 0);
        LabelSizePanel.Controls.Add(times, 2, 0);
        LabelSizePanel.Controls.Add(LabelSizeFrame(LabelWidthBox), 3, 0);
        return LabelSizePanel;
    }

    /// <summary>One field of the label size, in the same rounded frame as the path box, the number on the right.</summary>
    private static TextBoxFrame LabelSizeFrame(TextBox box)
    {
        box.ForeColor = Theme.Navy;
        box.TextAlign = HorizontalAlignment.Right;
        return new TextBoxFrame(box) { Size = LabelSizeBoxSize, Margin = new Padding(0) };
    }

    /// <summary>
    /// A field of the label size was changed: a valid number (1 – 1000 mm, dot or comma) is used at once and saved;
    /// anything else turns red and the preview keeps the last valid size.
    /// </summary>
    private void OnLabelSizeChanged(TextBox box)
    {
        bool valid = LabelXml.TryNumber(box.Text, out double millimetres) && AppSettings.IsValidLabelSize(millimetres);
        box.ForeColor = valid ? Theme.Navy : Theme.Error;
        if (!valid)
        {
            return;
        }

        if (box == LabelLengthBox)
        {
            _settings.LabelLength = millimetres;
        }
        else
        {
            _settings.LabelWidth = millimetres;
        }

        Preview.LabelSize = new SizeF((float)_settings.LabelLength, (float)_settings.LabelWidth);
        TrySaveSettings();
    }

    /// <summary>
    /// Row 1: two lines, each a "?" icon (hover: what the two kinds of label files are), a caption, a path box (Enter
    /// opens what was typed) and a Browse button:
    ///   "Template of label-xml:"  the file in the editor;
    ///   "Label-csv:"              the data for a template (not used, and greyed out, with a label-xml).
    /// </summary>
    private Control BuildFileRows()
    {
        // Icons, captions, frames and buttons in AutoSize columns, only the frames stretch (anchored left and right in the
        // Percent column), so the captions never wrap and the icons and buttons are never shrunk.
        var rows = new TableLayoutPanel { ColumnCount = 4, RowCount = 2, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        SetUpHowToPicture(XmlHowToPicture);
        SetUpHowToPicture(CsvHowToPicture);
        AddFileLine(rows, 0, XmlHowToPicture, XmlFileCaption, XmlFileBox, XmlBrowseButton, OpenTypedXmlFile, BrowseForXmlFile);
        AddFileLine(rows, 1, CsvHowToPicture, CsvFileCaption, CsvFileBox, CsvBrowseButton, OpenTypedCsvFile, BrowseForCsvFile);
        return rows;
    }

    /// <summary>
    /// The "?" icon before a path caption. Its text (the two kinds of label files) appears when the mouse rests on it,
    /// and also when it is tapped or clicked: a touch screen has no mouse to rest.
    /// </summary>
    private void SetUpHowToPicture(PictureBox picture)
    {
        picture.Image = AppIcons.HowTo();
        picture.SizeMode = PictureBoxSizeMode.Zoom;
        picture.Size = HowToIconSize;
        picture.BackColor = Theme.White;
        picture.Cursor = Cursors.Help;
        picture.Click += (sender, e) => HowToToolTip.Show(HowToToolTip.GetToolTip(picture), picture, 0, picture.Height + 4, HowToToolTip.AutoPopDelay);
        picture.MouseLeave += (sender, e) => HowToToolTip.Hide(picture); // also ends the text that a click showed
    }

    /// <summary>
    /// The description of the two kinds of label files, cut into lines of at most <paramref name="maxLineLength"/>
    /// characters (between words) for the tooltip, with an empty line between the paragraphs.
    /// </summary>
    internal static string WrapForToolTip(string text, int maxLineLength)
    {
        var paragraphs = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            var lines = new List<string>();
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > maxLineLength)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                {
                    line = line.Length == 0 ? word : line + " " + word;
                }
            }

            lines.Add(line);
            paragraphs.Add(string.Join("\n", lines));
        }

        return string.Join("\n\n", paragraphs);
    }

    /// <summary>One line of <see cref="BuildFileRows"/>; the second line has some room above it.</summary>
    private static void AddFileLine(TableLayoutPanel rows, int row, PictureBox howTo, Label caption, TextBox box, RoundedButton browse, Action openTyped, Action openBrowsed)
    {
        int top = row == 0 ? 0 : 6;

        howTo.Anchor = AnchorStyles.Left; // in the middle of the line's height, like the caption
        howTo.Margin = new Padding(0, top, 6, 0);

        caption.AutoSize = true;
        caption.Anchor = AnchorStyles.Left; // in the middle of the line's height
        caption.Margin = new Padding(0, top, 8, 0);
        caption.ForeColor = Theme.Navy;

        // The text box sits in a rounded frame that is as high as the button, so both line up.
        box.ForeColor = Theme.Navy;
        var frame = new TextBoxFrame(box)
        {
            Height = ButtonSize.Height,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, top, 8, 0),
        };
        box.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // no "ding"
                openTyped();
            }
        };

        browse.Size = ButtonSize;
        browse.Margin = new Padding(0, top, 0, 0);
        Theme.StyleSecondaryButton(browse);
        browse.Click += (sender, e) => openBrowsed();

        rows.Controls.Add(howTo, 0, row);
        rows.Controls.Add(caption, 1, row);
        rows.Controls.Add(frame, 2, row);
        rows.Controls.Add(browse, 3, row);
    }

    /// <summary>
    /// Row 2: two columns with a thin vertical line between them. Left the text of the template, right the preview of the
    /// label; above each a bold title. The line can be dragged to make one column wider (the share is remembered in
    /// settings.json); a double-click gives two equal halves again. This table is not AutoSize (it fills the rest of the
    /// window), so its controls may stretch.
    /// </summary>
    private Control BuildWorkspace()
    {
        PreviewCaption.Font = new Font(Font, FontStyle.Bold);
        MakeOneLineTitle(PreviewCaption);

        // The left column: the xml editor (XmlEditor.cs) and, for a label-csv, the csv table; one of the two is visible.
        Editor.TextChanged += (sender, e) => OnEditorChanged();
        BuildCsvGrid();
        EditorArea.Dock = DockStyle.Fill;
        EditorArea.Margin = new Padding(0);
        EditorArea.Controls.Add(new ContentFrame(CsvGrid) { Dock = DockStyle.Fill, Visible = false });
        EditorArea.Controls.Add(new ContentFrame(Editor) { Dock = DockStyle.Fill, FillColor = Theme.XmlBackground }); // no white ring around the grey editor

        Preview.Dock = DockStyle.Fill;
        Preview.Margin = new Padding(0);
        Preview.BackColor = Theme.White;

        // The handle is as wide as the room around the line (it used to be a 1 px line with 20 px margins), so it is easy
        // to grab. Its column is AutoSize: a fixed-size control in a Percent column would be shrunk.
        ColumnDivider.Width = ColumnSplitter.HandleWidth;
        ColumnDivider.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
        ColumnDivider.Margin = new Padding(0);
        ColumnDivider.Dragged += DragColumnDivider;
        ColumnDivider.Released += TrySaveSettings;
        ColumnDivider.ResetRequested += () =>
        {
            SetEditorColumnShare(AppSettings.DefaultEditorColumnShare);
            TrySaveSettings();
        };

        Workspace.ColumnCount = 3;
        Workspace.RowCount = 3;
        Workspace.Dock = DockStyle.Fill;
        Workspace.Margin = new Padding(0);
        float editorPercent = (float)(_settings.EditorColumnShare * 100); // the share of the last time (0.5 = equal halves)
        Workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, editorPercent));
        Workspace.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - editorPercent));
        // Three rows: the titles; the editor and the preview (their bottoms in line); the last row of the window, with
        // the logo at the left and, below the preview, the counter, arrows and print button (where Save / Close were).
        Workspace.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Workspace.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Workspace.Controls.Add(BuildEditorHeader(), 0, 0);
        Workspace.Controls.Add(BuildPreviewHeader(), 2, 0);
        Workspace.Controls.Add(EditorArea, 0, 1);
        Workspace.Controls.Add(ColumnDivider, 1, 1);
        Workspace.Controls.Add(Preview, 2, 1);
        Workspace.Controls.Add(BuildLogo(), 0, 2);
        Workspace.Controls.Add(BuildPreviewNavigation(), 2, 2);
        return Workspace;
    }

    /// <summary>
    /// The vertical line is being dragged: the middle of the line follows the mouse (X in the coordinates of the workspace).
    /// </summary>
    internal void DragColumnDivider(int mouseX)
    {
        int available = ColumnsWidth();
        if (available > 0)
        {
            SetEditorColumnShare((mouseX - Workspace.Padding.Left - ColumnDivider.Width / 2.0) / available);
        }
    }

    /// <summary>
    /// The user chose a share (dragging, double-click): it is used, within the minimum widths, and put in the settings
    /// (saved by the caller, when dragging stops).
    /// </summary>
    internal void SetEditorColumnShare(double share)
    {
        _settings.EditorColumnShare = ApplyEditorColumnShare(share);
    }

    /// <summary>
    /// Gives the left column this part of the width (0.5 = equal halves) and the preview the rest, and returns the share
    /// really used. The editor keeps at least <see cref="MinimumEditorColumnWidth"/>, the preview
    /// <see cref="MinimumColumnWidth"/>; when the window is too narrow for both, they share the room in that proportion.
    /// It does NOT change the settings: in a window that is (for a moment) too small for the share the user chose, that
    /// share must stay remembered (until 1.1.0 it was overwritten by the squeezed one at the next start, Daan 2026-10-25).
    /// </summary>
    private double ApplyEditorColumnShare(double share)
    {
        int available = ColumnsWidth();
        if (available > 0)
        {
            double editorMinimum = LogicalToDeviceUnits(MinimumEditorColumnWidth);
            double previewMinimum = LogicalToDeviceUnits(MinimumColumnWidth);
            double lowest = editorMinimum / available;
            double highest = 1 - previewMinimum / available;
            share = lowest <= highest
                ? Math.Max(lowest, Math.Min(highest, share))
                : editorMinimum / (editorMinimum + previewMinimum);
        }

        share = Math.Max(AppSettings.MinimumEditorColumnShare, Math.Min(AppSettings.MaximumEditorColumnShare, share));

        Workspace.ColumnStyles[0].Width = (float)(share * 100);
        Workspace.ColumnStyles[2].Width = (float)(100 - share * 100);
        return share;
    }

    /// <summary>The width of the two columns together (the workspace without the vertical line).</summary>
    private int ColumnsWidth() => Workspace.ClientSize.Width - Workspace.Padding.Horizontal - ColumnDivider.Width;

    /// <summary>
    /// The title of the left column and, against its right edge, the blue "Template opslaan" (only when the text was
    /// changed, Daan 2026-10-25) and the two buttons "Template (xml)" / "Label-csv" that choose what the column shows
    /// (only visible when a label-csv is open). Like the preview header, always as high as a button, so nothing jumps
    /// when the buttons appear.
    /// </summary>
    private Control BuildEditorHeader()
    {
        EditorCaption.Font = new Font(Font, FontStyle.Bold);
        MakeOneLineTitle(EditorCaption);

        SaveButton.Size = SaveButtonSize;
        SaveButton.Margin = new Padding(0, 0, 4, 0);
        SaveButton.Visible = false; // nothing changed yet
        Theme.StylePrimaryButton(SaveButton);
        SaveButton.Click += (sender, e) => SaveFile();
        // No AcceptButton: Enter in the path box opens the file, Enter in the editor starts a new line.

        XmlViewButton.Size = ViewButtonSize;
        XmlViewButton.Margin = new Padding(0, 0, 4, 0);
        CsvViewButton.Size = ViewButtonSize;
        CsvViewButton.Margin = new Padding(0);
        XmlViewButton.Click += (sender, e) => ShowCsvView(false);
        CsvViewButton.Click += (sender, e) => ShowCsvView(true);

        EditorHeader.ColumnCount = 4;
        EditorHeader.RowCount = 1;
        EditorHeader.AutoSize = true;
        EditorHeader.Dock = DockStyle.Fill;
        EditorHeader.Margin = new Padding(0, 0, 0, 4);
        EditorHeader.MinimumSize = new Size(0, ViewButtonSize.Height);
        EditorHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++)
        {
            EditorHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        EditorHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        EditorHeader.Controls.Add(EditorCaption, 0, 0);
        EditorHeader.Controls.Add(SaveButton, 1, 0);
        EditorHeader.Controls.Add(XmlViewButton, 2, 0);
        EditorHeader.Controls.Add(CsvViewButton, 3, 0);
        return EditorHeader;
    }

    /// <summary>
    /// A column title next to buttons: always ONE line, as wide as its cell, ending in "…" when the text does not fit.
    /// An AutoSize label in a table cell is wrapped by the table instead (seen with the long French csv title in 0.4.0,
    /// and in TsDssConverter 1.1.0), and then the row grows and the columns below it move.
    /// </summary>
    private static void MakeOneLineTitle(Label title)
    {
        title.AutoSize = false;
        title.AutoEllipsis = true;
        title.TextAlign = ContentAlignment.MiddleLeft; // in line with the middle of the buttons next to it
        title.Height = title.PreferredHeight; // one line of its (bold) font; scaled with the window like every size
        title.Anchor = AnchorStyles.Left | AnchorStyles.Right; // the cell's width, in the middle of the row's height
        title.Margin = new Padding(0);
    }

    /// <summary>
    /// The csv table: read-only, one row per csv line (= one label), the first column the label number (as in "2 / 3").
    /// Clicking a row shows that label in the preview; the arrows select the row of the label they show.
    /// The standard table of Windows, in the colours of the theme.
    /// </summary>
    private void BuildCsvGrid()
    {
        CsvGrid.ReadOnly = true;
        CsvGrid.AllowUserToAddRows = false;
        CsvGrid.AllowUserToDeleteRows = false;
        CsvGrid.AllowUserToResizeRows = false;
        CsvGrid.AllowUserToOrderColumns = false;
        CsvGrid.RowHeadersVisible = false;
        CsvGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        CsvGrid.MultiSelect = false;
        CsvGrid.BorderStyle = BorderStyle.None; // the rounded ContentFrame around it is the border
        CsvGrid.BackgroundColor = Theme.White;
        CsvGrid.GridColor = Theme.PaleBlue;
        // NOT AutoSizeColumnsMode.AllCells: that measures every cell of every row when the table is shown, about 1 second
        // per 1000 rows, and the window froze while doing it (Daan, 2026-10-26). FitCsvColumns measures one cell per column.
        CsvGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        CsvGrid.FontChanged += (sender, e) => FitCsvColumns(); // the window moved to a screen with another scaling
        CsvGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        // Bigger than the rest of the window, for a touch screen (Daan, 2026-10-25): the font 4 points bigger, and the rows
        // higher than the text (FillCsvGrid). The control's own Font, so WinForms scales it with the screen like the others.
        CsvGrid.Font = new Font(Font.FontFamily, Font.SizeInPoints + CsvExtraPoints, FontStyle.Regular, GraphicsUnit.Point);

        // The headers in the colours of the theme (otherwise Windows draws them in its own grey).
        CsvGrid.EnableHeadersVisualStyles = false;
        CsvGrid.ColumnHeadersDefaultCellStyle.BackColor = Theme.VeryPaleBlue;
        CsvGrid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Navy;
        CsvGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.VeryPaleBlue;
        CsvGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.Navy;
        CsvGrid.ColumnHeadersDefaultCellStyle.Font = new Font(CsvGrid.Font, FontStyle.Bold);
        CsvGrid.DefaultCellStyle.BackColor = Theme.White;
        CsvGrid.DefaultCellStyle.ForeColor = Theme.Navy;
        CsvGrid.DefaultCellStyle.SelectionBackColor = Theme.PaleBlue;
        CsvGrid.DefaultCellStyle.SelectionForeColor = Theme.Navy;

        CsvGrid.CurrentCellChanged += (sender, e) => OnCsvRowChosen(); // after the current row has changed (SelectionChanged comes too early)
    }

    /// <summary>Fills the csv table with the label-csv (or empties it). The columns keep the order of the file.</summary>
    private void FillCsvGrid(LabelCsv? csv)
    {
        _selectingCsvRow = true; // filling selects the first row: that is not a choice of the user
        CsvGrid.Rows.Clear();
        CsvGrid.Columns.Clear();

        // Room above and below the text of every row, easy to tap on a touch screen. Set here (not when the table is
        // built) because the font height in pixels is only right once the window is on its screen.
        CsvGrid.RowTemplate.Height = CsvGrid.Font.Height + LogicalToDeviceUnits(CsvRowExtraHeight);

        if (csv != null)
        {
            CsvGrid.Columns.Add("number", Strings.CsvNumberColumn);
            foreach (string column in csv.Columns)
            {
                CsvGrid.Columns.Add("", column);
            }

            foreach (DataGridViewColumn column in CsvGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable; // row n stays label n
            }

            for (int row = 0; row < csv.Rows.Count; row++)
            {
                var cells = new List<object> { row + 1 };
                cells.AddRange(csv.Columns.Select(column => (object)(csv.Rows[row].TryGetValue(column, out string? value) ? value : "")));
                CsvGrid.Rows.Add(cells.ToArray());
            }
        }

        FitCsvColumns();
        _selectingCsvRow = false;
    }

    /// <summary>
    /// Makes every column of the csv table as wide as its title and its longest text (the one with the most characters).
    /// Only that one cell is measured, not all of them: fast for a csv with thousands of lines. A text with as many but
    /// wider letters can be cut off with "…"; the column can then be made wider with the mouse.
    /// </summary>
    private void FitCsvColumns()
    {
        LabelCsv? csv = _csv;
        if (csv == null || CsvGrid.Columns.Count != csv.Columns.Count + 1 || CsvGrid.Rows.Count != csv.Rows.Count)
        {
            return; // nothing in the table (or it is being filled: fitted at the end of FillCsvGrid)
        }

        foreach (DataGridViewColumn column in CsvGrid.Columns)
        {
            // The longest text, from the csv itself (the first column is the label number: the last one is the longest).
            int longestRow = -1, longestLength = -1;
            for (int row = 0; row < csv.Rows.Count; row++)
            {
                int length = column.Index == 0
                    ? (row + 1).ToString(System.Globalization.CultureInfo.InvariantCulture).Length
                    : csv.Rows[row].TryGetValue(csv.Columns[column.Index - 1], out string? value) ? value.Length : 0;
                if (length > longestLength)
                {
                    longestRow = row;
                    longestLength = length;
                }
            }

            int width = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, fixedHeight: true);
            if (longestRow >= 0)
            {
                width = Math.Max(width, CsvGrid.Rows[longestRow].Cells[column.Index].PreferredSize.Width);
            }

            column.Width = width;
        }
    }

    /// <summary>Shows the template text (false) or the csv table (true) in the left column.</summary>
    internal void ShowCsvView(bool csv)
    {
        _showingCsv = csv && _csv != null;
        Editor.Parent!.Visible = !_showingCsv;
        CsvGrid.Parent!.Visible = _showingCsv;
        Theme.StyleToggleButton(XmlViewButton, selected: !_showingCsv);
        Theme.StyleToggleButton(CsvViewButton, selected: _showingCsv);
        EditorCaption.Text = _showingCsv ? Strings.CsvCaption : Strings.EditorCaption;
        if (_showingCsv)
        {
            SelectCsvRowOfShownLabel(); // the row of the label in the preview
        }
    }

    /// <summary>The user clicked a row of the csv table: show the (first) label of that csv line.</summary>
    private void OnCsvRowChosen()
    {
        if (_selectingCsvRow || CsvGrid.CurrentRow == null || Preview.Labels == null)
        {
            return;
        }

        int row = CsvGrid.CurrentRow.Index;
        int label = Preview.Labels.Labels.FindIndex(item => item.CsvRow == row);
        if (label >= 0 && label != Preview.Index)
        {
            ShowLabel(label);
        }
    }

    /// <summary>Selects the csv row of the label in the preview (after the arrows, typing, or opening the table).</summary>
    private void SelectCsvRowOfShownLabel()
    {
        if (Preview.Count == 0 || Preview.Labels!.Labels[Preview.Index].CsvRow is not int row || row >= CsvGrid.Rows.Count)
        {
            return;
        }

        // Already the current row: the user just clicked it. Setting it again from inside the table's own event is not
        // allowed by the DataGridView ("reentrant call").
        if (CsvGrid.CurrentRow?.Index == row)
        {
            return;
        }

        _selectingCsvRow = true;
        CsvGrid.CurrentCell = CsvGrid.Rows[row].Cells[0]; // also scrolls the row into view
        _selectingCsvRow = false;
    }

    /// <summary>
    /// The title of the preview and, against the right edge of the preview, the label size (Daan, 2026-10-25: in the
    /// place the arrows had). As high as the editor header (the size fields are as high as a button).
    /// </summary>
    private Control BuildPreviewHeader()
    {
        // The title takes the free width (one line, "…" when the column is narrow); the label size what it needs.
        PreviewHeader.ColumnCount = 2;
        PreviewHeader.RowCount = 1;
        PreviewHeader.AutoSize = true;
        PreviewHeader.Dock = DockStyle.Fill;
        PreviewHeader.Margin = new Padding(0, 0, 0, 4);
        PreviewHeader.MinimumSize = new Size(0, ViewButtonSize.Height);
        PreviewHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        PreviewHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        PreviewHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        PreviewHeader.Controls.Add(PreviewCaption, 0, 0);
        PreviewHeader.Controls.Add(BuildLabelSizePanel(), 1, 0);
        return PreviewHeader;
    }

    /// <summary>
    /// Below the preview, as wide as the preview (Daan, 2026-10-26): "2 / 3" against its left edge, the two arrows in the
    /// middle (only when there is more than one label) and the print button against its right edge (as soon as there is
    /// a label); the three buttons twice as high as the other buttons and as wide as fits (FitNavigationButtons). The row
    /// is always as high as these buttons, also when they are hidden, so the preview never jumps.
    /// </summary>
    private Control BuildPreviewNavigation()
    {
        LabelCounter.AutoSize = true;
        LabelCounter.Anchor = AnchorStyles.Left;
        LabelCounter.Margin = new Padding(0);
        LabelCounter.ForeColor = Theme.Navy;
        LabelCounter.Font = new Font(Font, FontStyle.Bold); // Daan, 2026-10-26: easier to read at the reprint station

        // The arrow signs twice as big too, to fit the bigger buttons.
        var arrowFont = new Font(Font.FontFamily, Font.SizeInPoints * 2, FontStyle.Regular, GraphicsUnit.Point);
        foreach (RoundedButton arrow in new[] { PreviousButton, NextButton })
        {
            arrow.Size = NavigationButtonSize;
            arrow.Margin = new Padding(0);
            arrow.Font = arrowFont;
            Theme.StyleSecondaryButton(arrow);
        }

        PreviousButton.Text = Strings.PreviousArrow;
        NextButton.Text = Strings.NextArrow;
        PreviousButton.Margin = new Padding(0, 0, 4, 0);
        PreviousButton.Click += (sender, e) => ShowLabel(Preview.Index - 1);
        NextButton.Click += (sender, e) => ShowLabel(Preview.Index + 1);

        // The print button: against the right edge of the preview, the same size as the arrows, the printer icon instead
        // of a text.
        PrintButton.Size = NavigationButtonSize;
        PrintButton.Anchor = AnchorStyles.Right;
        PrintButton.Margin = new Padding(0);
        PrintButton.Image = AppIcons.Print();
        PrintButton.IconHeight = 2 * RoundedButton.DefaultIconHeight;
        Theme.StyleSecondaryButton(PrintButton);
        PrintButton.Click += (sender, e) => PrintLabels();

        // Four columns: two equal Percent columns (the counter at the left of the first, print at the right of the
        // second) around two AutoSize columns with the arrows, which so stay exactly in the middle. This table is NOT
        // AutoSize: it is as wide as the preview (anchored left and right in the same column) and as high as the buttons
        // (a fixed height, not stretched: its row of the workspace is AutoSize).
        PreviewNavigation.ColumnCount = 4;
        PreviewNavigation.RowCount = 1;
        PreviewNavigation.AutoSize = false;
        PreviewNavigation.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        PreviewNavigation.Margin = new Padding(0, 20, 0, 0); // the room the footer with Save / Close had above it
        PreviewNavigation.Height = NavigationButtonSize.Height;
        PreviewNavigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        PreviewNavigation.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        PreviewNavigation.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        PreviewNavigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        PreviewNavigation.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        PreviewNavigation.Controls.Add(LabelCounter, 0, 0);
        PreviewNavigation.Controls.Add(PreviousButton, 1, 0);
        PreviewNavigation.Controls.Add(NextButton, 2, 0);
        PreviewNavigation.Controls.Add(PrintButton, 3, 0);
        PreviewNavigation.SizeChanged += (sender, e) => FitNavigationButtons();
        return PreviewNavigation;
    }

    /// <summary>
    /// The arrows and print get the same width: as wide as fits in the row below the preview (four button widths, the
    /// room between the arrows and room on both sides of them), at least the 72 they always had and at most 120 (at 100%).
    /// Called when the row changes width (the window, the column line).
    /// </summary>
    private void FitNavigationButtons()
    {
        int room = PreviewNavigation.Width - PreviousButton.Margin.Right - 2 * LogicalToDeviceUnits(NavigationSideRoom);
        int width = Math.Max(LogicalToDeviceUnits(NavigationButtonSize.Width),
                             Math.Min(LogicalToDeviceUnits(NavigationButtonMaximumWidth), room / 4));
        foreach (RoundedButton button in new[] { PreviousButton, NextButton, PrintButton })
        {
            if (button.Width != width) // also stops the loop: a new width lays the row out again
            {
                button.Width = width;
            }
        }
    }

    /// <summary>The company logo at the bottom left of the window (below the editor), opens rogiers.be.</summary>
    private Control BuildLogo()
    {
        LogoPicture.Image = AppIcons.CompanyLogo();
        LogoPicture.SizeMode = PictureBoxSizeMode.Zoom; // keeps the proportions of the logo
        LogoPicture.Size = LogoSize;
        LogoPicture.BackColor = Theme.White;
        LogoPicture.Anchor = AnchorStyles.Left | AnchorStyles.Bottom; // its bottom in line with the bottom of the big buttons
        LogoPicture.Margin = new Padding(0);
        LogoPicture.Cursor = Cursors.Hand;
        _toolTip.SetToolTip(LogoPicture, Strings.CompanyName);
        LogoPicture.Click += (sender, e) => OpenWebsite(Strings.CompanyUrl);
        return LogoPicture;
    }

    /// <summary>A thin horizontal line over the full width, in the pale blue of the theme.</summary>
    private void AddDivider(TableLayoutPanel table, ref int row, Padding margin)
    {
        var line = new Panel { Height = 1, Dock = DockStyle.Fill, BackColor = Theme.PaleBlue, Margin = margin };
        Dividers.Add(line);
        AddRow(table, ref row, line);
    }

    private static void AddRow(TableLayoutPanel table, ref int row, Control control)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(control, 0, row);
        row++;
    }

    // ------------------------------------------------------------------ language

    /// <summary>Puts every text in the selected language. Called at the start and after a language change.</summary>
    internal void ApplyTexts()
    {
        UpdateTitle();
        VersionLabel.Text = Strings.AppVersion(AppInfo.Version);
        _toolTip.SetToolTip(UserGuidePicture, Strings.UserGuideTooltip);
        string howTo = WrapForToolTip(Strings.FileDescription, HowToLineLength);
        HowToToolTip.SetToolTip(XmlHowToPicture, howTo);
        HowToToolTip.SetToolTip(CsvHowToPicture, howTo);
        XmlFileCaption.Text = Strings.XmlFileCaption;
        CsvFileCaption.Text = Strings.CsvFileCaption;
        XmlBrowseButton.Text = Strings.Browse;
        CsvBrowseButton.Text = Strings.Browse;
        EditorCaption.Text = _showingCsv ? Strings.CsvCaption : Strings.EditorCaption;
        XmlViewButton.Text = Strings.XmlViewButton;
        CsvViewButton.Text = Strings.CsvViewButton;
        PreviewCaption.Text = Strings.PreviewCaption;
        LabelSizeCaption.Text = Strings.LabelSizeCaption;
        _toolTip.SetToolTip(LabelLengthBox, Strings.LabelLengthTooltip);
        _toolTip.SetToolTip(LabelWidthBox, Strings.LabelWidthTooltip);
        _toolTip.SetToolTip(PreviousButton, Strings.PreviousLabel);
        _toolTip.SetToolTip(NextButton, Strings.NextLabel);
        _toolTip.SetToolTip(PrintButton, Strings.PrintTooltip);
        PrintButton.AccessibleName = Strings.PrintTooltip; // the button has a picture, no text: for a screen reader
        _toolTip.SetToolTip(ColumnDivider, Strings.ColumnDividerTooltip);
        SaveButton.Text = Strings.SaveTemplate;
        FitFileTexts(); // the description has another length in another language
        RefreshPreview(newFile: false); // the preview's messages and the status line are in the language too
    }

    private void ChangeLanguage()
    {
        AppLanguage language = ((LanguageItem)LanguageBox.SelectedItem!).Language;
        Localizer.Current = language;
        _settings.Language = Localizer.ToCode(language);
        ApplyTexts();
        TrySaveSettings();
    }

    // ------------------------------------------------------------------ opening and saving

    /// <summary>
    /// Opens a label-xml or a template in the editor together with a label-csv (null = none; ignored for a label-xml).
    /// Used at the start (the files of the last time) and by the tests. No questions asked: the caller chose the files.
    /// Returns false (and says why, unless quiet) if a file cannot be read; then nothing changes.
    /// </summary>
    internal bool OpenLabelFiles(string xmlPath, string? csvPath, bool quiet = false)
    {
        using BusyOverlay? busy = BusyOverlay.ShowIfBig(this, BusyOverlay.SizeOfFiles(xmlPath, csvPath)); // gone at the end
        LabelCsv? csv = null;
        if (!TryRead(() => TextFile.Read(xmlPath), quiet, out TextFileContent? content) ||
            (csvPath != null && !TryRead(() => LabelCsv.Read(csvPath), quiet, out csv)))
        {
            return false;
        }

        ShowXmlFile(xmlPath, content!);
        UseCsv(IsLabelXml(content!.Text) ? null : csv);
        RefreshPreview(newFile: true);
        ShowCsvView(true); // with a label-csv the table is shown first (Daan, 2026-10-25); a label-xml has none: its text
        RememberFiles();
        return true;
    }

    /// <summary>
    /// The upper line: opens a template or label-xml in the editor. A template keeps the label-csv that is open (another
    /// template for the same data); a label-xml has its own data, so the csv is closed. Returns false if nothing was opened.
    /// </summary>
    internal bool OpenXmlFile(string path, bool quiet = false)
    {
        // A big file: the gray layer with "loading" until the end of this method. The open csv is worked through again too.
        using BusyOverlay? busy = BusyOverlay.ShowIfBig(this, BusyOverlay.SizeOfFiles(path, _csv?.FilePath));
        if (!TryRead(() => TextFile.Read(path), quiet, out TextFileContent? content))
        {
            return false;
        }

        ShowXmlFile(path, content!);
        if (IsLabelXml(content!.Text))
        {
            UseCsv(null);
        }

        RefreshPreview(newFile: true);
        ShowCsvView(_showingCsv);
        RememberFiles();
        return true;
    }

    /// <summary>
    /// The lower line: opens a label-csv with the data for the template in the editor (also before the template is chosen),
    /// or closes it (null). The text in the editor stays as it is. Returns false if the file cannot be read.
    /// </summary>
    internal bool OpenCsvFile(string? path, bool quiet = false)
    {
        using BusyOverlay? busy = BusyOverlay.ShowIfBig(this, BusyOverlay.SizeOfFiles(path)); // gone at the end
        LabelCsv? csv = null;
        if (path != null && !TryRead(() => LabelCsv.Read(path), quiet, out csv))
        {
            return false;
        }

        UseCsv(csv);
        RefreshPreview(newFile: true); // the first label of the new data
        ShowCsvView(true); // a chosen label-csv is shown as its table (Daan, 2026-10-25); closed (null): the template text
        RememberFiles();
        return true;
    }

    /// <summary>Reads a file; when that fails, says why (unless quiet) and returns false.</summary>
    private bool TryRead<T>(Func<T> read, bool quiet, out T? result)
    {
        try
        {
            result = read();
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException)
        {
            if (!quiet)
            {
                MessageBox.Show(this, Strings.CannotReadFile(error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            result = default;
            return false;
        }
    }

    /// <summary>Puts a file that was read into the editor (not a change by the user: "undo" cannot go back past it).</summary>
    private void ShowXmlFile(string path, TextFileContent content)
    {
        _loading = true;
        Editor.LoadText(content.Text);
        _loading = false;

        _currentFile = Path.GetFullPath(path);
        _loaded = content;
        _modified = false;
        XmlFileBox.Text = _currentFile;
        UpdateTitle();
        Preview.ForgetImages(); // the pictures of the labels may be other files now
    }

    /// <summary>The label-csv that goes with the template (null = none): its path box and its table.</summary>
    private void UseCsv(LabelCsv? csv)
    {
        _csv = csv;
        CsvFileBox.Text = csv?.FilePath ?? "";
        FillCsvGrid(csv);
    }

    /// <summary>Remembers the open files, to open them again at the next start.</summary>
    private void RememberFiles()
    {
        if (_currentFile != null) // no template open (it could not be opened at the start): keep the last one for next time
        {
            _settings.LastFile = _currentFile;
        }

        _settings.LastCsvFile = _csv?.FilePath ?? "";
        TrySaveSettings();
    }

    /// <summary>A valid label-xml (not a template, not broken xml): it holds its own data, a label-csv is not used.</summary>
    private static bool IsLabelXml(string xmlText)
    {
        LabelSet labels = LabelSetBuilder.Build(xmlText, null, DateTime.Now);
        return labels.Error == null && labels.Kind == LabelFileKind.LabelXml;
    }

    /// <summary>A file given on the command line (or dropped on the exe): a .csv goes to the lower line, anything else to the upper.</summary>
    private bool OpenChosenFile(string path)
    {
        return Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? OpenCsvFile(path)
            : ConfirmLeavingChanges() && OpenXmlFile(path);
    }

    /// <summary>
    /// Saves the editor to the open file, in the same encoding and line endings as it was read. With no file open yet
    /// it asks for a name first. Returns false if nothing was saved.
    /// </summary>
    internal bool SaveFile()
    {
        string? path = _currentFile;
        if (path == null)
        {
            using var dialog = new SaveFileDialog { Filter = Strings.XmlFileFilter };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            path = dialog.FileName;
        }

        // A new file: UTF-8 without BOM and Windows line endings.
        TextFileContent how = _loaded ?? new TextFileContent("", new System.Text.UTF8Encoding(false), LineEnding.CrLf);
        try
        {
            TextFile.Write(path, Editor.Text, how.Encoding, how.LineEnding);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            MessageBox.Show(this, Strings.CannotSaveFile(error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        _currentFile = Path.GetFullPath(path);
        _loaded = how;
        _modified = false;
        XmlFileBox.Text = _currentFile;
        UpdateTitle();
        RefreshPreview(newFile: false); // pictures with a relative path: the folder may have changed
        RememberFiles();
        return true;
    }

    /// <summary>
    /// The print button (and Ctrl+P): the Windows print dialog, starting with the printer of the last time and with
    /// "current page" = the label in the preview. "Pages" are the label numbers ("2 / 3"), so the dialog can also print
    /// all labels or a range. Before printing, the printer's page is compared with the label size of the window.
    /// </summary>
    internal void PrintLabels()
    {
        if (Preview.Count == 0)
        {
            return;
        }

        try
        {
            using var document = new PrintDocument
            {
                DocumentName = Strings.AppName + " - " + (_currentFile == null ? Strings.Untitled : Path.GetFileName(_currentFile)),
            };
            if (_settings.PrinterName != "" && PrinterSettings.InstalledPrinters.Cast<string>().Contains(_settings.PrinterName))
            {
                document.PrinterSettings.PrinterName = _settings.PrinterName;
            }

            document.PrinterSettings.MinimumPage = 1;
            document.PrinterSettings.MaximumPage = Preview.Count;
            document.PrinterSettings.FromPage = Preview.Index + 1;
            document.PrinterSettings.ToPage = Preview.Index + 1;
            document.PrinterSettings.PrintRange = PrintRange.CurrentPage;

            // UseEXDialog: the dialog with "current page" (the newer Windows dialog has no page choice).
            using var dialog = new PrintDialog { Document = document, UseEXDialog = true, AllowCurrentPage = true, AllowSomePages = true };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            PrinterSettings chosen = document.PrinterSettings;
            List<int> numbers = LabelPrinter.LabelsToPrint(chosen.PrintRange, chosen.FromPage, chosen.ToPage, Preview.Index, Preview.Count);
            if (numbers.Count == 0)
            {
                return;
            }

            _settings.PrinterName = chosen.PrinterName;
            TrySaveSettings();

            SizeF paper = LabelPrinter.PaperSize(document.DefaultPageSettings);
            if (!LabelPrinter.PaperFits(paper, Preview.LabelSize))
            {
                string question = Strings.PaperSizeQuestion(chosen.PrinterName, LabelXml.Format(paper.Width), LabelXml.Format(paper.Height),
                    LabelXml.Format(Preview.LabelSize.Width), LabelXml.Format(Preview.LabelSize.Height));
                if (MessageBox.Show(this, question, Strings.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }

            string? folder = _currentFile == null ? null : Path.GetDirectoryName(_currentFile);
            LabelPrinter.Print(document, numbers.Select(number => Preview.Labels!.Labels[number]).ToList(), folder);
        }
        catch (Exception error) when (error is InvalidPrinterException || error is System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this, Strings.CannotPrint(error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>The upper Browse button (and Ctrl+O): a template or a label-xml.</summary>
    private void BrowseForXmlFile()
    {
        string? path = AskForFile(Strings.XmlFileFilter, _currentFile, AppDataFolder.LabelsFolderName);
        if (path != null && ConfirmLeavingChanges())
        {
            OpenXmlFile(path);
        }
    }

    /// <summary>The lower Browse button: a label-csv.</summary>
    private void BrowseForCsvFile()
    {
        string? path = AskForFile(Strings.CsvFilter, _csv?.FilePath, AppDataFolder.CsvFolderName);
        if (path != null)
        {
            OpenCsvFile(path);
        }
    }

    /// <summary>
    /// A file dialog that starts in the folder of the open file of that line, or else in the example folder of
    /// C:\DssLabelTemplate (labels, CSV examples). Null = Cancel.
    /// </summary>
    private string? AskForFile(string filter, string? openFile, string exampleFolderName)
    {
        using var dialog = new OpenFileDialog { Filter = filter };
        string? exampleFolder = _dataFolder == null ? null : Path.Combine(_dataFolder, exampleFolderName);
        if (openFile != null)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(openFile);
        }
        else if (exampleFolder != null && Directory.Exists(exampleFolder))
        {
            dialog.InitialDirectory = exampleFolder;
        }

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>Enter in the upper path box: open what was typed (the box goes back to the open file if that fails).</summary>
    private void OpenTypedXmlFile()
    {
        string typed = TypedPath(XmlFileBox);
        if (typed == "" || !ConfirmLeavingChanges() || !OpenXmlFile(typed))
        {
            XmlFileBox.Text = _currentFile ?? "";
        }
    }

    /// <summary>Enter in the lower path box: open what was typed; an empty box closes the label-csv.</summary>
    private void OpenTypedCsvFile()
    {
        string typed = TypedPath(CsvFileBox);
        if (!OpenCsvFile(typed == "" ? null : typed))
        {
            CsvFileBox.Text = _csv?.FilePath ?? "";
        }
    }

    /// <summary>The path in a box, without the quotes that Explorer's "Copy as path" puts around it.</summary>
    private static string TypedPath(TextBox box) => box.Text.Trim().Trim('"');

    /// <summary>
    /// Before the text in the editor is replaced or the program closes: if there are unsaved changes, ask whether to
    /// save them. Returns false when the user chose Cancel (or the save failed): then nothing must happen.
    /// </summary>
    private bool ConfirmLeavingChanges()
    {
        if (!_modified)
        {
            return true;
        }

        string name = _currentFile == null ? Strings.Untitled : Path.GetFileName(_currentFile);
        DialogResult answer = MessageBox.Show(this, Strings.SaveChangesQuestion(name), Strings.AppName,
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => SaveFile(),
            DialogResult.No => true,
            _ => false,
        };
    }

    private void OnEditorChanged()
    {
        if (_loading)
        {
            return;
        }

        if (!_modified)
        {
            _modified = true;
            UpdateTitle();
        }

        // Restart the timer: the preview follows when the user stops typing for a moment.
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    /// <summary>
    /// Reads the text in the editor again (with the label-csv of a template) and shows it: the preview, the arrows and the
    /// status line. Called after opening a file (the first label), 300 ms after typing (the same label stays), and after a
    /// language change.
    /// </summary>
    private void RefreshPreview(bool newFile)
    {
        _previewTimer.Stop();
        LabelSet? labels = Editor.Text.Trim() == "" ? null : LabelSetBuilder.Build(Editor.Text, _csv, DateTime.Now);
        string? folder = _currentFile == null ? null : Path.GetDirectoryName(_currentFile);
        Preview.ShowLabels(labels, folder, keepIndex: !newFile);
        Editor.MarkVariables();
        _isLabelXml = labels is { Error: null, Kind: LabelFileKind.LabelXml };
        UpdateCsvLine();
        UpdateNavigation();
        UpdateFileStatus(labels);
    }

    /// <summary>
    /// The label-csv line is greyed out while the editor holds a label-xml (it has its own data, the csv is not used);
    /// the grey text in the empty box says so. With a template, or nothing open yet, a csv can be chosen.
    /// </summary>
    private void UpdateCsvLine()
    {
        bool usable = !_isLabelXml;
        CsvFileCaption.Enabled = usable;
        CsvFileBox.Enabled = usable;
        CsvBrowseButton.Enabled = usable;
        CsvFileBox.PlaceholderText = usable ? Strings.CsvPlaceholder : Strings.CsvNotNeededPlaceholder;
    }

    /// <summary>Shows another label (the arrows); the index is kept within the labels by the preview.</summary>
    internal void ShowLabel(int index)
    {
        Preview.Index = index;
        UpdateNavigation();
    }

    /// <summary>The counter and the arrows: only when there is more than one label; an arrow at the end cannot be pressed.</summary>
    private void UpdateNavigation()
    {
        int count = Preview.Count;
        bool several = count > 1;
        LabelCounter.Visible = several;
        PreviousButton.Visible = several;
        NextButton.Visible = several;

        // "2 / 3" is the same in every language (numbers only).
        LabelCounter.Text = (Preview.Index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " / " +
                            count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        PreviousButton.Enabled = Preview.Index > 0;
        NextButton.Enabled = Preview.Index < count - 1;

        // Print: as soon as there is a label to print (also just one); not for broken xml or an empty label file.
        PrintButton.Visible = count > 0;

        // The two view buttons only with a label-csv that is used; the csv table follows the label of the preview.
        bool csv = _csv != null && !_isLabelXml;
        XmlViewButton.Visible = csv;
        CsvViewButton.Visible = csv;
        if (!csv && _showingCsv)
        {
            ShowCsvView(false);
        }

        SelectCsvRowOfShownLabel();
    }

    /// <summary>The line below the path boxes: what kind of file is open, with how many labels.</summary>
    private void UpdateFileStatus(LabelSet? labels)
    {
        FileStatus.Text = labels switch
        {
            null when _csv != null => Strings.StatusCsvWithoutTemplate,
            null => Strings.StatusNothingOpen,
            { Error: not null } => Strings.StatusXmlError,
            { Kind: LabelFileKind.LabelXml } => Strings.StatusLabelXml(labels.Labels.Count),
            _ when _csv != null => Strings.StatusTemplateWithCsv(labels.Labels.Count),
            _ => Strings.StatusTemplateWithoutCsv,
        };
    }

    /// <summary>
    /// The title bar: the program name, the open file, and a * when there are unsaved changes (like Notepad). The save
    /// button follows the same * : it is only shown while there is something to save.
    /// </summary>
    private void UpdateTitle()
    {
        SaveButton.Visible = _modified;

        string title = Strings.AppName;
        if (_currentFile != null || _modified)
        {
            string name = _currentFile == null ? Strings.Untitled : Path.GetFileName(_currentFile);
            title += " - " + name + (_modified ? " *" : "");
        }

        Text = title;
    }

    private void TrySaveSettings()
    {
        try
        {
            _saveSettings(_settings);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            MessageBox.Show(this, Strings.SettingsSaveFailed(error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenWebsite(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception || error is IOException)
        {
            MessageBox.Show(this, Strings.CannotOpenLink(error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ------------------------------------------------------------------ window events

    /// <summary>Ctrl+S saves, Ctrl+O opens, Ctrl+P prints (as in Notepad).</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S))
        {
            SaveFile();
            return true;
        }

        if (keyData == (Keys.Control | Keys.P))
        {
            PrintLabels();
            return true;
        }

        if (keyData == (Keys.Control | Keys.O))
        {
            BrowseForXmlFile();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// Makes sure the window is not bigger than the part of the screen that is free (not under the taskbar)
    /// and puts it in the middle of it.
    /// </summary>
    internal void FitToArea(Rectangle area)
    {
        int width = Math.Min(Width, area.Width);
        int height = Math.Min(Height, area.Height);
        Size = new Size(width, height);
        Location = new Point(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e); // the scaling of the whole window has been applied by now
        RestoreWindowPlace();

        // The column share of the last time, within the minimum widths of the window as it is now; again every time the
        // window changes size (e.g. maximised again), so the share the user chose comes back when there is room for it.
        ApplyEditorColumnShare(_settings.EditorColumnShare);
        Workspace.SizeChanged += (sender, args) => ApplyEditorColumnShare(_settings.EditorColumnShare);
    }

    /// <summary>
    /// The window opens where it was closed last time, with the same size, maximised if it was (Daan, 2026-10-25). The
    /// first time, or when that place is not on any screen any more (a monitor was removed): the standard size in the
    /// middle of the screen.
    /// </summary>
    private void RestoreWindowPlace()
    {
        var titleBar = new Rectangle(_settings.WindowLeft, _settings.WindowTop, LogicalToDeviceUnits(200), LogicalToDeviceUnits(30));
        bool known = _settings.WindowWidth > 0 && Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(titleBar));
        if (!known)
        {
            FitToArea(Screen.FromControl(this).WorkingArea);
        }
        else
        {
            // First the place, then the size: on a monitor with another scaling Windows scales the window when it arrives
            // there, so the size (saved at 100%) is converted with the scaling of THAT monitor.
            Location = new Point(_settings.WindowLeft, _settings.WindowTop);
            Size = new Size(LogicalToDeviceUnits(_settings.WindowWidth), LogicalToDeviceUnits(_settings.WindowHeight));

            Rectangle area = Screen.FromControl(this).WorkingArea;
            if (Width > area.Width || Height > area.Height)
            {
                FitToArea(area); // a window bigger than this screen (it was on a bigger one): fit it in
            }
        }

        if (_settings.WindowMaximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    /// <summary>
    /// Remembers the place of the window for the next start (<see cref="RestoreWindowPlace"/>). When it is maximised (or
    /// minimised), the place and size of the normal window are remembered, so un-maximising later goes back there.
    /// </summary>
    private void RememberWindowPlace()
    {
        Rectangle normal = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _settings.WindowMaximized = WindowState == FormWindowState.Maximized;
        _settings.WindowLeft = normal.Left;
        _settings.WindowTop = normal.Top;
        _settings.WindowWidth = (int)Math.Round(normal.Width * 96.0 / DeviceDpi);  // at 100%: right for any monitor
        _settings.WindowHeight = (int)Math.Round(normal.Height * 96.0 / DeviceDpi);
        TrySaveSettings();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // A file given at the start; otherwise the last files, quietly (they may have been moved or deleted since).
        // A label-csv that is gone: the template is opened without data; a template that is gone: only the csv.
        if (_fileToOpen != null)
        {
            OpenChosenFile(_fileToOpen);
            return;
        }

        string? csv = _settings.LastCsvFile != "" && File.Exists(_settings.LastCsvFile) ? _settings.LastCsvFile : null;
        if (_settings.LastFile == "" || !OpenLabelFiles(_settings.LastFile, csv, quiet: true))
        {
            if (csv != null)
            {
                OpenCsvFile(csv, quiet: true);
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !ConfirmLeavingChanges())
        {
            e.Cancel = true;
        }
        else
        {
            RememberWindowPlace();
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _previewTimer.Dispose();
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }
}
