using System.Drawing.Printing;

namespace DssLabelTemplate;

/// <summary>
/// The only window of the program. Since 2.0.0 in the "Maestro" skin (Daan, 2026-11-21: the customer works all day with
/// the SCM machine software Maestro and wanted the same look; mockup "DssLabelTemplate Maestro skin"). Top to bottom:
///   the top bar:   at the left a white tab with the logo and the program name; at the right the language, the credit and
///                  the version, the ROGIERS logo (Shift+click: settings.json) and the round user-guide button;
///   the files:     a white panel with the two label files, each with a path box and a Browse button: the template or
///                  label-xml (shown in the editor) and the label-csv (the data for a template), and a status line;
///   two panels:    left the text of the label-xml or template, the csv table or the board (three tabs in its title, and
///                  "Save template" just below "Template (xml)" after a change); right the preview of one label on a grey
///                  work area with the label size in its title, and below it two tiles: where the label sits and its sheet.
///                  The grey gap between the two panels can be dragged;
///   the bottom bar: a blue band with round buttons (the csv table, the board, previous, the counter, next) and on the grey
///                  at its right the print button. Made for a touch screen.
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
    private DateTime _settingsFileTime;   // the date of settings.json when it was last read (see OnActivated)

    // The "Magic parameters" over the window while they are open (null when closed), see MagicParameters.cs.
    private DimOverlay? _dim;
    private FormWindowState _lastWindowState;

    // The size of the window when it opens, at 100%. Wider than the 940 x 780 of 1.x: the top bar holds the title tab, the
    // language, the credit, the logo, the user guide, the clock and the window buttons on one line.
    private const int WindowWidth = 1180;
    private const int WindowHeight = 820;
    private static readonly Size SmallestWindow = new(960, 640);

    // The window has no Windows frame (Daan, 2026-11-21: "the windows top bar should not be shown"). Around it a thin edge
    // where it can be resized with the mouse (not while maximised), and the window buttons in the top bar.
    private const int ResizeEdge = 4;
    private static readonly Size WindowButtonSize = new(40, 40);

    // The bars and the room around the white panels (at 100%, Daan's mockup scaled to this window).
    private const int TopBarHeight = 76;
    private const int PanelGap = 14;       // the grey room between and around the white panels
    private const int PanelPadding = 16;   // inside a white panel, left of its title
    private const int HeaderHeight = 44;   // the title row of the two big panels (the tabs are as high)

    // The two Browse buttons and the path boxes next to them (Daan, 2026-11-08, "option A"): they are used a lot on the
    // touch screen, so 44 high (about 1 cm, the usual smallest size for a finger). The width follows the text (FitButtonWidths).
    private static readonly Size FileButtonSize = new(150, 44);

    // The round buttons of the bottom bar (the csv table, the board, the arrows) and the print button next to it: big, made
    // for a touch screen at the reprint station. The counter "2 / 3" between the arrows: twice the window's font, bold.
    // The csv and board buttons are 20% bigger: their disc is drawn 60 at rest and fills the 72 when it is chosen (Daan,
    // 2026-11-21), so the button itself never changes size. Below the chosen one the bar draws a short white line.
    private static readonly Size DiscSize = new(60, 60);
    private static readonly Size GrowingDiscSize = new(72, 72);
    private static readonly Size PrintButtonSize = new(170, 56);
    private const float CounterFontFactor = 2f;

    // Dragging the gap between the panels: the preview panel keeps at least this width (at 100%), so the label size still
    // fits next to a few letters of its title. (For the editor panel see EditorColumnMinimum.)
    private const int MinimumColumnWidth = 330;

    // The two fields of the label size, in the preview's title: room for "1000,5".
    private static readonly Size LabelSizeBoxSize = new(56, 32);

    // "Save template", just below "Template (xml)" (Daan, 2026-11-21), only after a change. Its width follows its text.
    private const int SaveButtonHeight = 34;

    // The csv table, for a touch screen: its font this many points bigger than the window's, and every row this many
    // pixels (at 100%) higher than its text.
    private const float CsvExtraPoints = 4;
    private const int CsvRowExtraHeight = 14;

    // The ROGIERS logo and the user-guide "?" in the top bar (at 100%): as high as the clock sign, 18 (Daan, 2026-11-21).
    // The "?" button itself is a little bigger than its 18 px disc, so it is still easy to hit.
    private static readonly Size LogoSize = new(24, 18);
    private static readonly Size UserGuideSize = new(28, 28);

    // The "?" icon before each path caption (at 100%), and the longest line of its hover text (in characters): a WinForms
    // tooltip does not wrap by itself, a paragraph would be one line as wide as the screen.
    private static readonly Size HowToIconSize = new(20, 20);

    // The "v" before each file line (folds the line up), at 100%. A folded line is as high as this button, so it is not
    // bigger than needed: each folded line gives the panels below about 20 px.
    private static readonly Size FoldButtonSize = new(24, 24);
    internal const int HowToLineLength = 80;

    // The preview follows the text this long after the last key press (not after every letter).
    private const int PreviewDelayMilliseconds = 300;

    // The controls we need to read or fill (internal: the tests check them)
    internal readonly TableLayoutPanel TopBar = new();
    internal readonly TitleTab TitleTab = new();     // the white tab at the top left: logo, name, subtitle
    internal readonly Label LanguageCaption = new();
    internal readonly ComboBox LanguageBox = new();
    internal readonly Label CreditLabel = new();
    internal readonly Label VersionLabel = new();
    internal readonly DiscButton UserGuideButton = new(); // the round navy "?" at the top right: opens the user guide
    internal readonly DiscButton MagicParametersButton = new(); // the cog right of the "?": opens the Magic parameters (2.0.1)
    internal readonly ClockDisplay Clock = new();         // the time, at the top right (like Maestro)
    internal readonly WindowButton MinimizeButton = new() { Kind = WindowButtonKind.Minimize }; // the window has no Windows title bar:
    internal readonly WindowButton MaximizeButton = new() { Kind = WindowButtonKind.Maximize }; // its three buttons are in the top bar
    internal readonly WindowButton CloseButton = new() { Kind = WindowButtonKind.Close };
    internal readonly Label FilesCaption = new();    // "Label files", the title of the white panel with the two file lines
    internal readonly PictureBox FilesHowToPicture = new(); // the "?" after "Label files": the two kinds of label files
    internal readonly FoldButton XmlFoldButton = new();   // the "v" before "Template of label-xml:": folds that line up
    internal readonly FoldButton CsvFoldButton = new();   // the same before "Label-csv:"
    internal readonly Label XmlFoldedName = new();        // a folded line shows only its caption and the file name
    internal readonly Label CsvFoldedName = new();
    internal readonly Label XmlFileCaption = new();  // "Template of label-xml:"
    internal readonly TextBox XmlFileBox = new();    // the path of the file in the editor
    internal readonly RoundedButton XmlBrowseButton = new();
    internal readonly Label CsvFileCaption = new();  // "Label-csv:"
    internal readonly TextBox CsvFileBox = new();    // the path of the label-csv with the data for a template
    internal readonly RoundedButton CsvBrowseButton = new();
    internal readonly Label LabelSizeCaption = new(); // "Labelformaat (mm):", in the preview's title
    internal readonly TextBox LabelLengthBox = new(); // the label length in mm (horizontal, X)
    internal readonly TextBox LabelWidthBox = new();  // the label width in mm (vertical, Y)
    internal readonly TableLayoutPanel LabelSizePanel = new(); // the caption and the two fields, right of the preview title
    internal readonly TitleLabel EditorCaption = new(); // with the csv's file name in bold after "Data =" (Daan, 2026-11-21)
    internal readonly TabButton XmlViewButton = new();  // the template text (only with a label-csv); the table and the board: round buttons below
    internal readonly TableLayoutPanel EditorHeader = new(); // the title of the left panel with "Template (xml)", and save below it
    internal readonly Panel EditorArea = new();             // holds the editor, the csv table and the board, one of them visible
    internal readonly DataGridView CsvGrid = new();         // the label-csv, read-only, one row per label
    internal readonly Label PreviewCaption = new();
    internal readonly InfoTile PositionTile = new(); // below the preview: X, Y, ID and rotation of the shown label
    internal readonly InfoTile SheetTile = new();    // below the preview: the size of its sheet
    internal readonly TableLayoutPanel Tiles = new(); // the two tiles side by side
    internal readonly Label LabelCounter = new();    // "2 / 3", between the arrows in the bottom bar
    internal readonly DiscButton PreviousButton = new();
    internal readonly DiscButton NextButton = new();
    internal readonly PillButton PrintButton = new();  // at the bottom right: prints the shown label (or more)
    internal readonly DiscButton CsvViewButton = new(); // in the bottom bar: the csv table (the same as the csv tab)
    internal readonly DiscButton BoardButton = new();   // in the bottom bar: the board (a toggle, the same as the board tab)
    internal readonly SheetBoard Board;                  // the board view: the sheet with all label positions, big
    internal readonly BottomBar BottomBar;               // the blue bar at the bottom with the round buttons and print
    internal readonly TableLayoutPanel PreviewHeader = new(); // the preview title with the label size
    internal readonly XmlEditor Editor = new();
    internal readonly LabelPreview Preview = new();
    internal readonly ColumnSplitter ColumnDivider = new(); // the grey gap between the two panels, can be dragged
    internal readonly TableLayoutPanel Workspace = new();   // the two panels and the gap between them
    internal readonly PictureBox LogoPicture = new();       // the ROGIERS logo in the top bar
    internal readonly RoundedButton SaveButton = new(); // "Template opslaan", just below "Template (xml)", only after a change

    private readonly ToolTip _toolTip = new();

    // The hover text of the "?" icons is long: it stays up longer than the standard 5 seconds (32767 ms is the maximum).
    internal readonly ToolTip HowToToolTip = new() { AutoPopDelay = 32767, InitialDelay = 200 };
    private readonly System.Windows.Forms.Timer _previewTimer = new() { Interval = PreviewDelayMilliseconds };
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 5000 }; // the clock shows minutes

    // The open file. Null = nothing opened yet (Save then asks for a name).
    private string? _currentFile;
    private TextFileContent? _loaded;   // how the file was written (encoding, line endings), to save it the same way
    private bool _modified;             // changed since it was opened or saved
    private bool _loading;              // filling the editor from a file: that is not a change by the user
    private LabelCsv? _csv;             // the label-csv that goes with the template in the editor; null for a label-xml
    private bool _isLabelXml;           // the editor holds a valid label-xml: then a label-csv is not used
    private bool _showingCsv;           // the left column shows the csv table instead of the template text
    private bool _showingBoard;         // the left column shows the board (over the text or the table, which stays chosen)
    private bool _selectingCsvRow;      // the program (not the user) selects a row of the csv table
    private bool _fillingCsvGrid;       // the program (not the operator) adds and orders the columns of the csv table
    private bool _csvColumnOrderSavePending; // a dragged column order will be saved right after the drop

    /// <summary>Internal: the file that is open now (the tests check it).</summary>
    internal string? CurrentFile => _currentFile;

    /// <summary>Internal: true when the left column shows the csv table.</summary>
    internal bool ShowingCsv => _showingCsv;

    /// <summary>Internal: true when the left column shows the board.</summary>
    internal bool ShowingBoard => _showingBoard;

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
        Board = new SheetBoard(Preview); // the board draws what the preview knows
        BottomBar = new BottomBar(CsvViewButton, BoardButton, PreviousButton, LabelCounter, NextButton, PrintButton);

        // The settings were just read from settings.json: a later change of the file is noticed in OnActivated.
        string? settingsFile = SettingsFilePath;
        _settingsFileTime = settingsFile != null && File.Exists(settingsFile) ? File.GetLastWriteTimeUtc(settingsFile) : DateTime.MinValue;

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
        ApplyHandEditedSettings(); // the settings that are only in settings.json, not in the window

        // The file lines folded as the user left them (after the window was built: the heights are then in pixels).
        FoldFileLine(xmlLine: true, _settings.XmlFileLineFolded, save: false);
        FoldFileLine(xmlLine: false, _settings.CsvFileLineFolded, save: false);
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
        Font = Theme.CreateBodyFont(); // Verdana, like Maestro; all controls below inherit it
        Icon = AppIcons.ForWindow();
        FormBorderStyle = FormBorderStyle.None; // no Windows title bar: the top bar is the title bar (CreateParams, WndProc)
        Padding = ResizePadding(ResizeEdge);    // the thin edge to resize with (0 while maximised: OnSizeChanged)
        BackColor = Theme.WindowEdge;           // only that edge shows the form's own colour
        ForeColor = Theme.Text;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(WindowWidth, WindowHeight);
        MinimumSize = SmallestWindow;

        // In between the two bars: the white panel with the files, and below it the two big panels (they get the rest of
        // the height), on the grey frame.
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(PanelGap),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Frame,
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(BuildFilesPanel(), 0, 0);
        Control workspace = BuildWorkspace();
        workspace.Margin = new Padding(0, PanelGap, 0, 0);
        body.Controls.Add(workspace, 0, 1);

        // Docking: the control that is added LAST is placed first (the bars at the top and bottom, the body in between).
        Controls.Add(body);
        Controls.Add(BuildBottomBar());
        Controls.Add(BuildTopBar());

        SetUpTabOrder();
    }

    /// <summary>
    /// The top bar (it replaces the blue banner and the language row of 1.x): at the left the white title tab with the logo
    /// and the program name; at the right the language (title above the box), a short line, the credit and the version (two
    /// small lines), the ROGIERS logo and the round user-guide button (Daan's mockup).
    /// </summary>
    private Control BuildTopBar()
    {
        TitleTab.Text = Strings.AppName;
        TitleTab.Subtitle = Strings.BannerSubtitle;
        TitleTab.Dock = DockStyle.Fill; // the rest of the width; the white tab is as wide as its text (or narrower)
        TitleTab.Margin = new Padding(0);

        Panel Separator() => new() { Size = new Size(1, 40), BackColor = Theme.Separator, Anchor = AnchorStyles.Left, Margin = new Padding(16, 0, 14, 0) };

        TopBar.Dock = DockStyle.Top;
        TopBar.Height = TopBarHeight;
        TopBar.BackColor = Theme.TopBar;
        TopBar.Padding = new Padding(0, 0, PanelGap, 0);
        TopBar.Margin = new Padding(0);
        TopBar.ColumnCount = 12;
        TopBar.RowCount = 1;
        TopBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 1; i < TopBar.ColumnCount; i++)
        {
            TopBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // fixed-size controls: never in a Percent column
        }

        TopBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        TopBar.Controls.Add(TitleTab, 0, 0);
        TopBar.Controls.Add(BuildLanguageColumn(), 1, 0);
        TopBar.Controls.Add(Separator(), 2, 0);
        TopBar.Controls.Add(BuildCredits(), 3, 0);
        TopBar.Controls.Add(BuildLogo(), 4, 0);
        TopBar.Controls.Add(BuildUserGuideButton(), 5, 0);
        TopBar.Controls.Add(BuildMagicParametersButton(), 6, 0);
        TopBar.Controls.Add(Separator(), 7, 0);
        TopBar.Controls.Add(BuildClock(), 8, 0);
        TopBar.Controls.Add(MinimizeButton, 9, 0);
        TopBar.Controls.Add(MaximizeButton, 10, 0);
        TopBar.Controls.Add(CloseButton, 11, 0);

        foreach (WindowButton button in new[] { MinimizeButton, MaximizeButton, CloseButton })
        {
            button.Size = WindowButtonSize;
            button.Anchor = AnchorStyles.Left;
            button.Margin = new Padding(6, 0, 0, 0);
        }

        MinimizeButton.Click += (sender, e) => WindowState = FormWindowState.Minimized;
        MaximizeButton.Click += (sender, e) => ToggleMaximized();
        CloseButton.Click += (sender, e) => Close(); // still asks about unsaved changes (OnFormClosing)

        // The top bar is the title bar: dragging it moves the window, a double-click maximises it (or back). Only on the
        // bar itself and the parts that do nothing on a click (the title tab, the credit, the clock).
        foreach (Control handle in new Control[] { TopBar, TitleTab, CreditLabel, VersionLabel, Clock, LanguageCaption })
        {
            handle.MouseDown += (sender, e) => DragWindowFromTopBar(e);
        }

        return TopBar;
    }

    /// <summary>The clock: the time now, again every few seconds (it shows minutes, so it is never more than seconds late).</summary>
    private Control BuildClock()
    {
        Clock.Anchor = AnchorStyles.Left;
        Clock.Margin = new Padding(0, 0, 10, 0);
        Clock.Size = new Size(80, 30); // the width follows the font (FitButtonWidths)
        Clock.Time = DateTime.Now.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        _clockTimer.Tick += (sender, e) => Clock.Time = DateTime.Now.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        _clockTimer.Start();
        return Clock;
    }

    /// <summary>The maximise button (and a double-click on the top bar): maximised ↔ the normal window. In full screen: back.</summary>
    internal void ToggleMaximized()
    {
        if (_fullScreen)
        {
            ToggleFullScreen();
            return;
        }

        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
    }

    private bool _fullScreen;
    private FormWindowState _stateBeforeFullScreen;

    /// <summary>Internal: true while the window covers the whole monitor, taskbar included (F11).</summary>
    internal bool IsFullScreen => _fullScreen;

    /// <summary>
    /// F11, like a browser (Daan, 2026-11-21): the window covers the WHOLE monitor, the Windows taskbar included; F11 again
    /// (or the maximise button) goes back to how it was. It is a maximised window whose "maximised" size is the whole
    /// monitor instead of its working area (WndProc, WM_GETMINMAXINFO). Not remembered for the next start.
    /// </summary>
    internal void ToggleFullScreen()
    {
        if (!_fullScreen)
        {
            _stateBeforeFullScreen = WindowState == FormWindowState.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
            _fullScreen = true;
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal; // maximising again asks Windows for the (new) maximised size
            }

            WindowState = FormWindowState.Maximized;
        }
        else
        {
            _fullScreen = false;
            WindowState = FormWindowState.Normal;
            WindowState = _stateBeforeFullScreen;
        }
    }

    /// <summary>
    /// The mouse went down on the top bar: Windows moves the window from here, as if the title bar had been grabbed (also
    /// snapping to a screen edge, and moving to another monitor). A double-click maximises it, or back.
    /// </summary>
    private void DragWindowFromTopBar(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (e.Clicks == 2)
        {
            ToggleMaximized();
            return;
        }

        NativeWindowMethods.ReleaseCapture();
        NativeWindowMethods.SendMessage(Handle, NativeWindowMethods.WM_NCLBUTTONDOWN, NativeWindowMethods.HTCAPTION, IntPtr.Zero);
    }

    /// <summary>
    /// Without a Windows frame a window cannot be minimised from the taskbar and does not get the minimise / maximise
    /// animation: these two styles give them back (they draw nothing).
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.Style |= NativeWindowMethods.WS_MINIMIZEBOX | NativeWindowMethods.WS_MAXIMIZEBOX | NativeWindowMethods.WS_SYSMENU;
            return parameters;
        }
    }

    /// <summary>
    /// Two Windows messages for a window without a frame: where the mouse may resize it (the thin edge, the corners a
    /// little bigger), and how big "maximised" is (the working area of ITS monitor, so the taskbar stays visible).
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeWindowMethods.WM_GETMINMAXINFO)
        {
            NativeWindowMethods.LimitMaximizedToWorkingArea(Handle, m.LParam, wholeMonitor: _fullScreen);
        }

        base.WndProc(ref m);

        if (m.Msg == NativeWindowMethods.WM_NCHITTEST && WindowState == FormWindowState.Normal && (int)m.Result == NativeWindowMethods.HTCLIENT)
        {
            Point point = PointToClient(new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16))));
            int edge = LogicalToDeviceUnits(ResizeEdge), corner = LogicalToDeviceUnits(ResizeEdge * 4);
            bool left = point.X < edge, right = point.X >= ClientSize.Width - edge;
            bool top = point.Y < edge, bottom = point.Y >= ClientSize.Height - edge;
            bool nearLeft = point.X < corner, nearRight = point.X >= ClientSize.Width - corner;
            bool nearTop = point.Y < corner, nearBottom = point.Y >= ClientSize.Height - corner;
            int hit = (top && nearLeft) || (left && nearTop) ? NativeWindowMethods.HTTOPLEFT
                : (top && nearRight) || (right && nearTop) ? NativeWindowMethods.HTTOPRIGHT
                : (bottom && nearLeft) || (left && nearBottom) ? NativeWindowMethods.HTBOTTOMLEFT
                : (bottom && nearRight) || (right && nearBottom) ? NativeWindowMethods.HTBOTTOMRIGHT
                : left ? NativeWindowMethods.HTLEFT
                : right ? NativeWindowMethods.HTRIGHT
                : top ? NativeWindowMethods.HTTOP
                : bottom ? NativeWindowMethods.HTBOTTOM
                : NativeWindowMethods.HTCLIENT;
            m.Result = (IntPtr)hit;
        }
    }

    /// <summary>
    /// The resize edge at the left, right and bottom; NOT at the top, so the white title tab sticks to the top of the window
    /// (Daan, 2026-11-21). The top corners still resize (the side edges reach up to them).
    /// </summary>
    private static Padding ResizePadding(int edge) => new(edge, 0, edge, edge);

    /// <summary>Maximised: no resize edge (it would show as a grey line around the screen), and the restore sign.</summary>
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        bool maximised = WindowState == FormWindowState.Maximized;
        Padding edge = maximised ? Padding.Empty : ResizePadding(LogicalToDeviceUnits(ResizeEdge));
        if (Padding != edge && WindowState != FormWindowState.Minimized)
        {
            Padding = edge;
        }

        MaximizeButton.Maximised = maximised;

        // Maximised or restored while the Magic parameters are open: a new picture for the dim layer. (While the edge is
        // being dragged the old one is stretched; OnResizeEnd takes a new one, so it is not taken many times a second.)
        if (WindowState != _lastWindowState)
        {
            _lastWindowState = WindowState;
            if (WindowState != FormWindowState.Minimized)
            {
                _dim?.RefreshBackdrop();
            }
        }
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        base.OnResizeEnd(e);
        _dim?.RefreshBackdrop();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (_dim != null)
        {
            BeginInvoke(() => _dim?.RefreshBackdrop()); // after WinForms has scaled the window to the other monitor
        }
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

    /// <summary>In the top bar: the language title (in all three languages) above the language box.</summary>
    private Control BuildLanguageColumn()
    {
        LanguageCaption.Text = Strings.LanguageTitle;
        LanguageCaption.AutoSize = true;
        LanguageCaption.ForeColor = Theme.Text;
        LanguageCaption.Margin = new Padding(0, 0, 0, 3);

        LanguageBox.DropDownStyle = ComboBoxStyle.DropDownList; // choose from the list, no typing
        LanguageBox.FlatStyle = FlatStyle.Flat;                 // a flat grey field, like the path boxes
        LanguageBox.BackColor = Theme.Field;
        LanguageBox.Items.Add(new LanguageItem(AppLanguage.Dutch, Strings.LanguageDutch));
        LanguageBox.Items.Add(new LanguageItem(AppLanguage.French, Strings.LanguageFrench));
        LanguageBox.Items.Add(new LanguageItem(AppLanguage.English, Strings.LanguageEnglish));
        LanguageBox.Width = 200;
        LanguageBox.Anchor = AnchorStyles.Left;
        LanguageBox.ForeColor = Theme.Text;
        LanguageBox.Margin = new Padding(0);

        // One AutoSize column (a Percent column in an AutoSize table would shrink the drop-down); in the middle of the
        // bar's height (anchored left only).
        var column = new TableLayoutPanel { ColumnCount = 1, RowCount = 2, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(16, 0, 0, 0) };
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        column.Controls.Add(LanguageCaption, 0, 0);
        column.Controls.Add(LanguageBox, 0, 1);
        return column;
    }

    /// <summary>In the top bar: "Dev.: Daan Verhoost" above "App version: 2.0.0", small, against the right (Daan's mockup).</summary>
    private Control BuildCredits()
    {
        var small = Theme.CreateFont(Font, Font.SizeInPoints - 1.5f);
        foreach (Label label in new[] { CreditLabel, VersionLabel })
        {
            label.AutoSize = true;
            label.Font = small;
            label.ForeColor = Theme.Text;
            label.Anchor = AnchorStyles.Right;
            label.Margin = new Padding(0, 1, 0, 1);
        }

        CreditLabel.Text = Strings.DeveloperCredit;

        var column = new TableLayoutPanel { ColumnCount = 1, RowCount = 2, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0) };
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        column.Controls.Add(CreditLabel, 0, 0);
        column.Controls.Add(VersionLabel, 0, 1);
        return column;
    }

    /// <summary>The round navy "?" at the top right: opens the user guide (the GitHub page of this tool).</summary>
    private Control BuildUserGuideButton()
    {
        // Flat and grey like the clock sign (Daan, 2026-11-21).
        UserGuideButton.Flat = true;
        UserGuideButton.Glyph = DiscGlyph.Question;
        UserGuideButton.Size = UserGuideSize;
        UserGuideButton.Anchor = AnchorStyles.Left;
        UserGuideButton.Margin = new Padding(8, 0, 0, 0);
        UserGuideButton.Click += (sender, e) => OpenWebsite(Strings.UserGuideUrl);
        return UserGuideButton;
    }

    /// <summary>The cog right of the "?" (2.0.1, Daan): opens the Magic parameters. The same flat grey disc as the "?".</summary>
    private Control BuildMagicParametersButton()
    {
        MagicParametersButton.Flat = true;
        MagicParametersButton.Glyph = DiscGlyph.Cog;
        MagicParametersButton.Size = UserGuideSize;
        MagicParametersButton.Anchor = AnchorStyles.Left;
        MagicParametersButton.Margin = new Padding(4, 0, 0, 0);
        MagicParametersButton.Click += (sender, e) => OpenMagicParameters();
        return MagicParametersButton;
    }

    /// <summary>
    /// The white panel with the two label files: its title, the two file lines (<see cref="BuildFileRows"/>) and the status
    /// line. AutoSize: as high as its content.
    /// </summary>
    private Control BuildFilesPanel()
    {
        FilesCaption.AutoSize = true;
        FilesCaption.Font = Theme.CreateFont(Font, Font.SizeInPoints + 2);
        FilesCaption.ForeColor = Theme.Text;
        FilesCaption.Anchor = AnchorStyles.Left;
        FilesCaption.Margin = new Padding(0, 0, 8, 0);

        // The title and, right after it, the one "?" with the description of the two kinds of label files.
        SetUpHowToPicture(FilesHowToPicture);
        FilesHowToPicture.Anchor = AnchorStyles.Left;
        FilesHowToPicture.Margin = new Padding(0);
        var title = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0, 0, 0, 8) };
        title.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        title.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        title.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        title.Controls.Add(FilesCaption, 0, 0);
        title.Controls.Add(FilesHowToPicture, 1, 0);

        // No status line below the files any more (Daan, 2026-11-21): the number of labels is in the left panel's title,
        // what to do when nothing (or broken xml) is open is written in the preview.
        var panel = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            Dock = DockStyle.Fill,
            BackColor = Theme.White,
            Padding = new Padding(PanelPadding, 12, PanelPadding, 12),
            Margin = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(BuildFileRows(), 0, 1);
        return panel;
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
        LabelSizeCaption.ForeColor = Theme.Text;

        var times = new Label { Text = Strings.LabelSizeTimes, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 0, 4, 0), ForeColor = Theme.Text };

        // The size panel: AutoSize columns only (the frames have a fixed size).
        LabelSizePanel.ColumnCount = 4;
        LabelSizePanel.RowCount = 1;
        LabelSizePanel.AutoSize = true;
        LabelSizePanel.Anchor = AnchorStyles.Right;
        LabelSizePanel.Margin = new Padding(0, 0, 12, 0);
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

    /// <summary>One field of the label size: a flat grey field like the path boxes, the number on the right.</summary>
    private static TextBoxFrame LabelSizeFrame(TextBox box)
    {
        box.ForeColor = Theme.Text;
        box.TextAlign = HorizontalAlignment.Right;
        return GreyField(box, LabelSizeBoxSize);
    }

    /// <summary>A text box in the flat grey field of the Maestro skin (the path boxes, the label size).</summary>
    private static TextBoxFrame GreyField(TextBox box, Size size)
    {
        var frame = new TextBoxFrame(box) { Size = size, Margin = new Padding(0) };
        Theme.StyleField(frame, Theme.Field);
        box.BackColor = Theme.Field;
        return frame;
    }

    /// <summary>
    /// A field of the label size was changed: a valid number (1 – 1000 mm, dot or comma) is used at once and saved;
    /// anything else turns red and the preview keeps the last valid size.
    /// </summary>
    private void OnLabelSizeChanged(TextBox box)
    {
        bool valid = LabelXml.TryNumber(box.Text, out double millimetres) && AppSettings.IsValidLabelSize(millimetres);
        box.ForeColor = valid ? Theme.Text : Theme.Error;
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
    /// The two file lines, each a "v" (folds the line up, Daan 2026-11-21), a caption, a path box (Enter opens what was
    /// typed) and a Browse button:
    ///   "Template of label-xml:"  the file in the editor;
    ///   "Label-csv:"              the data for a template (not used, and greyed out, with a label-xml).
    /// A folded line keeps its "v" and caption and shows the path as one line of text instead of the box and Browse, so the
    /// panels below get that height.
    /// </summary>
    private Control BuildFileRows()
    {
        // Fold buttons, captions, path holders and Browse in AutoSize columns, only the path holders stretch (anchored left
        // and right in the Percent column), so the captions never wrap and the buttons are never shrunk.
        var rows = new TableLayoutPanel { ColumnCount = 4, RowCount = 2, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddFileLine(rows, 0, XmlFoldButton, XmlFileCaption, XmlFileBox, XmlFoldedName, XmlBrowseButton, OpenTypedXmlFile, BrowseForXmlFile);
        AddFileLine(rows, 1, CsvFoldButton, CsvFileCaption, CsvFileBox, CsvFoldedName, CsvBrowseButton, OpenTypedCsvFile, BrowseForCsvFile);
        XmlFoldButton.Click += (sender, e) => FoldFileLine(xmlLine: true, !XmlFoldButton.Folded, save: true);
        CsvFoldButton.Click += (sender, e) => FoldFileLine(xmlLine: false, !CsvFoldButton.Folded, save: true);
        return rows;
    }

    /// <summary>
    /// Folds a file line up (only the "v", the caption and the path as text) or opens it again (the path box and Browse),
    /// and remembers it in the settings when the user did it (<paramref name="save"/>).
    /// </summary>
    internal void FoldFileLine(bool xmlLine, bool folded, bool save)
    {
        FoldButton fold = xmlLine ? XmlFoldButton : CsvFoldButton;
        TextBox box = xmlLine ? XmlFileBox : CsvFileBox;
        Label name = xmlLine ? XmlFoldedName : CsvFoldedName;
        RoundedButton browse = xmlLine ? XmlBrowseButton : CsvBrowseButton;
        Control holder = name.Parent!;

        fold.Folded = folded;
        box.Parent!.Visible = !folded;
        name.Visible = folded;
        browse.Visible = !folded;
        holder.Height = folded ? name.Font.Height + LogicalToDeviceUnits(6) : browse.Height;
        _toolTip.SetToolTip(fold, folded ? Strings.UnfoldFileLine : Strings.FoldFileLine);
        fold.AccessibleName = folded ? Strings.UnfoldFileLine : Strings.FoldFileLine;

        if (save)
        {
            if (xmlLine)
            {
                _settings.XmlFileLineFolded = folded;
            }
            else
            {
                _settings.CsvFileLineFolded = folded;
            }

            TrySaveSettings();
        }
    }

    /// <summary>
    /// The "?" icon after "Label files" (Daan, 2026-11-21: one icon instead of one before each line). Its text (the two
    /// kinds of label files) appears when the mouse rests on it, and also when it is tapped or clicked: a touch screen has
    /// no mouse to rest.
    /// </summary>
    private void SetUpHowToPicture(PictureBox picture)
    {
        picture.Image = AppIcons.HowTo(); // a navy circle with a white "?", like the Maestro buttons
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
    private static void AddFileLine(TableLayoutPanel rows, int row, FoldButton fold, Label caption, TextBox box, Label foldedName, RoundedButton browse, Action openTyped, Action openBrowsed)
    {
        int top = row == 0 ? 0 : 8;

        fold.Size = FoldButtonSize;
        fold.Anchor = AnchorStyles.Left; // in the middle of the line's height, like the caption
        fold.Margin = new Padding(0, top, 6, 0);

        caption.AutoSize = true;
        caption.Anchor = AnchorStyles.Left; // in the middle of the line's height
        caption.Margin = new Padding(0, top, 12, 0);
        caption.ForeColor = Theme.Text;

        // The text box sits in a flat grey field that is as high as the Browse button, so both line up (the text in the
        // middle of the field's height). The field and the text of a folded line share a holder: one of them is visible,
        // and the holder is as high as that one (a fixed height, set by FoldFileLine; not stretched by the table).
        box.ForeColor = Theme.Text;
        TextBoxFrame frame = GreyField(box, new Size(FileButtonSize.Width, FileButtonSize.Height));
        frame.Dock = DockStyle.Fill;
        foldedName.Dock = DockStyle.Fill;
        foldedName.AutoSize = false;
        foldedName.AutoEllipsis = true; // one line, "…" at the end
        foldedName.TextAlign = ContentAlignment.MiddleLeft;
        foldedName.ForeColor = Theme.TextSoft;
        foldedName.Visible = false;
        box.TextChanged += (sender, e) => foldedName.Text = box.Text; // the folded line shows the same path
        var holder = new Panel
        {
            Height = FileButtonSize.Height,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, top, 10, 0),
            BackColor = Theme.White,
        };
        holder.Controls.Add(frame);
        holder.Controls.Add(foldedName);
        box.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // no "ding"
                openTyped();
            }
        };

        // A navy button with a white folder before its text (Daan's mockup).
        browse.Size = FileButtonSize;
        browse.Margin = new Padding(0, top, 0, 0);
        browse.Image = AppIcons.Folder();
        browse.IconHeight = 16;
        Theme.StyleNavyButton(browse);
        browse.Click += (sender, e) => openBrowsed();

        rows.Controls.Add(fold, 0, row);
        rows.Controls.Add(caption, 1, row);
        rows.Controls.Add(holder, 2, row);
        rows.Controls.Add(browse, 3, row);
    }

    /// <summary>
    /// The two big panels with a grey gap between them. Left a white panel with the template text, the csv table or the
    /// board (tabs in its title); right a white panel with the preview on its grey work area, and below it the two tiles.
    /// The gap can be dragged to make one panel wider (the share is remembered in settings.json); a double-click gives two
    /// equal halves again. This table is not AutoSize (it fills the rest of the window), so its controls may stretch.
    /// </summary>
    private Control BuildWorkspace()
    {
        // The left panel: the xml editor (XmlEditor.cs), for a label-csv the csv table, and the board; one is visible.
        // A thin grey line between the title and them (the area's top padding on the line colour).
        Editor.TextChanged += (sender, e) => OnEditorChanged();
        BuildCsvGrid();
        EditorArea.Dock = DockStyle.Fill;
        EditorArea.Margin = new Padding(0);
        EditorArea.Padding = new Padding(0, 1, 0, 0);
        EditorArea.BackColor = Theme.GridLine;
        var gridFrame = new ContentFrame(CsvGrid) { Dock = DockStyle.Fill, Visible = false, Inset = 0 };
        Theme.StyleField(gridFrame, Theme.White);
        var editorFrame = new ContentFrame(Editor) { Dock = DockStyle.Fill, Inset = 4 };
        Theme.StyleField(editorFrame, Theme.XmlBackground); // the grey of the editor all the way to the edge
        EditorArea.Controls.Add(gridFrame);
        EditorArea.Controls.Add(editorFrame);

        // The board: on its own grey work area, like the preview (no frame around it).
        Board.Dock = DockStyle.Fill;
        Board.Margin = new Padding(0);
        Board.Visible = false;
        Board.LabelChosen += ShowLabel; // a tapped label is shown in the preview
        EditorArea.Controls.Add(Board);

        var leftPanel = WhitePanel();
        leftPanel.Controls.Add(BuildEditorHeader(), 0, 0);
        leftPanel.Controls.Add(EditorArea, 0, 1);

        // The handle is the grey gap between the two panels. Its column is AutoSize: a fixed-size control in a Percent
        // column would be shrunk.
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
        Workspace.RowCount = 1;
        Workspace.Dock = DockStyle.Fill;
        Workspace.Margin = new Padding(0);
        Workspace.BackColor = Theme.Frame;
        float editorPercent = (float)(_settings.EditorColumnShare * 100); // the share of the last time (0.5 = equal halves)
        Workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, editorPercent));
        Workspace.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - editorPercent));
        Workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Workspace.Controls.Add(leftPanel, 0, 0);
        Workspace.Controls.Add(ColumnDivider, 1, 0);
        Workspace.Controls.Add(BuildPreviewColumn(), 2, 0);
        return Workspace;
    }

    /// <summary>A white panel: its title row (AutoSize) above its content (the rest of the height).</summary>
    private static TableLayoutPanel WhitePanel()
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
            BackColor = Theme.White,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return panel;
    }

    /// <summary>
    /// The right column: the white panel with the preview (its title with the label size, the label on the grey work area)
    /// and below it the two tiles (Daan's mockup): where the shown label sits, and the size of its sheet.
    /// </summary>
    private Control BuildPreviewColumn()
    {
        Preview.Dock = DockStyle.Fill;
        Preview.Margin = new Padding(0);

        var previewPanel = WhitePanel();
        previewPanel.Controls.Add(BuildPreviewHeader(), 0, 0);
        previewPanel.Controls.Add(Preview, 0, 1);

        // The tiles: a table that is NOT AutoSize (two fixed-height tiles in its Percent columns), as high as a tile
        // (FitTiles), anchored left and right in an AutoSize row of the column.
        foreach (InfoTile tile in new[] { PositionTile, SheetTile })
        {
            tile.Dock = DockStyle.Fill;
            tile.FontChanged += (sender, e) => FitTiles();
        }

        PositionTile.Margin = new Padding(0, 0, PanelGap / 2, 0);
        SheetTile.Margin = new Padding(PanelGap / 2, 0, 0, 0);
        Tiles.ColumnCount = 2;
        Tiles.RowCount = 1;
        Tiles.AutoSize = false;
        Tiles.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        Tiles.Margin = new Padding(0, PanelGap, 0, 0);
        Tiles.BackColor = Theme.Frame;
        Tiles.Height = 100; // until FitTiles knows the fonts
        Tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        Tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        Tiles.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Tiles.Controls.Add(PositionTile, 0, 0);
        Tiles.Controls.Add(SheetTile, 1, 0);

        var column = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
            BackColor = Theme.Frame,
            Margin = new Padding(0),
        };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        column.Controls.Add(previewPanel, 0, 0);
        column.Controls.Add(Tiles, 0, 1);
        return column;
    }

    /// <summary>The tiles get the height their fonts need (again after a move to a screen with another scaling).</summary>
    private void FitTiles()
    {
        int height = Math.Max(PositionTile.HeightNeeded(), SheetTile.HeightNeeded());
        if (Tiles.Height != height)
        {
            Tiles.Height = height;
        }
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
    /// really used. The editor keeps at least <see cref="EditorColumnMinimum"/>, the preview
    /// <see cref="MinimumColumnWidth"/>; when the window is too narrow for both, they share the room in that proportion.
    /// It does NOT change the settings: in a window that is (for a moment) too small for the share the user chose, that
    /// share must stay remembered (until 1.1.0 it was overwritten by the squeezed one at the next start, Daan 2026-10-25).
    /// </summary>
    private double ApplyEditorColumnShare(double share)
    {
        int available = ColumnsWidth();
        if (available > 0)
        {
            double editorMinimum = EditorColumnMinimum();
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

    /// <summary>The width of the two columns together (the workspace without the gap between them).</summary>
    private int ColumnsWidth() => Workspace.ClientSize.Width - Workspace.Padding.Horizontal - ColumnDivider.Width;

    /// <summary>
    /// The left panel is never narrower than its "Template (xml)" tab (or the save button below it, if wider) with a little
    /// room for its title, which may then shrink to "…". In pixels; the widths follow their texts (FitButtonWidths).
    /// </summary>
    private int EditorColumnMinimum() =>
        Math.Max(XmlViewButton.Width, SaveButton.Width) + LogicalToDeviceUnits(2 * PanelPadding + 120);

    /// <summary>
    /// The title of the left panel and, against its right edge, the tab "Template (xml)" (the template text; only with a
    /// label-csv). The csv table and the board are chosen with the round buttons of the bottom bar (Daan, 2026-11-21: the
    /// "Table" and "Board" tabs of the mockup were removed, "since the buttons on the bar below work"). Just below
    /// "Template (xml)" the navy "Save template", only while the text has unsaved changes (Daan, 2026-11-21: "just below
    /// 'modele (xml)' a button should appear to save it if the user edits the XML"); the row below the tab is then a little
    /// higher, the right panel does not move.
    /// </summary>
    private Control BuildEditorHeader()
    {
        EditorCaption.Font = Theme.CreateFont(Font, Font.SizeInPoints + 2);
        EditorCaption.ForeColor = Theme.Text;
        MakeOneLineTitle(EditorCaption);
        EditorCaption.Margin = new Padding(PanelPadding, 0, 8, 0);

        XmlViewButton.IdleFill = Theme.XmlTabIdle;
        XmlViewButton.Height = HeaderHeight;
        XmlViewButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        XmlViewButton.Margin = new Padding(0);
        XmlViewButton.FontChanged += (sender, e) => FitButtonWidthsLater();
        // Also closes the board: the user chose what the left column must show.
        XmlViewButton.Click += (sender, e) => { _showingBoard = false; ShowCsvView(false); };

        // Below "Template (xml)", against the right edge like the tab; it may be wider than the tab.
        SaveButton.Height = SaveButtonHeight;
        SaveButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        SaveButton.Margin = new Padding(0, 8, 0, 8);
        SaveButton.Visible = false; // nothing changed yet
        Theme.StyleNavyButton(SaveButton);
        SaveButton.Click += (sender, e) => SaveFile();
        // No AcceptButton: Enter in the path box opens the file, Enter in the editor starts a new line.

        EditorHeader.ColumnCount = 2;
        EditorHeader.RowCount = 2;
        EditorHeader.AutoSize = true;
        EditorHeader.Dock = DockStyle.Fill;
        EditorHeader.Margin = new Padding(0);
        EditorHeader.MinimumSize = new Size(0, HeaderHeight);
        EditorHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        EditorHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        EditorHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        EditorHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        EditorHeader.Controls.Add(EditorCaption, 0, 0);
        EditorHeader.Controls.Add(XmlViewButton, 1, 0);
        EditorHeader.Controls.Add(SaveButton, 1, 1);
        return EditorHeader;
    }

    /// <summary>
    /// The buttons whose width follows their text (Browse, the tabs, save, print) get it: after a language change, and
    /// after a move to a screen with another scaling (then later, when WinForms has finished scaling the window: a width
    /// measured in the middle of that would be scaled a second time).
    /// </summary>
    private void FitButtonWidthsLater()
    {
        if (IsHandleCreated)
        {
            BeginInvoke(FitButtonWidths);
        }
    }

    private void FitButtonWidths()
    {
        foreach (RoundedButton browse in new[] { XmlBrowseButton, CsvBrowseButton })
        {
            browse.Width = Math.Max(LogicalToDeviceUnits(FileButtonSize.Width), Math.Max(XmlBrowseButton.WidthNeededForIconAndText(), CsvBrowseButton.WidthNeededForIconAndText()));
        }

        // "Template (xml)" as wide as its text; "Save template" below it at least as wide as the tab.
        XmlViewButton.Width = XmlViewButton.WidthNeeded();
        SaveButton.Width = Math.Max(XmlViewButton.Width, TextRenderer.MeasureText(SaveButton.Text, SaveButton.Font).Width + LogicalToDeviceUnits(28));

        PrintButton.Width = Math.Max(LogicalToDeviceUnits(PrintButtonSize.Width), PrintButton.WidthNeeded());
        Clock.Width = Clock.WidthNeeded();
        Clock.Height = Clock.HeightNeeded();
        LabelCounter.Width = CounterWidthNeeded();
        LabelCounter.Height = LabelCounter.Font.Height + LogicalToDeviceUnits(4);
        FitTiles();
        BottomBar.PerformLayout();
        ApplyEditorColumnShare(_settings.EditorColumnShare); // the minimum width of the left panel may have changed
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
        // The operator may drag the column titles into another order (Daan, 2026-10-27); it is kept in settings.json.
        CsvGrid.AllowUserToOrderColumns = true;
        CsvGrid.ColumnDisplayIndexChanged += (sender, e) => OnCsvColumnMoved();
        CsvGrid.RowHeadersVisible = false;
        CsvGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        CsvGrid.MultiSelect = false;
        CsvGrid.BorderStyle = BorderStyle.None;
        CsvGrid.BackgroundColor = Theme.White;
        // Lines between the rows AND the columns (Daan, 2026-11-21: "add column separation, so it's a bit more visible"),
        // a little darker than the light lines of the mockup; the column titles get the same lines.
        CsvGrid.GridColor = Theme.TableLine;
        CsvGrid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        CsvGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        // NOT AutoSizeColumnsMode.AllCells: that measures every cell of every row when the table is shown, about 1 second
        // per 1000 rows, and the window froze while doing it (Daan, 2026-10-26). FitCsvColumns measures one cell per column.
        CsvGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        CsvGrid.FontChanged += (sender, e) => FitCsvColumns(); // the window moved to a screen with another scaling
        CsvGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        // Bigger than the rest of the window, for a touch screen (Daan, 2026-10-25): the font 4 points bigger, and the rows
        // higher than the text (FillCsvGrid). The control's own Font, so WinForms scales it with the screen like the others.
        // Both times RowHeightCSV percent (settings.json, Daan 2026-11-08; 100 = this standard).
        float points = (Font.SizeInPoints + CsvExtraPoints) * _settings.RowHeightCSV / 100f;
        CsvGrid.Font = new Font(Font.FontFamily, points, FontStyle.Regular, GraphicsUnit.Point);

        // The headers in the colours of the theme (otherwise Windows draws them in its own grey): light grey with bold blue
        // titles, the chosen row in the light blue of the Maestro skin. Its text stays dark (navy on that blue is 6:1;
        // the white of the mockup would be only 2:1, too pale to read at the reprint station).
        CsvGrid.EnableHeadersVisualStyles = false;
        CsvGrid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Field;
        CsvGrid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Text;
        CsvGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Field;
        CsvGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.Text;
        CsvGrid.ColumnHeadersDefaultCellStyle.Font = new Font(CsvGrid.Font, FontStyle.Bold);
        CsvGrid.DefaultCellStyle.BackColor = Theme.White;
        CsvGrid.DefaultCellStyle.ForeColor = Theme.Text;
        CsvGrid.DefaultCellStyle.SelectionBackColor = Theme.Selection;
        CsvGrid.DefaultCellStyle.SelectionForeColor = Theme.ButtonNavy;

        CsvGrid.CurrentCellChanged += (sender, e) => OnCsvRowChosen(); // after the current row has changed (SelectionChanged comes too early)
    }

    /// <summary>The height of a row of the csv table: its text plus room above and below, both RowHeightCSV percent.</summary>
    private int CsvRowHeight() =>
        CsvGrid.Font.Height + LogicalToDeviceUnits((int)Math.Round(CsvRowExtraHeight * _settings.RowHeightCSV / 100.0));

    /// <summary>
    /// RowHeightCSV was changed in settings.json while the program runs: the text and the rows of the csv table get the
    /// new size at once. The font is changed in proportion to the one it has now, so the screen scaling stays in it.
    /// </summary>
    private void ResizeCsvTable(int oldPercent)
    {
        float points = CsvGrid.Font.SizeInPoints * _settings.RowHeightCSV / oldPercent;
        CsvGrid.Font = new Font(CsvGrid.Font.FontFamily, points, FontStyle.Regular, GraphicsUnit.Point); // the columns follow (FontChanged)
        CsvGrid.ColumnHeadersDefaultCellStyle.Font = new Font(CsvGrid.Font, FontStyle.Bold);

        int height = CsvRowHeight();
        CsvGrid.RowTemplate.Height = height;
        foreach (DataGridViewRow row in CsvGrid.Rows)
        {
            row.Height = height;
        }

        FitCsvColumns(); // also for the bold titles
    }

    /// <summary>Fills the csv table with the label-csv (or empties it). The columns keep the order of the file.</summary>
    private void FillCsvGrid(LabelCsv? csv)
    {
        _selectingCsvRow = true; // filling selects the first row: that is not a choice of the user
        CsvGrid.Rows.Clear();
        CsvGrid.Columns.Clear();

        // Room above and below the text of every row, easy to tap on a touch screen. Set here (not when the table is
        // built) because the font height in pixels is only right once the window is on its screen.
        CsvGrid.RowTemplate.Height = CsvRowHeight();

        if (csv != null)
        {
            _fillingCsvGrid = true; // the columns move while they are added and put in order: that is not the operator
            CsvGrid.Columns.Add("number", Strings.CsvNumberColumn);
            foreach (string column in csv.Columns)
            {
                CsvGrid.Columns.Add("", column);
            }

            foreach (DataGridViewColumn column in CsvGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable; // row n stays label n
            }

            // The label number stays the first column: a frozen column cannot be dragged among the others (and it stays
            // in view when the table scrolls sideways).
            CsvGrid.Columns[0].Frozen = true;
            ApplyCsvColumnOrder();
            _fillingCsvGrid = false;

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
    /// Puts the csv columns in the order the operator dragged them the last time (settings.json: CsvColumnOrder), right
    /// after the number column. Columns that are not in that list keep their file order after the ones that are.
    /// </summary>
    private void ApplyCsvColumnOrder()
    {
        int next = 1; // place 0 is the number column
        foreach (string name in _settings.CsvColumnOrder)
        {
            DataGridViewColumn? column = CsvGrid.Columns.Cast<DataGridViewColumn>().Skip(1)
                .FirstOrDefault(c => string.Equals(c.HeaderText, name, StringComparison.OrdinalIgnoreCase));
            if (column != null && column.DisplayIndex >= next) // ">= next": a name twice in the list is placed once
            {
                column.DisplayIndex = next++;
            }
        }
    }

    /// <summary>
    /// A column of the csv table was dragged to another place. One drop moves several columns (each one raises the event),
    /// so the order is saved once, right after the drop.
    /// </summary>
    private void OnCsvColumnMoved()
    {
        if (_fillingCsvGrid || _csvColumnOrderSavePending)
        {
            return;
        }

        if (!IsHandleCreated)
        {
            SaveCsvColumnOrder();
            return;
        }

        _csvColumnOrderSavePending = true;
        BeginInvoke(SaveCsvColumnOrder);
    }

    /// <summary>
    /// Keeps the order of the csv table in settings.json: the columns as they are shown now, then the names of the saved
    /// order that this csv does not have (so a csv with other columns does not lose them).
    /// </summary>
    private void SaveCsvColumnOrder()
    {
        _csvColumnOrderSavePending = false;
        List<string> shown = CsvGrid.Columns.Cast<DataGridViewColumn>()
            .Where(column => column.Index != 0) // not the number column
            .OrderBy(column => column.DisplayIndex)
            .Select(column => column.HeaderText)
            .ToList();
        if (shown.Count == 0)
        {
            return;
        }

        var order = new List<string>(shown);
        order.AddRange(_settings.CsvColumnOrder.Where(name => !shown.Contains(name, StringComparer.OrdinalIgnoreCase)));
        _settings.CsvColumnOrder = order;
        TrySaveSettings();
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

    /// <summary>
    /// Chooses the template text (false) or the csv table (true) for the left column. While the board is shown it stays
    /// on top: the choice is used when the board is closed (opening another csv does not close the board).
    /// </summary>
    internal void ShowCsvView(bool csv)
    {
        _showingCsv = csv && _csv != null;
        ShowLeftColumn();
    }

    /// <summary>
    /// The "View board" button (Daan, 2026-11-08): true = the left column shows the board, the sheet with all label
    /// positions (<see cref="SheetBoard"/>); false = closes it. Only when the shown label knows its sheet.
    /// The switch is between the csv table and the board (Daan, 2026-11-08): closing the board shows the csv table when a
    /// label-csv is open (also when the template text was shown before), otherwise the text.
    /// </summary>
    internal void ShowBoard(bool board)
    {
        bool closing = _showingBoard && !board;
        _showingBoard = board && Preview.ShownLabelHasSheet;
        if (closing && _csv != null)
        {
            _showingCsv = true;
        }

        ShowLeftColumn();
    }

    /// <summary>Shows what was chosen in the left column, and marks the tab and the round button that chose it.</summary>
    private void ShowLeftColumn()
    {
        Board.Visible = _showingBoard;
        Editor.Parent!.Visible = !_showingBoard && !_showingCsv;
        CsvGrid.Parent!.Visible = !_showingBoard && _showingCsv;

        // Exactly one of the three is chosen: the template text (its tab is white with a line below it), the csv table or the
        // board (their round button in the bottom bar gets the dark ring and is 10% bigger).
        XmlViewButton.Selected = !_showingBoard && !_showingCsv;
        CsvViewButton.Selected = !_showingBoard && _showingCsv;
        BoardButton.Selected = _showingBoard;
        Preview.ShowSheetMap = !_showingBoard; // the board shows the sheet big: the small map goes, the label gets its room
        UpdateEditorCaption();
        if (_showingCsv && !_showingBoard)
        {
            SelectCsvRowOfShownLabel(); // the row of the label in the preview
        }
    }

    private string LeftColumnCaption() =>
        _showingBoard || _showingCsv ? Strings.CsvCaption : Strings.EditorCaption;

    /// <summary>
    /// The title of the left panel (Daan, 2026-11-21). For the csv table AND the board: "Données :", the file name in bold
    /// (the csv; on the board of a label-xml that xml) and " | 13 étiquette(s)" — this replaced the status line below the
    /// files and the board's own title. For the template text just "Texte du modèle".
    /// </summary>
    private void UpdateEditorCaption()
    {
        EditorCaption.Text = LeftColumnCaption();
        string? file = _csv?.FilePath ?? (_showingBoard ? _currentFile : null);
        bool withFile = (_showingBoard || _showingCsv) && file != null;
        EditorCaption.Emphasis = withFile ? Path.GetFileName(file!) : "";
        EditorCaption.Suffix = withFile ? Strings.LabelCount(Preview.Count) : "";
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
        PreviewCaption.Font = Theme.CreateFont(Font, Font.SizeInPoints + 2);
        PreviewCaption.ForeColor = Theme.Text;
        MakeOneLineTitle(PreviewCaption);
        PreviewCaption.Margin = new Padding(PanelPadding, 0, 8, 0);

        // The title takes the free width (one line, "…" when the column is narrow); the label size what it needs. As
        // high as the tabs of the left panel, so both panels start their content at the same height.
        PreviewHeader.ColumnCount = 2;
        PreviewHeader.RowCount = 1;
        PreviewHeader.AutoSize = true;
        PreviewHeader.Dock = DockStyle.Fill;
        PreviewHeader.Margin = new Padding(0);
        PreviewHeader.MinimumSize = new Size(0, HeaderHeight);
        PreviewHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        PreviewHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        PreviewHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        PreviewHeader.Controls.Add(PreviewCaption, 0, 0);
        PreviewHeader.Controls.Add(BuildLabelSizePanel(), 1, 0);
        return PreviewHeader;
    }

    /// <summary>
    /// The bar at the bottom of the window (Daan's mockup, like Maestro's): on the blue band the round csv and board buttons
    /// at the left and previous / "2 / 3" / next at its round right end; on the grey right of it the print button. The
    /// buttons are placed by <see cref="BottomBar"/> itself. All of them only when they can be used (UpdateNavigation).
    /// </summary>
    private Control BuildBottomBar()
    {
        // The csv table (only with a label-csv) and the board (only when the labels know their sheet), with the signs of
        // the mockup (Daan, 2026-11-21: "use the icons from the Claude mockup"): "CSV" in bold and a drawn board. The one
        // that is chosen is 10% bigger (GrowWhenSelected).
        CsvViewButton.Text = Strings.CsvSign;
        CsvViewButton.Visible = false;
        CsvViewButton.Click += (sender, e) => { _showingBoard = false; ShowCsvView(true); };

        BoardButton.Glyph = DiscGlyph.Board;
        BoardButton.IconHeight = 30;
        BoardButton.Visible = false;
        BoardButton.Click += (sender, e) => ShowBoard(!_showingBoard);
        foreach (DiscButton disc in new[] { CsvViewButton, BoardButton })
        {
            disc.Size = GrowingDiscSize;
            disc.GrowWhenSelected = true;
        }

        PreviousButton.Glyph = DiscGlyph.Previous;
        NextButton.Glyph = DiscGlyph.Next;
        PreviousButton.IconHeight = NextButton.IconHeight = 24;
        PreviousButton.Click += (sender, e) => ShowLabel(Preview.Index - 1);
        NextButton.Click += (sender, e) => ShowLabel(Preview.Index + 1);
        PreviousButton.Size = NextButton.Size = DiscSize;

        // "2 / 3": white and bold on the blue band (5:1), twice the window's font: easy to read at the reprint station.
        LabelCounter.AutoSize = false;
        LabelCounter.TextAlign = ContentAlignment.MiddleCenter;
        LabelCounter.BackColor = Color.Transparent;
        LabelCounter.ForeColor = Theme.White;
        LabelCounter.Font = Theme.CreateFont(Font, Font.SizeInPoints * CounterFontFactor, FontStyle.Bold);
        LabelCounter.Size = new Size(96, 36); // until FitButtonWidths knows the font
        LabelCounter.FontChanged += (sender, e) => FitButtonWidthsLater();

        // The print button: a pill with the white printer of the mockup in a navy circle, and the text "Print".
        PrintButton.Size = PrintButtonSize;
        PrintButton.Visible = false; // as soon as there is a label to print
        PrintButton.Click += (sender, e) => PrintLabels();
        PrintButton.FontChanged += (sender, e) => FitButtonWidthsLater();

        BottomBar.Dock = DockStyle.Bottom;
        BottomBar.Height = BottomBar.BarHeight;
        BottomBar.Margin = new Padding(0);
        return BottomBar;
    }

    /// <summary>
    /// The width of the widest counter of the open file ("88 / 88" for 10 – 99 labels): the arrows do not move while going
    /// through the labels, only when a file with more or fewer digits is opened.
    /// </summary>
    private int CounterWidthNeeded()
    {
        string digits = new('8', Math.Max(1, Preview.Count).ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
        return TextRenderer.MeasureText(digits + " / " + digits, LabelCounter.Font).Width + LogicalToDeviceUnits(8);
    }

    /// <summary>
    /// The ROGIERS logo in the top bar (Daan's mockup; it was at the bottom left in 1.x): opens rogiers.be. (The hidden
    /// Shift+click to settings.json of 1.1.5 – 2.0.0 is gone since the Magic parameters, 2.0.1, Daan.)
    /// </summary>
    private Control BuildLogo()
    {
        LogoPicture.Image = AppIcons.CompanyLogo();
        LogoPicture.SizeMode = PictureBoxSizeMode.Zoom; // keeps the proportions of the logo
        LogoPicture.Size = LogoSize;
        LogoPicture.BackColor = Theme.TopBar;
        LogoPicture.Anchor = AnchorStyles.Left; // in the middle of the bar's height
        LogoPicture.Margin = new Padding(16, 0, 0, 0);
        LogoPicture.Cursor = Cursors.Hand;
        _toolTip.SetToolTip(LogoPicture, Strings.CompanyName);
        LogoPicture.Click += (sender, e) => OpenWebsite(Strings.CompanyUrl);
        return LogoPicture;
    }

    // ------------------------------------------------------------------ the Magic parameters

    /// <summary>The popup while it is open, else null (the tests look at it).</summary>
    internal MagicParametersPopup? MagicParameters => _dim?.Popup;

    // The parts of the window that the dim layer covers and that are switched off meanwhile (taken when it opens).
    private Control[] _coveredParts = Array.Empty<Control>();

    /// <summary>
    /// The cog: opens the Magic parameters over the window (MagicParameters.cs). First a picture is taken of the window as
    /// it is, for the dim layer; then the window's own controls are switched off, so neither the mouse nor the Tab key can
    /// reach them, and Enter / Esc belong to the popup (Enter = Save, Esc = Cancel).
    /// </summary>
    internal void OpenMagicParameters()
    {
        if (_dim != null)
        {
            return;
        }

        string settingsFile = SettingsFilePath ?? AppDataFolder.SettingsFile(AppDataFolder.DefaultRoot);
        var popup = new MagicParametersPopup(_settings, settingsFile, Font, SaveMagicParameters, CloseMagicParameters);
        _coveredParts = Controls.Cast<Control>().ToArray(); // the top bar, the panels, the bottom bar
        _dim = new DimOverlay(popup, TakePicture)
        {
            Bounds = ClientRectangle,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
        };
        _dim.RefreshBackdrop(); // the picture of the window, before anything is switched off

        foreach (Control part in _coveredParts)
        {
            part.Enabled = false;
        }

        AcceptButton = popup.SavePill;
        CancelButton = popup.CancelPill;

        Controls.Add(_dim);
        _dim.BringToFront();
        popup.SheetRotationBox.Select();
    }

    /// <summary>Closes the popup (Cancel, Esc, or after Save) and gives the window back.</summary>
    internal void CloseMagicParameters()
    {
        if (_dim == null)
        {
            return;
        }

        DimOverlay overlay = _dim;
        _dim = null;
        foreach (Control part in _coveredParts)
        {
            part.Enabled = true;
        }

        _coveredParts = Array.Empty<Control>();
        AcceptButton = null; // the window has no Enter / Esc button of its own (Enter in a path box opens the file)
        CancelButton = null;
        Controls.Remove(overlay);

        // Disposed a moment later: this can be running inside the click of one of its own pills.
        if (IsHandleCreated)
        {
            BeginInvoke(overlay.Dispose);
        }
        else
        {
            overlay.Dispose();
        }
    }

    /// <summary>
    /// Save in the popup: only its values are changed in settings.json (the file is read again first, the rest of it stays
    /// as it is) and used at once (the sheet map, the preview, the table, the print button). False when that failed: the
    /// user is told and the popup stays open.
    /// </summary>
    private bool SaveMagicParameters(AppSettings values)
    {
        string? path = SettingsFilePath;
        if (path != null) // null in the tests: no data folder, only used
        {
            try
            {
                SettingsStore.SaveHandEditedSettings(path, _settings, values);
                _settingsFileTime = File.GetLastWriteTimeUtc(path); // our own change: OnActivated has nothing to read again
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                MessageBox.Show(this, Strings.SettingsSaveFailed(error.Message), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        UseHandEditedSettings(values);
        return true;
    }

    /// <summary>A picture of the window's inside as it is now (the top bar, the panels, the bottom bar), for the dim layer.</summary>
    private Bitmap TakePicture()
    {
        var picture = new Bitmap(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
        using (Graphics g = Graphics.FromImage(picture))
        {
            g.Clear(BackColor); // the resize edge around the panels
        }

        foreach (Control part in _coveredParts)
        {
            if (part.Visible && part.Width > 0 && part.Height > 0)
            {
                part.DrawToBitmap(picture, part.Bounds);
            }
        }

        return picture;
    }

    /// <summary>C:\DssLabelTemplate\settings.json; null when the window has no data folder (the tests).</summary>
    private string? SettingsFilePath => _dataFolder == null ? null : AppDataFolder.SettingsFile(_dataFolder);

    /// <summary>
    /// The window became active again (for example after editing settings.json in Notepad): when the file changed, the
    /// settings that are only in that file are read again and used at once. The settings of the window itself stay as
    /// they are (the window wins for those, see <see cref="SettingsStore.SaveKeepingHandEdits"/>).
    /// </summary>
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        string? path = SettingsFilePath;
        if (path == null)
        {
            return;
        }

        DateTime time;
        try
        {
            time = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            return; // cannot look now: act as if nothing changed
        }

        if (time == _settingsFileTime)
        {
            return; // nothing changed since the last look
        }

        SettingsReadResult read = SettingsStore.Read(path);
        if (!read.FileExisted || read.ErrorIsTemporary)
        {
            return; // gone (the settings in use stay), or busy being written: look again the next time
        }

        _settingsFileTime = time;

        if (read.Error != "")
        {
            // Once per change of the file; after the activation itself, so the message box does not disturb it.
            BeginInvoke(() => MessageBox.Show(this, Strings.SettingsFileHasMistake(path, read.Error), Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning));
            return;
        }

        UseHandEditedSettings(read.Settings);
    }

    /// <summary>
    /// Uses the settings that are only in settings.json from <paramref name="file"/>: the sheet map, the label rotation in
    /// the preview and the print button. Returns false when they are the same as the ones in use (nothing to do).
    /// </summary>
    internal bool UseHandEditedSettings(AppSettings file)
    {
        if (_settings.HasSameHandEditedSettings(file))
        {
            return false;
        }

        int oldCsvRowSize = _settings.RowHeightCSV;
        _settings.CopyHandEditedSettingsFrom(file);
        ApplyHandEditedSettings();
        if (_settings.RowHeightCSV != oldCsvRowSize)
        {
            ResizeCsvTable(oldCsvRowSize);
        }
        ApplyTexts(); // the tooltip of the print button depends on HideWindowsPrintDialog; refreshes the preview too
        return true;
    }

    /// <summary>Gives the preview the settings that are only in settings.json.</summary>
    private void ApplyHandEditedSettings()
    {
        Preview.SheetRotation = _settings.SheetRotationInViewer;
        Preview.ReferenceCorner = SheetCorners.Parse(_settings.SheetReferenceCorner) ?? SheetCorner.BottomLeft;
        Preview.ShowRotation = _settings.ShowLabelRotation;
        Preview.RotationClockwise = _settings.LabelRotationIsClockwise;
    }

    // ------------------------------------------------------------------ language

    /// <summary>Puts every text in the selected language. Called at the start and after a language change.</summary>
    internal void ApplyTexts()
    {
        UpdateTitle();
        VersionLabel.Text = Strings.AppVersion(AppInfo.Version);
        _toolTip.SetToolTip(UserGuideButton, Strings.UserGuideTooltip);
        UserGuideButton.AccessibleName = Strings.UserGuideTooltip; // a sign, no text: for a screen reader
        FilesCaption.Text = Strings.FilesCaption;
        string howTo = WrapForToolTip(Strings.FileDescription, HowToLineLength);
        HowToToolTip.SetToolTip(FilesHowToPicture, howTo);
        _toolTip.SetToolTip(XmlFoldButton, XmlFoldButton.Folded ? Strings.UnfoldFileLine : Strings.FoldFileLine);
        _toolTip.SetToolTip(CsvFoldButton, CsvFoldButton.Folded ? Strings.UnfoldFileLine : Strings.FoldFileLine);
        XmlFileCaption.Text = Strings.XmlFileCaption;
        CsvFileCaption.Text = Strings.CsvFileCaption;
        XmlBrowseButton.Text = Strings.Browse;
        CsvBrowseButton.Text = Strings.Browse;
        UpdateEditorCaption();
        XmlViewButton.Text = Strings.XmlViewButton;
        _toolTip.SetToolTip(CsvViewButton, Strings.CsvViewTooltip);
        CsvViewButton.AccessibleName = Strings.CsvViewTooltip; // a picture, no text: for a screen reader
        PreviewCaption.Text = Strings.PreviewCaption;
        LabelSizeCaption.Text = Strings.LabelSizeCaption;
        PositionTile.Caption = Strings.PositionTile;
        SheetTile.Caption = Strings.SheetTile;
        _toolTip.SetToolTip(LabelLengthBox, Strings.LabelLengthTooltip);
        _toolTip.SetToolTip(LabelWidthBox, Strings.LabelWidthTooltip);
        _toolTip.SetToolTip(PreviousButton, Strings.PreviousLabel);
        PreviousButton.AccessibleName = Strings.PreviousLabel;
        _toolTip.SetToolTip(NextButton, Strings.NextLabel);
        NextButton.AccessibleName = Strings.NextLabel;
        PrintButton.Text = Strings.PrintButtonText;
        string printTooltip = _settings.HideWindowsPrintDialog ? Strings.PrintTooltipWithoutDialog : Strings.PrintTooltip;
        _toolTip.SetToolTip(PrintButton, printTooltip);
        PrintButton.AccessibleName = printTooltip; // says whether a dialog follows: for a screen reader
        _toolTip.SetToolTip(BoardButton, Strings.BoardTooltip);
        BoardButton.AccessibleName = Strings.BoardTooltip;
        _toolTip.SetToolTip(ColumnDivider, Strings.ColumnDividerTooltip);
        SaveButton.Text = Strings.SaveTemplate;
        FitButtonWidths(); // the texts have another length in another language
        RefreshPreview(newFile: false); // the preview's messages and the panel titles are in the language too
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
    /// With HideWindowsPrintDialog (settings.json) there is no dialog: the shown label is printed at once.
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

            // HideWindowsPrintDialog (settings.json): no dialog. The label in the preview is printed at once, on the printer
            // set up above (PrinterName, or else Windows' default printer) with its driver's own settings.
            if (!_settings.HideWindowsPrintDialog)
            {
                // UseEXDialog: the dialog with "current page" (the newer Windows dialog has no page choice).
                using var dialog = new PrintDialog { Document = document, UseEXDialog = true, AllowCurrentPage = true, AllowSomePages = true };
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _settings.PrinterName = document.PrinterSettings.PrinterName;
                TrySaveSettings();
            }

            PrinterSettings chosen = document.PrinterSettings;
            List<int> numbers = LabelPrinter.LabelsToPrint(chosen.PrintRange, chosen.FromPage, chosen.ToPage, Preview.Index, Preview.Count);
            if (numbers.Count == 0)
            {
                return;
            }

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

            // PrintLabelWithRotation (settings.json): only a square label can be printed turned; another shape: ask.
            SizeF? turnedLabelSize = null;
            if (_settings.PrintLabelWithRotation)
            {
                if (LabelPrinter.IsSquare(Preview.LabelSize))
                {
                    turnedLabelSize = Preview.LabelSize;
                }
                else
                {
                    string question = Strings.RotationNeedsSquareLabelQuestion(LabelXml.Format(Preview.LabelSize.Width), LabelXml.Format(Preview.LabelSize.Height));
                    if (MessageBox.Show(this, question, Strings.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        return;
                    }
                }
            }

            string? folder = _currentFile == null ? null : Path.GetDirectoryName(_currentFile);
            LabelPrinter.Print(document, numbers.Select(number => Preview.Labels!.Labels[number]).ToList(), folder, turnedLabelSize,
                _settings.SheetRotationInViewer, _settings.LabelRotationIsClockwise);
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
        UpdateNavigation(); // also the title of the left panel (the number of labels)

        // ShowBoardByDefault (settings.json, Daan 2026-11-08): every newly opened file (also the ones opened again at the
        // start) is shown on the board, when its labels know their sheet. Closing the board stays possible.
        if (newFile && _settings.ShowBoardByDefault && Preview.ShownLabelHasSheet)
        {
            ShowBoard(true);
        }
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

        // "Template (xml)" and the csv button only with a label-csv that is used; the csv table follows the label of the
        // preview.
        bool csv = _csv != null && !_isLabelXml;
        XmlViewButton.Visible = csv;
        CsvViewButton.Visible = csv;
        if (!csv && _showingCsv)
        {
            ShowCsvView(false);
        }

        // The board button: only when the labels know their sheet; a file without one closes the board.
        bool board = Preview.ShownLabelHasSheet;
        BoardButton.Visible = board;
        if (!board && _showingBoard)
        {
            ShowBoard(false);
        }

        // The counter of another file may need more room; the bar places its buttons again (some appeared or went).
        int counterWidth = CounterWidthNeeded();
        if (LabelCounter.Width != counterWidth)
        {
            LabelCounter.Width = counterWidth;
        }

        BottomBar.PerformLayout();
        UpdateTiles();
        UpdateEditorCaption(); // the number of labels in the title
        SelectCsvRowOfShownLabel();
    }

    /// <summary>
    /// The two tiles below the preview: where the shown label sits (X, Y, its ID and rotation) and the size of its sheet.
    /// A dash when there is no label (nothing open, broken xml) or no sheet size. X or Y that is not a number: "?".
    /// </summary>
    private void UpdateTiles()
    {
        LabelItem? label = Preview.Count > 0 && Preview.Labels!.Error == null ? Preview.Labels.Labels[Preview.Index] : null;
        if (label == null)
        {
            PositionTile.Value = Strings.NoValue;
            PositionTile.Line1 = PositionTile.Line2 = "";
            SheetTile.Value = Strings.NoValue;
            SheetTile.Line1 = SheetTile.Line2 = "";
            return;
        }

        static string Number(double? value) => value is double number ? LabelXml.Format(number) : "?";

        PositionTile.Value = string.Format(System.Globalization.CultureInfo.InvariantCulture, Strings.PositionFormat, Number(label.X), Number(label.Y));
        PositionTile.Line1 = Strings.Millimetres;
        PositionTile.Line2 = Strings.LabelDetails(label.Id, label.Rotation.ToString(System.Globalization.CultureInfo.InvariantCulture));

        bool hasSheet = label.SheetLength > 0 && label.SheetWidth > 0;
        SheetTile.Value = hasSheet
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, Strings.SheetSizeFormat, LabelXml.Format(label.SheetLength!.Value), LabelXml.Format(label.SheetWidth!.Value))
            : Strings.NoValue;
        SheetTile.Line1 = hasSheet ? Strings.Millimetres : "";
        SheetTile.Line2 = "";
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
        // While the Magic parameters are open the window behind them is off: its shortcuts do nothing.
        if (_dim != null && keyData is (Keys.Control | Keys.S) or (Keys.Control | Keys.P) or (Keys.Control | Keys.O) or Keys.F11)
        {
            return true;
        }

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

        if (keyData == Keys.F11)
        {
            ToggleFullScreen(); // like a browser: the whole monitor, without the taskbar
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
            _clockTimer.Dispose();
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }
}
