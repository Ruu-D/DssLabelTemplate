using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DssLabelTemplate;

// The "Magic parameters" (2.0.1, Daan 2026-11-22, the same as TsDssConverter 2.0.1, from his mockup "DssLabelTemplate
// Magic parameters"): the cog right of the "?" in the top bar opens a popup with the settings that were only in
// settings.json (the ones of AppSettings.CopyHandEditedSettingsFrom), because the customer cannot be expected to edit that
// file. The popup changes only those values in settings.json (read, change, write: the file stays THE place of the
// settings). While it is open the window behind it is dimmed: it stays sharp under a see-through navy layer, and every
// click and key on the window is blocked.
//
// It is not a second Form but three controls laid over the window (MainForm.OpenMagicParameters):
//   DimOverlay            covers the whole window, paints the dimmed picture of it and the popup's shadow
//   MagicParametersPopup  the dark-grey card with rounded corners: title, the white panel with the rows in three groups,
//                         the note where it is saved, Cancel and Save
//   RoundedWhitePanel     the white panel with rounded corners inside the card; it scrolls when the rows do not fit
// Being part of the window, it moves, resizes and scales with it, and the corners can be drawn smoothly over the dimmed
// picture. Why a PICTURE of the window and not the window itself under a see-through control: WinForms cannot make a
// control really see-through over its sibling controls; it would only show the window's background colour.

/// <summary>
/// Lies over the whole window while the Magic parameters are open: shows the window, dimmed by a see-through navy layer
/// (Daan's choice, the same as TsDssConverter), with the popup in the middle. It catches every click, so the window behind
/// (also its top bar with the window buttons: this window has no Windows title bar) cannot be used meanwhile.
/// </summary>
internal class DimOverlay : Control
{
    /// <summary>The popup is at most this part of the window's height (Daan): the rows scroll inside it.</summary>
    internal const double MaximumHeightShare = 2.0 / 3;

    private const int PreferredWidth = 820; // at 100%: the mockup's width (eight rows, so wider than TsDssConverter's)
    private const int EdgeMargin = 16;      // at 100%: the least room between the popup and the edge of the window
    private const int ShadowSize = 14;      // at 100%: how far the popup's shadow reaches

    /// <summary>The popup's share of the window's width when the window is big: √(1/3), like TsDssConverter.</summary>
    private static readonly double SideShare = Math.Sqrt(1.0 / 3);

    private readonly Func<Bitmap> _takePicture;
    private Bitmap? _picture;  // the window as it was when the picture was taken
    private Bitmap? _backdrop; // what the overlay shows: the picture at the current size, dimmed, with the popup's shadow

    internal MagicParametersPopup Popup { get; }

