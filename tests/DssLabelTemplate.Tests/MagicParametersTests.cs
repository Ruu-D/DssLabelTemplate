namespace DssLabelTemplate.Tests;

/// <summary>
/// The "Magic parameters" (2.0.1): the cog right of the "?" opens a popup with the settings that are not in the window
/// itself, over the window dimmed in navy. Saving changes only those values in settings.json.
/// </summary>
public class MagicParametersTests
{
    // The settings the popup must show, in the order of Daan's mockup (SettingsTests checks that these are exactly the
    // settings that are not in the window).
    private static readonly string[] PopupSettings =
    {
        "SheetRotationInViewer", "ShowLabelRotation", "ShowBoardByDefault", "RowHeightCSV",
        "PrintLabelWithRotation", "HideWindowsPrintDialog", "LabelDirection", "SheetReferenceCorner",
    };

    /// <summary>A window with fake settings, shown invisibly; with a data folder only when a test needs settings.json.</summary>
    private sealed class Window : IDisposable
    {
        public AppSettings Settings { get; }
        public MainForm Form { get; }

        public Window(AppSettings? settings = null, string? dataFolder = null)
        {
            Settings = settings ?? new AppSettings();
            Form = new MainForm(Settings, saved => { }, fileToOpen: null, dataFolder) { Opacity = 0, ShowInTaskbar = false };
            Form.Show();
            Pump();
        }

        public void Pump()
        {
            for (int i = 0; i < 5; i++)
            {
                Application.DoEvents();
            }
        }

        public void Dispose()
        {
            Form.Dispose();
        }
    }

    private static int Left(Control control) => control.PointToScreen(Point.Empty).X;
    private static int Right(Control control) => control.PointToScreen(new Point(control.Width, 0)).X;
    private static int Middle(Control control) => control.PointToScreen(new Point(0, control.Height / 2)).Y;