    /// <param name="popup">The popup to show in the middle.</param>
    /// <param name="takePicture">Takes a picture of the window (without this overlay), as big as the window's inside.</param>
    public DimOverlay(MagicParametersPopup popup, Func<Bitmap> takePicture)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
        TabStop = false;
        Popup = popup;
        _takePicture = takePicture;
        Controls.Add(popup);
    }

    /// <summary>
    /// Takes a new picture of the window. Called when the overlay opens and when the window changed size (while the edge
    /// is being dragged the old picture is stretched for that moment).
    /// </summary>
    internal void RefreshBackdrop()
    {
        Bitmap picture = _takePicture();
        _picture?.Dispose();
        _picture = picture;
        ComposeBackdrop();
    }

    /// <summary>Draws a picture stretched over <paramref name="target"/> without a dark rim at the edges.</summary>
    private static void DrawStretched(Graphics g, Image image, Rectangle target)
    {
        // Without TileFlipXY GDI+ mixes the edge pixels with transparent black while stretching: a dark rim.
        using var attributes = new ImageAttributes();
        attributes.SetWrapMode(WrapMode.TileFlipXY);
        g.DrawImage(image, target, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
    }

    /// <summary>
    /// The popup in the middle: 820 wide at 100% (or √(1/3) of a wider window), as high as its rows need but at most 2/3
    /// of the window's height (Daan; then the rows scroll), never outside the window.
    /// </summary>
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        int margin = LogicalToDeviceUnits(EdgeMargin);
        int width = Math.Max(LogicalToDeviceUnits(PreferredWidth), (int)Math.Round(Width * SideShare));
        width = Math.Max(1, Math.Min(width, Width - 2 * margin));
        int highest = Math.Min((int)(Height * MaximumHeightShare), Height - 2 * margin);
        int height = Math.Max(1, Math.Min(Popup.HeightFor(width), highest));

        var bounds = new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
        if (Popup.Bounds != bounds)
        {
            Popup.Bounds = bounds;
        }

        ComposeBackdrop();
    }

    /// <summary>The window at the overlay's size, under the navy layer, with a soft shadow around the popup.</summary>
    private void ComposeBackdrop()
    {
        if (_picture == null || Width <= 0 || Height <= 0)
        {
            return;
        }

        var backdrop = new Bitmap(Width, Height);
        using (Graphics g = Graphics.FromImage(backdrop))
        {
            if (_picture.Size == backdrop.Size)
            {
                g.DrawImageUnscaled(_picture, 0, 0); // the usual case: sharp, pixel for pixel
            }
            else
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                DrawStretched(g, _picture, new Rectangle(0, 0, Width, Height)); // only while the window is being resized
            }

            using (var layer = new SolidBrush(Theme.DimLayer))
            {
                g.FillRectangle(layer, 0, 0, Width, Height);
            }

            // The shadow: a few rounded rectangles, each a little bigger and lower, in very see-through black. Together
            // they are darkest close to the popup and fade out.
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float radius = LogicalToDeviceUnits(MagicParametersPopup.CornerRadius);
            int reach = LogicalToDeviceUnits(ShadowSize);
            const int layers = 7;
            using var brush = new SolidBrush(Color.FromArgb(Theme.PopupShadow.A / layers, Theme.PopupShadow));
            for (int i = 1; i <= layers; i++)
            {
                float grow = reach * i / (float)layers;
                var shape = RectangleF.Inflate(Popup.Bounds, grow, grow);
                shape.Offset(0, grow / 2);
                using GraphicsPath path = FluentDrawing.RoundedRectangle(shape, radius + grow);
                g.FillPath(brush, path);
            }
        }

        _backdrop?.Dispose();
        _backdrop = backdrop;
        Invalidate(invalidateChildren: true);
    }

    /// <summary>The backdrop inside the popup (its rounded corners show it): the same picture, moved to its place.</summary>
    internal void PaintBackdropFor(Control child, Graphics g)
    {
        if (_backdrop == null)
        {
            g.Clear(Theme.Frame);
            return;
        }

        g.DrawImageUnscaled(_backdrop, -child.Left, -child.Top);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_backdrop == null)
        {
            e.Graphics.Clear(Theme.Frame);
            return;
        }

        e.Graphics.DrawImageUnscaled(_backdrop, 0, 0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _picture?.Dispose();
            _backdrop?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>One line of the popup: one setting of settings.json.</summary>
internal class ParameterRow
{
    /// <summary>The name of the setting in settings.json (shown in a small grey tag, for whoever maintains the app).</summary>
    public string Key { get; }
    public Label Caption { get; } = new();
    public Label KeyTag { get; } = new();
    public Label Hint { get; } = new();

    /// <summary>The control in the right column: a field, a drop-down or a check box.</summary>
    public Control Field { get; }

    /// <summary>The width of <see cref="Field"/> at 100%; 0 = the whole right column.</summary>
    public int FieldWidth { get; }

    /// <summary>A word after the field ("%"), or null.</summary>
    public Label? After { get; }

    /// <summary>The title of a group ("WEERGAVE") above this row, or null when the row is not the first of its group.</summary>
    public Label? GroupTitle { get; }

    public ParameterRow(string key, Control field, int fieldWidth, Label? after, Label? groupTitle)
    {
        Key = key;
        Field = field;
        FieldWidth = fieldWidth;
        After = after;
        GroupTitle = groupTitle;
    }
}

/// <summary>
/// The popup of the Magic parameters (Daan's mockup, 2026-11-22): a dark-grey card with rounded corners (the grey of the
/// bottom bar), the title "Magic parameters" with a cog, a white panel with one row per setting in three groups (name and
/// explanation at the left, the field at the right; a true/false setting is a check box in that same right column), and at
/// the bottom where it is saved, Cancel and Save.
/// </summary>
internal class MagicParametersPopup : Control
{
    // Sizes at 100%.
    internal const int CornerRadius = 20;
    private const int SideMargin = 26;    // the grey around the white panel, left and right
    private const int TitleHeight = 60;
    private const int FooterHeight = 84;
    private const int CogSize = 20;
    private const int TextPadding = 20;   // inside the white panel, left and right
    private const int RowPadding = 11;    // above and below the text of a row
    private const int GroupTop = 12;      // above a group title
    private const int ColumnGap = 24;     // between the text and the field
    private const int FieldColumn = 230;  // the right column
    private const int FieldHeight = 32;
    private const int SmallField = 110;   // the percentage
    private const int CheckSize = 22;
    private const int PillHeight = 40;
    private const int PillGap = 12;

    private readonly AppSettings _inUse;
    private readonly Func<AppSettings, bool> _save;
    private readonly Action _close;
    private readonly Font _captionFont;
    private readonly Font _titleFont;
    private readonly Font _keyFont;
    private readonly Font _groupFont;
    private bool _arranging;

    internal readonly Label TitleLabel = new();
    internal readonly RoundedWhitePanel WhitePanel = new();
    internal readonly ComboBox SheetRotationBox = new();
    internal readonly CheckBox ShowLabelRotationBox = new FluentCheckBox();
    internal readonly CheckBox ShowBoardBox = new FluentCheckBox();
    internal readonly NumericUpDown CsvSizeBox = new();
    internal readonly CheckBox PrintRotationBox = new FluentCheckBox();
    internal readonly CheckBox HidePrintDialogBox = new FluentCheckBox();
    internal readonly ComboBox LabelDirectionBox = new();
    internal readonly ComboBox ReferenceCornerBox = new();
    internal readonly Label SavedInLabel = new();
    internal readonly Label SettingsPathLabel = new();
    internal readonly PillButton SavePill = new() { Sign = PillSign.Tick };
    internal readonly PillButton CancelPill = new() { Sign = PillSign.None };
    internal readonly List<ParameterRow> Rows = new();

    /// <summary>One item of a drop-down: the value written in settings.json and the text shown for it.</summary>
    internal class Choice
    {
        public object Value { get; }
        public string Text { get; }

        public Choice(object value, string text)
        {
            Value = value;
            Text = text;
        }

        public override string ToString() => Text; // what the ComboBox shows
    }

    /// <param name="inUse">The settings in use: the popup shows their values.</param>
    /// <param name="settingsFile">The path of settings.json, shown at the bottom left.</param>
    /// <param name="font">The window's font (the popup is made after the window, so it is passed on explicitly).</param>
    /// <param name="save">Saves the values; false when that failed (the user was told, the popup stays open).</param>
    /// <param name="close">Closes the popup (after Save, or Cancel).</param>
    public MagicParametersPopup(AppSettings inUse, string settingsFile, Font font, Func<AppSettings, bool> save, Action close)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _inUse = inUse;
        _save = save;
        _close = close;
        Font = font;
        BackColor = Theme.WorkArea;
        ForeColor = Theme.Text;
        _captionFont = Theme.CreateFont(font, font.SizeInPoints + 1);
        _titleFont = Theme.CreateFont(font, font.SizeInPoints + 1); // the size of the tile titles (Daan)
        _keyFont = new Font("Consolas", Math.Max(6f, font.SizeInPoints - 1));
        _groupFont = Theme.CreateFont(font, Math.Max(6f, font.SizeInPoints - 0.5f), FontStyle.Bold);

        TitleLabel.Text = Strings.MagicParametersTitle;
        TitleLabel.Font = _titleFont;
        TitleLabel.ForeColor = Theme.ButtonNavy;
        TitleLabel.BackColor = Theme.WorkArea;
        TitleLabel.AutoSize = true;
        Controls.Add(TitleLabel);

        WhitePanel.AutoScroll = true; // the rows scroll when the popup is at its highest (2/3 of the window)
        WhitePanel.Layout += (sender, e) => ArrangeRows(WhitePanel.ClientSize.Width, place: true);
        Controls.Add(WhitePanel);

        BuildRows(inUse);

        SavedInLabel.Text = Strings.SavedIn;
        SettingsPathLabel.Text = settingsFile;
        SettingsPathLabel.Font = _keyFont;
        foreach (Label note in new[] { SavedInLabel, SettingsPathLabel })
        {
            note.ForeColor = Theme.ButtonNavy; // navy on the dark grey: 7:1
            note.BackColor = Theme.WorkArea;
            note.AutoEllipsis = true;
            Controls.Add(note);
        }

        CancelPill.Text = Strings.Cancel;
        CancelPill.Click += (sender, e) => _close();
        SavePill.Text = Strings.Save;
        SavePill.Click += (sender, e) => SaveAndClose();
        Controls.Add(CancelPill);
        Controls.Add(SavePill);

        // The Tab key: the fields top to bottom, then Cancel and Save (the window's own controls are switched off).
        int tab = 0;
        foreach (ParameterRow row in Rows)
        {
            row.Field.TabStop = true;
            row.Field.TabIndex = tab++;
        }

        CancelPill.TabStop = true;
        CancelPill.TabIndex = tab++;
        SavePill.TabStop = true;
        SavePill.TabIndex = tab;
    }

    /// <summary>The eight rows in three groups, in the order of the mockup, filled with the values in use.</summary>
    private void BuildRows(AppSettings inUse)
    {
        // ---- Display
        AddChoices(SheetRotationBox, new[]
        {
            new Choice(0, Strings.RotationNone),
            new Choice(90, Strings.RotationLeft90),
            new Choice(180, Strings.Rotation180),
            new Choice(-90, Strings.RotationRight90),
        }, SameQuarterTurn(inUse.SheetRotationInViewer));
        AddRow(nameof(AppSettings.SheetRotationInViewer), Strings.SheetRotationCaption, Strings.SheetRotationHint, SheetRotationBox, 0, group: Strings.GroupDisplay);

        AddCheckRow(nameof(AppSettings.ShowLabelRotation), Strings.ShowLabelRotationCaption, Strings.ShowLabelRotationHint, ShowLabelRotationBox, inUse.ShowLabelRotation);
        AddCheckRow(nameof(AppSettings.ShowBoardByDefault), Strings.ShowBoardCaption, Strings.ShowBoardHint, ShowBoardBox, inUse.ShowBoardByDefault);

        CsvSizeBox.Minimum = AppSettings.MinimumCsvRowSize;
        CsvSizeBox.Maximum = AppSettings.MaximumCsvRowSize;
        CsvSizeBox.Increment = 10;
        CsvSizeBox.Value = Math.Clamp(inUse.RowHeightCSV, AppSettings.MinimumCsvRowSize, AppSettings.MaximumCsvRowSize);
        CsvSizeBox.ForeColor = Theme.Text;
        CsvSizeBox.AccessibleName = Strings.CsvSizeCaption;
        var frame = new TextBoxFrame(CsvSizeBox);
        Theme.StyleField(frame, Theme.Field);
        CsvSizeBox.BackColor = Theme.Field;
        var percent = new Label { Text = "%", ForeColor = Theme.Text, BackColor = Theme.White, AutoSize = true };
        AddRow(nameof(AppSettings.RowHeightCSV), Strings.CsvSizeCaption, Strings.CsvSizeHint, frame, SmallField, percent);

        // ---- Printing
        AddCheckRow(nameof(AppSettings.PrintLabelWithRotation), Strings.PrintRotationCaption, Strings.PrintRotationHint, PrintRotationBox, inUse.PrintLabelWithRotation, Strings.GroupPrinting);
        AddCheckRow(nameof(AppSettings.HideWindowsPrintDialog), Strings.HidePrintDialogCaption, Strings.HidePrintDialogHint, HidePrintDialogBox, inUse.HideWindowsPrintDialog);

        // ---- Machine: the same as DSSClient and TsDssConverter
        AddChoices(LabelDirectionBox, new[]
        {
            new Choice(AppSettings.CounterClockwise, Strings.DirectionCounterClockwise),
            new Choice(AppSettings.Clockwise, Strings.DirectionClockwise),
        }, inUse.LabelRotationIsClockwise ? AppSettings.Clockwise : AppSettings.CounterClockwise);
        AddRow(nameof(AppSettings.LabelDirection), Strings.LabelDirectionCaption, Strings.LabelDirectionHint, LabelDirectionBox, 0, group: Strings.GroupMachine);

        AddChoices(ReferenceCornerBox, new[]
        {
            new Choice(SheetCorners.Name(SheetCorner.BottomLeft), Strings.CornerBottomLeft),
            new Choice(SheetCorners.Name(SheetCorner.TopLeft), Strings.CornerTopLeft),
            new Choice(SheetCorners.Name(SheetCorner.BottomRight), Strings.CornerBottomRight),
            new Choice(SheetCorners.Name(SheetCorner.TopRight), Strings.CornerTopRight),
        }, SheetCorners.Name(SheetCorners.Parse(inUse.SheetReferenceCorner) ?? SheetCorner.BottomLeft));
        AddRow(nameof(AppSettings.SheetReferenceCorner), Strings.ReferenceCornerCaption, Strings.ReferenceCornerHint, ReferenceCornerBox, 0);
    }

    /// <summary>
    /// The same quarter turn as one of the four choices: settings.json may hold 270, -180 or -270 (edited by hand), which
    /// turn the sheet the same way as -90, 180 and 90.
    /// </summary>
    internal static int SameQuarterTurn(int degrees)
    {
        int turn = ((degrees % 360) + 360) % 360; // 0, 90, 180 or 270
        return turn == 270 ? -90 : turn;
    }

    /// <summary>A drop-down in the flat grey of the fields, no typing, with these choices and the one in use chosen.</summary>
    private static void AddChoices(ComboBox box, Choice[] choices, object chosen)
    {
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.FlatStyle = FlatStyle.Flat;
        box.BackColor = Theme.Field;
        box.ForeColor = Theme.Text;
        box.Cursor = Cursors.Hand;
        box.Items.AddRange(choices);
        box.SelectedIndex = Math.Max(0, Array.FindIndex(choices, choice => Equals(choice.Value, chosen)));
    }

    /// <summary>A true/false setting: a check box in the right column (Daan); a click on its name ticks it too.</summary>
    private ParameterRow AddCheckRow(string key, string caption, string hint, CheckBox box, bool value, string? group = null)
    {
        box.Checked = value;
        box.Text = "";
        box.BackColor = Theme.White;
        ParameterRow row = AddRow(key, caption, hint, box, CheckSize, group: group);
        row.Caption.Cursor = Cursors.Hand;
        row.Caption.Click += (sender, e) => box.Checked = !box.Checked;
        return row;
    }

    private ParameterRow AddRow(string key, string caption, string hint, Control field, int fieldWidth, Label? after = null, string? group = null)
    {
        Label? groupTitle = null;
        if (group != null)
        {
            groupTitle = new Label { Text = group, Font = _groupFont, ForeColor = Theme.TextSoft, BackColor = Theme.White, AutoSize = false };
            WhitePanel.Controls.Add(groupTitle);
        }

        var row = new ParameterRow(key, field, fieldWidth, after, groupTitle);
        field.AccessibleName = caption;

        row.Caption.Text = caption;
        row.Caption.Font = _captionFont;
        row.Caption.ForeColor = Theme.Text;

        row.KeyTag.Text = key;
        row.KeyTag.Font = _keyFont;
        row.KeyTag.ForeColor = Theme.TextSoft;
        row.KeyTag.BackColor = Theme.Field;
        row.KeyTag.Padding = new Padding(3, 1, 3, 1);

        row.Hint.Text = hint;
        row.Hint.ForeColor = Theme.TextSoft;

        foreach (Label label in new[] { row.Caption, row.KeyTag, row.Hint })
        {
            label.AutoSize = false; // the size is set by ArrangeRows
            if (label != row.KeyTag)
            {
                label.BackColor = Theme.White;
            }

            WhitePanel.Controls.Add(label);
        }

        WhitePanel.Controls.Add(field);
        if (after != null)
        {
            WhitePanel.Controls.Add(after);
        }

        Rows.Add(row);
        return row;
    }

    // ------------------------------------------------------------------ values and saving

    /// <summary>
    /// The settings in use with the values of the popup put in. A sheet rotation that turns the same way as the one in use
    /// keeps the number in use (270 stays 270, it is not rewritten as -90).
    /// </summary>
    internal AppSettings ReadValues()
    {
        AppSettings values = _inUse.Clone();
        int rotation = (int)((Choice)SheetRotationBox.SelectedItem!).Value;
        values.SheetRotationInViewer = rotation == SameQuarterTurn(_inUse.SheetRotationInViewer) ? _inUse.SheetRotationInViewer : rotation;
        values.ShowLabelRotation = ShowLabelRotationBox.Checked;
        values.ShowBoardByDefault = ShowBoardBox.Checked;
        values.RowHeightCSV = (int)CsvSizeBox.Value; // reading Value also takes a number that is still being typed
        values.PrintLabelWithRotation = PrintRotationBox.Checked;
        values.HideWindowsPrintDialog = HidePrintDialogBox.Checked;
        values.LabelDirection = (string)((Choice)LabelDirectionBox.SelectedItem!).Value;
        values.SheetReferenceCorner = (string)((Choice)ReferenceCornerBox.SelectedItem!).Value;
        return values;
    }

    /// <summary>The Save pill (and Enter): saves and closes; when saving failed the popup stays open.</summary>
    internal void SaveAndClose()
    {
        if (_save(ReadValues()))
        {
            _close();
        }
    }

    // ------------------------------------------------------------------ layout

    /// <summary>The height the popup needs at this width (in pixels): the title, all rows and the bottom part.</summary>
    internal int HeightFor(int width)
    {
        int panelWidth = Math.Max(1, width - 2 * LogicalToDeviceUnits(SideMargin));
        return LogicalToDeviceUnits(TitleHeight) + ArrangeRows(panelWidth, place: false) + LogicalToDeviceUnits(FooterHeight);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int side = LogicalToDeviceUnits(SideMargin);
        int title = LogicalToDeviceUnits(TitleHeight);
        int footer = LogicalToDeviceUnits(FooterHeight);

        // The title after the cog (the cog is painted, see OnPaint).
        TitleLabel.Location = new Point(side + LogicalToDeviceUnits(CogSize + 10), (title - TitleLabel.Height) / 2 + LogicalToDeviceUnits(2));

        var panel = new Rectangle(side, title, Math.Max(1, Width - 2 * side), Math.Max(1, Height - title - footer));
        if (WhitePanel.Bounds != panel)
        {
            WhitePanel.Bounds = panel;
        }

        // Save against the right edge, Cancel left of it, both in the middle of the bottom part.
        int pill = LogicalToDeviceUnits(PillHeight);
        SavePill.Height = pill;
        CancelPill.Height = pill;
        SavePill.Width = SavePill.WidthNeeded();
        CancelPill.Width = CancelPill.WidthNeeded();
        int middle = Height - footer / 2;
        SavePill.Location = new Point(Width - side - SavePill.Width, middle - pill / 2);
        CancelPill.Location = new Point(SavePill.Left - LogicalToDeviceUnits(PillGap) - CancelPill.Width, SavePill.Top);

        // Where it is saved, at the left; a path that does not fit ends in "…".
        int noteWidth = Math.Max(1, CancelPill.Left - LogicalToDeviceUnits(16) - side);
        SavedInLabel.Size = new Size(noteWidth, SavedInLabel.PreferredHeight);
        SettingsPathLabel.Size = new Size(noteWidth, SettingsPathLabel.PreferredHeight);
        int notes = SavedInLabel.Height + SettingsPathLabel.Height;
        SavedInLabel.Location = new Point(side, middle - notes / 2);
        SettingsPathLabel.Location = new Point(side, SavedInLabel.Bottom);
    }

    /// <summary>
    /// Puts the rows in the white panel, one under the other, each group after its title: left the name (with its
    /// settings.json key behind it, or on the next line when there is no room) and the explanation below it, which wrap;
    /// right the field, in the middle of the row's height. Returns the height of all rows. With <paramref name="place"/>
    /// false it only measures. Sizes are set here, by hand, in pixels (not by a TableLayoutPanel: a table that wraps labels
    /// itself went wrong at 125% / 150% in TsDssConverter 1.1.0).
    /// </summary>
    private int ArrangeRows(int width, bool place)
    {
        if (place && _arranging)
        {
            return 0; // moving a row makes the panel lay out again: that is this method, already busy
        }

        _arranging = place;
        try
        {
            int pad = LogicalToDeviceUnits(TextPadding);
            int gap = LogicalToDeviceUnits(ColumnGap);
            int rowPad = LogicalToDeviceUnits(RowPadding);
            int column = Math.Min(LogicalToDeviceUnits(FieldColumn), Math.Max(LogicalToDeviceUnits(100), width / 3));
            int fieldLeft = width - pad - column;
            int textWidth = Math.Max(LogicalToDeviceUnits(80), fieldLeft - gap - pad);
            int scroll = place ? WhitePanel.AutoScrollPosition.Y : 0;
            var lines = new List<int>();
            int y = 0;

            foreach (ParameterRow row in Rows)
            {
                if (row.GroupTitle != null)
                {
                    // The group title: its own line, no line above it (the white space separates the groups).
                    if (lines.Count > 0)
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }

                    int groupTop = y + LogicalToDeviceUnits(GroupTop);
                    Size group = row.GroupTitle.GetPreferredSize(new Size(width - 2 * pad, 0));
                    if (place)
                    {
                        row.GroupTitle.Bounds = new Rectangle(pad, groupTop + scroll, Math.Min(group.Width, width - 2 * pad), group.Height);
                    }

                    y = groupTop + group.Height - LogicalToDeviceUnits(4);
                }

                int top = y + rowPad;

                Size caption = row.Caption.GetPreferredSize(new Size(textWidth, 0));
                caption.Width = Math.Min(caption.Width, textWidth);
                Size tag = row.KeyTag.GetPreferredSize(Size.Empty);
                int tagGap = LogicalToDeviceUnits(8);
                Point tagAt;
                int textBottom;
                if (caption.Width + tagGap + tag.Width <= textWidth)
                {
                    tagAt = new Point(pad + caption.Width + tagGap, top + (caption.Height - tag.Height) / 2);
                    textBottom = top + caption.Height;
                }
                else
                {
                    tagAt = new Point(pad, top + caption.Height + LogicalToDeviceUnits(3));
                    textBottom = tagAt.Y + tag.Height;
                }

                int hintTop = textBottom + LogicalToDeviceUnits(3);
                int hintHeight = row.Hint.GetPreferredSize(new Size(textWidth, 0)).Height;

                Size field = FieldSize(row, column);
                int rowHeight = Math.Max(hintTop + hintHeight - top, field.Height);
                int fieldTop = top + (rowHeight - field.Height) / 2;

                if (place)
                {
                    row.Caption.Bounds = new Rectangle(pad, top + scroll, caption.Width, caption.Height);
                    row.KeyTag.Bounds = new Rectangle(tagAt.X, tagAt.Y + scroll, tag.Width, tag.Height);
                    row.Hint.Bounds = new Rectangle(pad, hintTop + scroll, textWidth, hintHeight);
                    row.Field.Bounds = new Rectangle(fieldLeft, fieldTop + scroll, field.Width, field.Height);
                    if (row.After != null)
                    {
                        Size after = row.After.GetPreferredSize(Size.Empty);
                        row.After.Bounds = new Rectangle(row.Field.Right + LogicalToDeviceUnits(10), fieldTop + scroll + (field.Height - after.Height) / 2, after.Width, after.Height);
                    }
                }

                y = top + rowHeight + rowPad;
                lines.Add(y);
            }

            if (place)
            {
                if (lines.Count > 0)
                {
                    lines.RemoveAt(lines.Count - 1); // no line under the last row
                }

                WhitePanel.Lines = lines;
                WhitePanel.AutoScrollMinSize = new Size(0, y);
                WhitePanel.Invalidate();
            }

            return y;
        }
        finally
        {
            _arranging = false;
        }
    }

    /// <summary>The size of a row's field: a check box is small and square, a drop-down as high as Windows makes it.</summary>
    private Size FieldSize(ParameterRow row, int column)
    {
        if (row.Field is CheckBox)
        {
            int check = LogicalToDeviceUnits(CheckSize);
            return new Size(check, check);
        }

        int width = row.FieldWidth == 0 ? column : Math.Min(LogicalToDeviceUnits(row.FieldWidth), column);
        int height = row.Field is ComboBox combo ? combo.Height : LogicalToDeviceUnits(FieldHeight);
        return new Size(width, height);
    }

    // ------------------------------------------------------------------ painting

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        // Around the rounded corners the dimmed window must show, not a square of another colour.
        if (Parent is DimOverlay overlay)
        {
            overlay.PaintBackdropFor(this, g);
        }
        else
        {
            g.Clear(Theme.Frame);
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath card = FluentDrawing.RoundedRectangle(new RectangleF(0, 0, Width, Height), LogicalToDeviceUnits(CornerRadius));
        using var grey = new SolidBrush(Theme.WorkArea);
        g.FillPath(grey, card);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cog = LogicalToDeviceUnits(CogSize);
        float top = TitleLabel.Top + (TitleLabel.Height - cog) / 2f;
        MaestroDrawing.DrawCog(g, new RectangleF(LogicalToDeviceUnits(SideMargin), top, cog, cog), Theme.ButtonNavy, Theme.WorkArea);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captionFont.Dispose();
            _titleFont.Dispose();
            _keyFont.Dispose();
            _groupFont.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// The white panel inside the popup: white with rounded corners on the popup's grey, thin grey lines between the rows.
/// Scrolls (AutoScroll) when the rows do not fit.
/// </summary>
internal class RoundedWhitePanel : Panel
{
    private const int Radius = 12;      // at 100%
    private const int LineMargin = 20;  // the lines between the rows stop this far from the edges, at 100%

    /// <summary>Where the lines between the rows are, in the panel's content (before scrolling), in pixels.</summary>
    internal List<int> Lines = new();

    public RoundedWhitePanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.White; // the labels and check boxes on it take this as their background
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.WorkArea);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (GraphicsPath shape = FluentDrawing.RoundedRectangle(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), LogicalToDeviceUnits(Radius)))
        using (var white = new SolidBrush(Theme.White))
        {
            g.FillPath(white, shape);
        }

        g.SmoothingMode = SmoothingMode.None;
        using var pen = new Pen(Theme.GridLine);
        int margin = LogicalToDeviceUnits(LineMargin);
        foreach (int line in Lines)
        {
            int y = line + AutoScrollPosition.Y;
            g.DrawLine(pen, margin, y, ClientSize.Width - margin, y);
        }
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        Invalidate(); // the lines move with the rows
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Invalidate(); // scrolling with the wheel moves the rows too
    }
}