    [Fact]
    public void TheCog_SitsRightOfTheQuestionMark_TheSameSize_BeforeTheClock()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            Assert.Equal(DiscGlyph.Cog, form.MagicParametersButton.Glyph);
            Assert.True(form.MagicParametersButton.Flat);
            Assert.Equal(form.UserGuideButton.Size, form.MagicParametersButton.Size);
            Assert.True(Right(form.UserGuideButton) <= Left(form.MagicParametersButton));
            Assert.True(Right(form.MagicParametersButton) < Left(form.Clock));
            Assert.InRange(Middle(form.MagicParametersButton) - Middle(form.UserGuideButton), -1, 1);
        });
    }

    [Fact]
    public void TheCog_OpensTheMagicParameters_WithARowForEverySettingThatIsNotInTheWindow_ShowingTheValuesInUse()
    {
        Sta.Run(() =>
        {
            var settings = new AppSettings
            {
                SheetRotationInViewer = 180,
                ShowLabelRotation = true,
                ShowBoardByDefault = true,
                RowHeightCSV = 150,
                PrintLabelWithRotation = true,
                HideWindowsPrintDialog = true,
                LabelDirection = AppSettings.Clockwise,
                SheetReferenceCorner = "Rechtsboven",
            };
            using var window = new Window(settings);
            MainForm form = window.Form;
            Assert.Null(form.MagicParameters);

            form.MagicParametersButton.PerformClick();
            MagicParametersPopup popup = form.MagicParameters!;

            Assert.Equal(PopupSettings, popup.Rows.Select(row => row.Key));
            Assert.All(popup.Rows, row => Assert.Equal(row.Key, row.KeyTag.Text));
            Assert.Equal(3, popup.Rows.Count(row => row.GroupTitle != null)); // display, printing, machine
            Assert.Equal("Magic parameters", popup.TitleLabel.Text);

            Assert.Equal(180, ((MagicParametersPopup.Choice)popup.SheetRotationBox.SelectedItem!).Value);
            Assert.True(popup.ShowLabelRotationBox.Checked);
            Assert.True(popup.ShowBoardBox.Checked);
            Assert.Equal(150, popup.CsvSizeBox.Value);
            Assert.True(popup.PrintRotationBox.Checked);
            Assert.True(popup.HidePrintDialogBox.Checked);
            Assert.Equal(AppSettings.Clockwise, ((MagicParametersPopup.Choice)popup.LabelDirectionBox.SelectedItem!).Value);
            Assert.Equal("Rechtsboven", ((MagicParametersPopup.Choice)popup.ReferenceCornerBox.SelectedItem!).Value);

            // The true/false settings are check boxes in the right column, where the other fields are.
            int fieldLeft = popup.SheetRotationBox.Left;
            Assert.All(popup.Rows, row => Assert.Equal(fieldLeft, row.Field.Left));
            Assert.All(popup.Rows.Where(row => row.Field is CheckBox), row => Assert.Equal("", row.Field.Text));
        });
    }

    [Fact]
    public void MagicParameters_SwitchTheWindowOff_AndCancelGivesItBack_WithoutChangingAnything()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Control[] parts = form.Controls.Cast<Control>().ToArray();

            form.OpenMagicParameters();
            window.Pump();
            MagicParametersPopup popup = form.MagicParameters!;
            var overlay = (DimOverlay)popup.Parent!;

            Assert.Equal(form.ClientRectangle, overlay.Bounds);
            Assert.Equal(0, form.Controls.GetChildIndex(overlay)); // on top of everything, also the top bar
            Assert.All(parts, part => Assert.False(part.Enabled));
            Assert.Same(popup.SavePill, form.AcceptButton);      // Enter = Save
            Assert.Same(popup.CancelPill, form.CancelButton);    // Esc = Cancel

            popup.ShowBoardBox.Checked = true;
            popup.CancelPill.PerformClick();
            window.Pump();

            Assert.Null(form.MagicParameters);
            Assert.False(window.Settings.ShowBoardByDefault);   // nothing changed
            Assert.All(parts, part => Assert.True(part.Enabled));
            Assert.Null(form.AcceptButton);
            Assert.Null(form.CancelButton);
        });
    }

    [Fact]
    public void MagicParameters_Save_WritesOnlyTheirValuesToSettingsJson_AndUsesThemAtOnce()
    {
        Sta.Run(() =>
        {
            using var folder = new TempFolder();
            string path = AppDataFolder.SettingsFile(folder.Path);
            Directory.CreateDirectory(folder.Path);

            // In the file: a language and a label size that the window does not have in use (they must stay).
            var inFile = new AppSettings { Language = "fr", LabelLength = 60, LabelWidth = 60 };
            SettingsStore.Save(path, inFile);

            using var window = new Window(new AppSettings(), folder.Path);
            MainForm form = window.Form;
            form.OpenMagicParameters();
            MagicParametersPopup popup = form.MagicParameters!;

            popup.SheetRotationBox.SelectedIndex = 3;      // 90° clockwise = -90
            popup.ShowLabelRotationBox.Checked = true;
            popup.ShowBoardBox.Checked = true;
            popup.CsvSizeBox.Value = 200;
            popup.PrintRotationBox.Checked = true;
            popup.HidePrintDialogBox.Checked = true;
            popup.LabelDirectionBox.SelectedIndex = 1;     // CW
            popup.ReferenceCornerBox.SelectedIndex = 1;    // Linksboven
            popup.SavePill.PerformClick();
            window.Pump();

            Assert.Null(form.MagicParameters);

            // Used at once by the window ...
            Assert.Equal(-90, window.Settings.SheetRotationInViewer);
            Assert.Equal(200, window.Settings.RowHeightCSV);
            Assert.Equal(AppSettings.Clockwise, window.Settings.LabelDirection);

            // ... and in the file, where the rest stayed as it was.
            AppSettings read = SettingsStore.Load(path);
            Assert.Equal(-90, read.SheetRotationInViewer);
            Assert.True(read.ShowLabelRotation);
            Assert.True(read.ShowBoardByDefault);
            Assert.Equal(200, read.RowHeightCSV);
            Assert.True(read.PrintLabelWithRotation);
            Assert.True(read.HideWindowsPrintDialog);
            Assert.Equal(AppSettings.Clockwise, read.LabelDirection);
            Assert.Equal("Linksboven", read.SheetReferenceCorner);
            Assert.Equal("fr", read.Language);
            Assert.Equal(60, read.LabelLength);
        });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 90)]
    [InlineData(180, 180)]
    [InlineData(270, -90)]
    [InlineData(-90, -90)]
    [InlineData(-180, 180)]
    [InlineData(-270, 90)]
    public void ASheetRotationEditedByHand_IsShownAsTheChoiceThatTurnsTheSameWay(int inFile, int shown)
    {
        Assert.Equal(shown, MagicParametersPopup.SameQuarterTurn(inFile));
    }

    [Fact]
    public void ASheetRotationThatIsNotChanged_KeepsTheNumberOfTheFile()
    {
        Sta.Run(() =>
        {
            using var window = new Window(new AppSettings { SheetRotationInViewer = 270 });
            window.Form.OpenMagicParameters();
            MagicParametersPopup popup = window.Form.MagicParameters!;

            Assert.Equal(-90, ((MagicParametersPopup.Choice)popup.SheetRotationBox.SelectedItem!).Value);
            Assert.Equal(270, popup.ReadValues().SheetRotationInViewer); // the same turn: not rewritten as -90
        });
    }

    [Theory]
    [InlineData(AppLanguage.Dutch, false)]
    [InlineData(AppLanguage.French, false)]
    [InlineData(AppLanguage.English, false)]
    [InlineData(AppLanguage.Dutch, true)]
    [InlineData(AppLanguage.French, true)]
    [InlineData(AppLanguage.English, true)]
    public void ThePopup_IsAtMostTwoThirdsOfTheWindowHigh_InTheMiddle_AndNothingInItOverlapsOrIsCutOff(AppLanguage language, bool smallestWindow)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;
            if (smallestWindow)
            {
                form.Size = form.MinimumSize;
                window.Pump();
            }

            form.OpenMagicParameters();
            window.Pump();
            MagicParametersPopup popup = form.MagicParameters!;
            Control overlay = popup.Parent!;
            overlay.PerformLayout();

            // At most 2/3 of the window's height (Daan), in the middle, inside the window.
            Assert.True(popup.Height <= overlay.Height * 2 / 3 + 1, $"the popup is {popup.Height} px high, the window {overlay.Height}");
            Assert.True(overlay.ClientRectangle.Contains(popup.Bounds));
            Assert.InRange(popup.Left - (overlay.Width - popup.Right), -1, 1);
            Assert.InRange(popup.Top - (overlay.Height - popup.Bottom), -1, 1);

            // The rows: one under the other, the text left of the field, everything inside the white panel's width.
            RoundedWhitePanel panel = popup.WhitePanel;
            int previousBottom = int.MinValue;
            foreach (ParameterRow row in popup.Rows)
            {
                if (row.GroupTitle != null)
                {
                    Assert.True(row.GroupTitle.Top >= previousBottom, $"the group above {row.Key} runs into the row above it");
                    Assert.True(row.Caption.Top >= row.GroupTitle.Bottom, row.Key);
                }

                Assert.True(row.Caption.Top >= previousBottom, $"the row {row.Key} runs into the row above it");
                Assert.True(row.Hint.Top >= row.Caption.Bottom, row.Key);
                Assert.True(row.Hint.Right <= row.Field.Left, $"the explanation of {row.Key} runs into its field");
                Assert.True(row.KeyTag.Right <= row.Field.Left, $"the tag of {row.Key} runs into its field");
                Assert.True(row.Caption.GetPreferredSize(new Size(row.Caption.Width, 0)).Height <= row.Caption.Height, $"'{row.Caption.Text}' is cut off");
                Assert.True(row.Hint.GetPreferredSize(new Size(row.Hint.Width, 0)).Height <= row.Hint.Height, $"'{row.Hint.Text}' is cut off");
                Control last = row.After ?? row.Field;
                Assert.True(last.Right <= panel.ClientSize.Width, $"the field of {row.Key} sticks out of the panel");
                previousBottom = Math.Max(row.Hint.Bottom, row.Field.Bottom);
            }

            // The rows that do not fit scroll inside the white panel.
            Assert.True(panel.AutoScroll);
            Assert.True(panel.AutoScrollMinSize.Height >= previousBottom - panel.AutoScrollPosition.Y);

            // The bottom: where it is saved, then Cancel and Save, inside the popup.
            Assert.True(popup.SavedInLabel.Right < popup.CancelPill.Left);
            Assert.True(popup.CancelPill.Right < popup.SavePill.Left);
            Assert.True(popup.SavePill.Right <= popup.Width);
            Assert.True(popup.SavePill.Bottom <= popup.Height);
            Assert.True(panel.Bottom <= popup.SavePill.Top);
            Assert.True(popup.SavePill.WidthNeeded() <= popup.SavePill.Width);
            Assert.True(popup.CancelPill.WidthNeeded() <= popup.CancelPill.Width);
        });
    }

    [Fact]
    public void TheWindowsShortcuts_DoNothing_WhileTheMagicParametersAreOpen()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenMagicParameters();
            window.Pump();

            // F11 would make the window full screen; while the popup is open it does nothing.
            FormWindowState before = form.WindowState;
            Rectangle bounds = form.Bounds;
            SendKeysTo(form, Keys.F11);
            window.Pump();
            Assert.Equal(before, form.WindowState);
            Assert.Equal(bounds, form.Bounds);
            Assert.NotNull(form.MagicParameters);
        });
    }

    /// <summary>Lets the window handle a key as if it was pressed (ProcessCmdKey is where its shortcuts are).</summary>
    private static void SendKeysTo(MainForm form, Keys key)
    {
        var message = Message.Create(form.Handle, 0x0100 /* WM_KEYDOWN */, (IntPtr)key, IntPtr.Zero);
        var method = typeof(MainForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        method.Invoke(form, new object[] { message, key });
    }
}
