namespace DssLabelTemplate.Tests;

/// <summary>
/// The window. These tests check what must stay true (alignment, equal columns, texts that fit, opening and saving,
/// the arrows), not pixel values. They run at the scaling of the test process (not DPI aware = 100%): look at the real
/// window at 125% and 150% too (see CLAUDE.md).
/// </summary>
public class MainFormTests
{
    /// <summary>A window with fake settings, shown invisibly (Opacity 0) so the layout is really done.</summary>
    private sealed class Window : IDisposable
    {
        public AppSettings Settings { get; }
        public int SaveCount { get; private set; }
        public MainForm Form { get; }

        public Window(AppSettings? settings = null)
        {
            Settings = settings ?? new AppSettings();
            Form = new MainForm(Settings, saved => SaveCount++, fileToOpen: null)
            {
                Opacity = 0,
                ShowInTaskbar = false,
            };
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

    private static int RightEdge(Control control) => control.PointToScreen(new Point(control.Width, 0)).X;
    private static int Left(Control control) => control.PointToScreen(Point.Empty).X;
    private static int Top(Control control) => control.PointToScreen(Point.Empty).Y;
    private static int Bottom(Control control) => control.PointToScreen(new Point(0, control.Height)).Y;

    // ---------------------------------------------------------------- the bottom bar and the tabs (2.0.0)

    [Fact]
    public void TheRoundButtons_SitInTheBottomBar_CsvAndBoardAtTheLeft_TheArrowsAtTheEndOfTheBand_PrintOnTheGrey()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.False(form.BoardButton.Visible); // nothing open: no sheet

            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();

            foreach (DiscButton disc in new[] { form.CsvViewButton, form.BoardButton, form.PreviousButton, form.NextButton })
            {
                Assert.True(disc.Visible);
                Assert.True(form.BottomBar.Contains(disc));
                int middle = Top(disc) + disc.Height / 2, arrowMiddle = Top(form.PreviousButton) + form.PreviousButton.Height / 2;
                Assert.InRange(middle - arrowMiddle, -1, 1); // one line
                Assert.True(disc.Height >= form.LogicalToDeviceUnits(56), "big enough for a finger: " + disc.Height);
                Assert.False(disc.TabStop);
            }

            // The csv and board buttons: the signs of the mockup (Daan, 2026-11-21), 20% bigger than the arrows so that the
            // chosen one can grow without the button changing size.
            Assert.Equal(Strings.CsvSign, form.CsvViewButton.Text);
            Assert.Equal(DiscGlyph.Board, form.BoardButton.Glyph);
            Assert.True(form.CsvViewButton.GrowWhenSelected && form.BoardButton.GrowWhenSelected);
            Assert.False(form.PreviousButton.GrowWhenSelected || form.NextButton.GrowWhenSelected);
            Assert.Equal(form.CsvViewButton.Size, form.BoardButton.Size);
            Assert.InRange(form.BoardButton.Width / (double)form.PreviousButton.Width, 1.18, 1.22);
            Assert.Equal(form.PreviousButton.Size, form.NextButton.Size);

            // Left to right: csv, board, ..., previous, the counter, next; then the end of the blue band; then print.
            Assert.True(RightEdge(form.CsvViewButton) < Left(form.BoardButton));
            Assert.True(RightEdge(form.BoardButton) < Left(form.PreviousButton));
            Assert.True(RightEdge(form.PreviousButton) <= Left(form.LabelCounter));
            Assert.True(RightEdge(form.LabelCounter) <= Left(form.NextButton));
            int bandEnd = Left(form.BottomBar) + form.BottomBar.BandRight;
            Assert.True(RightEdge(form.NextButton) <= bandEnd, "next is on the band");
            Assert.True(Left(form.PrintButton) > bandEnd, "print is on the grey right of the band");
            Assert.Equal(RightEdge(form.BottomBar) - form.LogicalToDeviceUnits(24), RightEdge(form.PrintButton));

            // Only "Template (xml)" is a tab in the left panel's title (Daan, 2026-11-21: the Table and Board tabs went,
            // the round buttons do it), against the panel's right edge.
            Assert.True(form.XmlViewButton.Visible && form.EditorHeader.Contains(form.XmlViewButton));
            Assert.DoesNotContain(form.EditorHeader.Controls.Cast<Control>(), control => control is TabButton tab && tab != form.XmlViewButton);
            Assert.Equal(RightEdge(form.EditorArea), RightEdge(form.XmlViewButton));

            form.Width = form.MinimumSize.Width; // the narrowest window: nothing runs into anything
            window.Pump();
            Assert.True(RightEdge(form.BoardButton) < Left(form.PreviousButton));
            Assert.True(RightEdge(form.NextButton) < Left(form.PrintButton));
            Assert.True(RightEdge(form.PrintButton) <= RightEdge(form.BottomBar));
        });
    }

    [Fact]
    public void TheWindowHasNoWindowsTitleBar_ItsButtonsAndAClockAreInTheTopBar()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.Equal(FormBorderStyle.None, form.FormBorderStyle); // Daan, 2026-11-21

            // At the top right, in this order: the "?", the clock, minimise, maximise, close; close against the right.
            Assert.True(RightEdge(form.UserGuideButton) < Left(form.Clock));
            Assert.True(RightEdge(form.Clock) < Left(form.MinimizeButton));
            Assert.True(RightEdge(form.MinimizeButton) < Left(form.MaximizeButton));
            Assert.True(RightEdge(form.MaximizeButton) < Left(form.CloseButton));
            Assert.Equal(RightEdge(form.TopBar) - form.TopBar.Padding.Right, RightEdge(form.CloseButton));
            Assert.Matches(@"^\d\d:\d\d$", form.Clock.Time);
            Assert.True(form.Clock.WidthNeeded() <= form.Clock.Width, "the clock shows its whole time");
            Assert.InRange(form.Clock.TimeFont.Size * 0.73 / form.LogicalToDeviceUnits(18), 0.97, 1.03); // digits as high as the sign
            Assert.True(form.Clock.HeightNeeded() <= form.Clock.Height);

            // Maximise and back: the sign follows, and a maximised window has no resize edge.
            form.MaximizeButton.PerformClick();
            window.Pump();
            Assert.Equal(FormWindowState.Maximized, form.WindowState);
            Assert.True(form.MaximizeButton.Maximised);
            Assert.Equal(Padding.Empty, form.Padding);
            form.ToggleMaximized();
            window.Pump();
            Assert.Equal(FormWindowState.Normal, form.WindowState);
            Assert.False(form.MaximizeButton.Maximised);
            Assert.True(form.Padding.Left > 0);

            // F11, like a browser: the whole monitor (taskbar included), F11 again: back (Daan, 2026-11-21).
            Rectangle before = form.Bounds;
            form.ToggleFullScreen();
            window.Pump();
            Assert.True(form.IsFullScreen);
            Assert.Equal(Screen.FromControl(form).Bounds, form.Bounds);
            form.ToggleFullScreen();
            window.Pump();
            Assert.False(form.IsFullScreen);
            Assert.Equal(FormWindowState.Normal, form.WindowState);
            Assert.Equal(before, form.Bounds);

            // The ROGIERS logo and the "?" are as high as the clock sign, the "?" flat and grey like it.
            Assert.Equal(form.LogicalToDeviceUnits(18), form.LogoPicture.Height);
            Assert.True(form.UserGuideButton.Flat);
            Assert.Equal(18, form.UserGuideButton.FlatDiameter);

            form.MinimizeButton.PerformClick();
            Assert.Equal(FormWindowState.Minimized, form.WindowState);
            form.WindowState = FormWindowState.Normal;

            form.CloseButton.PerformClick(); // nothing changed: it closes without a question
            Assert.True(form.IsDisposed || !form.Visible);
        });
    }

    [Fact]
    public void TheCsvAndTheBoardButton_AreTheSwitch_ClosingTheBoardShowsTheTable()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();

            form.XmlViewButton.PerformClick(); // the template text
            form.BoardButton.PerformClick();   // the board
            window.Pump();
            Assert.True(form.ShowingBoard);
            Assert.True(form.BoardButton.Selected);
            Assert.False(form.XmlViewButton.Selected);

            form.BoardButton.PerformClick(); // closed: the table, not the text that was shown before
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.True(form.ShowingCsv);
            Assert.True(form.CsvViewButton.Selected);

            form.BoardButton.PerformClick();
            form.CsvViewButton.PerformClick(); // the csv button closes the board too
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.True(form.CsvGrid.Parent!.Visible);
            Assert.False(form.BoardButton.Selected);

            form.CsvViewButton.PerformClick(); // again: the table stays (the template has its own tab)
            Assert.True(form.ShowingCsv);

            form.BoardButton.PerformClick();
            form.XmlViewButton.PerformClick(); // the tab closes the board too
            Assert.False(form.ShowingCsv);
            Assert.True(form.XmlViewButton.Selected);
            Assert.False(form.CsvViewButton.Selected || form.BoardButton.Selected); // exactly one of the three is chosen
        });

        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null); // no csv: closing the board shows the text
            window.Pump();
            Assert.False(form.CsvViewButton.Visible);
            form.BoardButton.PerformClick();
            form.BoardButton.PerformClick();
            window.Pump();
            Assert.False(form.ShowingCsv);
            Assert.True(form.Editor.Parent!.Visible);
        });
    }

    [Fact]
    public void WithShowBoardByDefault_EveryOpenedFileIsShownOnTheBoard_AndTheBoardCanStillBeClosed()
    {
        Sta.Run(() =>
        {
            using var window = new Window(new AppSettings { ShowBoardByDefault = true });
            MainForm form = window.Form;
            Assert.False(form.ShowingBoard); // nothing open yet

            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();
            Assert.True(form.ShowingBoard);
            Assert.True(form.Board.Visible);

            form.BoardButton.PerformClick(); // closed by the operator: the table
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.True(form.ShowingCsv);

            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null); // the next file: on the board again
            window.Pump();
            Assert.True(form.ShowingBoard);
        });

        Sta.Run(() =>
        {
            using var window = new Window(); // the default: off
            window.Form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();
            Assert.False(window.Form.ShowingBoard);
            Assert.True(window.Form.ShowingCsv);
        });
    }

    [Fact]
    public void TheBrowseButtons_AreTouchSized_AndThePathBoxesAreAsHighAndInLine()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            foreach (RoundedButton browse in new[] { form.XmlBrowseButton, form.CsvBrowseButton })
            {
                Control frame = (form.XmlBrowseButton == browse ? form.XmlFileBox : form.CsvFileBox).Parent!;
                Assert.True(browse.Height >= form.LogicalToDeviceUnits(44), "at least 44 high at 100%: " + browse.Height);
                Assert.True(browse.Height > form.SaveButton.Height, "higher than a normal button");
                Assert.NotNull(browse.Image); // the folder before the text
                Assert.Equal(browse.Height, frame.Height);
                Assert.Equal(Top(browse), Top(frame));
            }

            Assert.Equal(form.XmlBrowseButton.Size, form.CsvBrowseButton.Size);
            Assert.True(Top(form.CsvBrowseButton) >= Top(form.XmlBrowseButton) + form.XmlBrowseButton.Height, "the rows do not overlap");
        });
    }

    [Fact]
    public void RowHeightCSV_MakesTheTextAndTheRowsOfTheCsvTableBigger_AlsoWhenChangedWhileRunning()
    {
        (float Points, int RowHeight, int TitleHeight) Measure(MainForm form) =>
            (form.CsvGrid.Font.SizeInPoints, form.CsvGrid.Rows[0].Height, form.CsvGrid.ColumnHeadersHeight);

        Sta.Run(() =>
        {
            using var standard = new Window();
            standard.Form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            standard.Pump();
            var normal = Measure(standard.Form);

            using var big = new Window(new AppSettings { RowHeightCSV = 150 });
            big.Form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            big.Pump();
            var bigger = Measure(big.Form);

            Assert.InRange(bigger.Points / normal.Points, 1.45, 1.55);
            Assert.InRange((double)bigger.RowHeight / normal.RowHeight, 1.35, 1.65);
            Assert.True(bigger.TitleHeight > normal.TitleHeight, "the column titles grow too");

            // Edited in settings.json while the program runs (read again on Activated): at once, rows that are there too.
            var file = new AppSettings { RowHeightCSV = 200 };
            Assert.True(standard.Form.UseHandEditedSettings(file));
            standard.Pump();
            var twice = Measure(standard.Form);
            Assert.InRange(twice.Points / normal.Points, 1.95, 2.05);
            Assert.InRange((double)twice.RowHeight / normal.RowHeight, 1.8, 2.2);
            Assert.All(standard.Form.CsvGrid.Rows.Cast<DataGridViewRow>(), row => Assert.Equal(twice.RowHeight, row.Height));
        });
    }

    [Fact]
    public void TheLabelCounter_IsTwiceTheWindowsFont_WhiteAndBold_AndFitsTheLongestCounter()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.InRange(form.LabelCounter.Font.SizeInPoints / form.Font.SizeInPoints, 1.95, 2.05);
            Assert.True(form.LabelCounter.Font.Bold);
            Assert.Equal(Theme.White, form.LabelCounter.ForeColor); // on the blue band

            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();
            Assert.True(TextRenderer.MeasureText("8 / 8", form.LabelCounter.Font).Width <= form.LabelCounter.Width);
        });
    }

    [Fact]
    public void TappingALabelOnTheBoard_ShowsThatLabel_AlsoJustBesideItsSmallSquare()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();
            form.BoardButton.PerformClick();
            window.Pump();
            Assert.Equal(0, form.Preview.Index);

            // Where label 3 (index 2) is drawn: the middle of its square on the board.
            LabelItem third = form.Preview.Labels!.Labels[2];
            Rectangle area = form.Board.SheetArea();
            float length = (float)third.SheetLength!.Value, width = (float)third.SheetWidth!.Value;
            var (x, y) = LabelPreview.ToBottomLeft(SheetCorner.BottomLeft, length, width, third.X!.Value, third.Y!.Value);
            PointF middle = LabelPreview.MapPoint(area, length, width, 0, x, y);

            Assert.Equal(2, form.Board.ChooseLabelAt(Point.Round(middle)));
            Assert.Equal(2, form.Preview.Index); // shown in the preview
            Assert.Equal("3 / 3", form.LabelCounter.Text);
            Assert.True(form.ShowingBoard); // the board stays

            // A finger is bigger than the square: just beside it still counts; far away nothing happens.
            float scale = LabelPreview.MapScale(area, length, width, 0);
            int halfSquare = (int)Math.Ceiling(Math.Max(form.Preview.LabelSize.Width * scale, form.LogicalToDeviceUnits(4)) / 2);
            form.ShowLabel(0);
            Assert.Equal(2, form.Board.ChooseLabelAt(new Point((int)middle.X + halfSquare + form.LogicalToDeviceUnits(10), (int)middle.Y)));
            form.ShowLabel(0);
            Assert.Equal(-1, form.Board.ChooseLabelAt(new Point(area.Right - 1, area.Top + 1)));
            Assert.Equal(0, form.Preview.Index);
        });
    }

    [Fact]
    public void TheBoardButton_ShowsTheBoardInTheLeftColumn_LooksPressed_AndAClickAgainGoesBack()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();
            Assert.True(form.ShowingCsv);

            form.BoardButton.PerformClick();
            window.Pump();

            Assert.True(form.ShowingBoard);
            Assert.True(form.Board.Visible);
            Assert.False(form.CsvGrid.Parent!.Visible);
            Assert.False(form.Editor.Parent!.Visible);
            Assert.Equal(Strings.CsvCaption, form.EditorCaption.Text); // the same title as the table (Daan, 2026-11-21)
            Assert.Equal(Samples.Csv, form.EditorCaption.Emphasis);
            Assert.Equal(Strings.LabelCount(3), form.EditorCaption.Suffix);
            Assert.True(form.BoardButton.Selected); // "pressed": the dark ring
            Assert.False(form.CsvViewButton.Selected);
            Assert.Equal(form.EditorArea.ClientSize.Height - form.EditorArea.Padding.Vertical, form.Board.Height); // the whole left panel
            Assert.Equal(form.EditorArea.ClientSize.Width, form.Board.Width);
            Assert.False(form.Preview.ShowSheetMap); // the small map next to the label is gone: the label gets its room

            form.ShowLabel(1); // the board follows the preview, and opening the next label does not close it
            window.Pump();
            Assert.True(form.ShowingBoard);

            form.BoardButton.PerformClick(); // again: back to the table it showed before
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.False(form.Board.Visible);
            Assert.True(form.CsvGrid.Parent!.Visible);
            Assert.True(form.Preview.ShowSheetMap); // the small map is back
            Assert.False(form.BoardButton.Selected);
            Assert.True(form.CsvViewButton.Selected);

            form.BoardButton.PerformClick();
            form.XmlViewButton.PerformClick(); // a view button above the column closes the board too
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.True(form.Editor.Parent!.Visible);
        });
    }

    // ---------------------------------------------------------------- settings.json edited by hand

    [Fact]
    public void HandEditedSettings_AreUsedAtOnce_ButTheWindowsOwnSettingsStay()
    {
        Sta.Run(() =>
        {
            using var window = new Window(new AppSettings { Language = "nl", LabelLength = 60 });
            MainForm form = window.Form;

            var file = new AppSettings
            {
                Language = "en",      // a setting of the window: not taken from the file here
                LabelLength = 80,     // the same
                SheetRotationInViewer = 90,
                ShowLabelRotation = true,
                HideWindowsPrintDialog = true,
                LabelDirection = AppSettings.Clockwise,
                SheetReferenceCorner = "Rechtsboven",
            };

            Assert.True(form.UseHandEditedSettings(file));

            Assert.Equal(90, form.Preview.SheetRotation);
            Assert.Equal(SheetCorner.TopRight, form.Preview.ReferenceCorner);
            Assert.True(form.Preview.ShowRotation);
            Assert.Equal(Strings.PrintTooltipWithoutDialog, form.PrintButton.AccessibleName);
            Assert.Equal("nl", window.Settings.Language);
            Assert.Equal(60, window.Settings.LabelLength);

            Assert.False(form.UseHandEditedSettings(file)); // the same again: nothing to do
        });
    }

    // ---------------------------------------------------------------- layout

    [Fact]
    public void TheBrowseButtons_ThePreviewAndTheTiles_EndInLineAtTheRight()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            // The white panels end at the same right edge; the Browse buttons end at the inner padding of theirs.
            Assert.Equal(RightEdge(form.XmlBrowseButton), RightEdge(form.CsvBrowseButton));
            Assert.Equal(RightEdge(form.Preview) - form.LogicalToDeviceUnits(16), RightEdge(form.XmlBrowseButton));
            Assert.Equal(RightEdge(form.Preview), RightEdge(form.SheetTile));
            Assert.Equal(RightEdge(form.Preview) - form.LogicalToDeviceUnits(12), RightEdge(form.LabelWidthBox.Parent!));
        });
    }

    [Fact]
    public void TheTwoPanels_StartAndEndInLine_WithTheSameGreyRoomAroundThem_AndTheBarsAtTheTopAndBottom()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Control editorFrame = form.Editor.Parent!;

            Assert.InRange(Math.Abs(editorFrame.Width - form.Preview.Width), 0, 1);
            Assert.InRange(Top(editorFrame) - Top(form.Preview), 0, 1); // (the 1 px line below the left panel's title)
            Assert.Equal(Bottom(editorFrame), Bottom(form.Tiles)); // the left panel ends with the tiles
            Assert.True(form.Preview.Height > 200, "the preview is the big part: " + form.Preview.Height);
            Assert.True(Top(form.Tiles) > Bottom(form.Preview), "the tiles are below the preview");

            // The top bar at the top, the bottom bar at the bottom (inside the thin resize edge of the window, which has no
            // Windows frame), the same grey room between them and the panels as at the left of the panels.
            int gap = form.LogicalToDeviceUnits(14), edge = form.Padding.Left;
            Assert.True(edge > 0, "a normal window has an edge to resize with");
            Assert.Equal(form.PointToScreen(Point.Empty).Y, Top(form.TopBar)); // no edge at the top: the title tab sticks to it
            Assert.Equal(form.PointToScreen(new Point(0, form.ClientSize.Height)).Y - edge, Bottom(form.BottomBar));
            Assert.Equal(edge + gap, Left(editorFrame) - form.PointToScreen(Point.Empty).X);
            Assert.Equal(gap, Top(form.BottomBar) - Bottom(form.Tiles));
            Assert.Equal(gap, Top(form.XmlFileBox.Parent!.Parent!.Parent!.Parent!) - Bottom(form.TopBar)); // field, holder, lines, files panel
        });
    }

    [Fact]
    public void TheCsvTable_HasABiggerFont_AndRowsWithRoomAroundTheText_ForATouchScreen()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            form.ShowCsvView(true);
            window.Pump();

            Assert.True(form.CsvGrid.Font.SizeInPoints >= form.Font.SizeInPoints + 4, "font " + form.CsvGrid.Font.SizeInPoints);
            Assert.True(form.CsvGrid.ColumnHeadersDefaultCellStyle.Font!.SizeInPoints >= form.CsvGrid.Font.SizeInPoints);
            int rowHeight = form.CsvGrid.Rows[0].Height;
            Assert.True(rowHeight >= form.CsvGrid.Font.Height + 10, $"row {rowHeight} for a font of {form.CsvGrid.Font.Height}");
        });
    }

    [Fact]
    public void SaveTemplate_IsOnlyShownAfterAChange_JustBelowTemplateXml_AndThereIsNoCloseButton()
    {
        using var folder = new TempFolder();
        string path = folder.File("template.xml");
        File.Copy(Samples.File(Samples.Template), path);

        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(path, Samples.File(Samples.Csv));
            window.Pump();
            Assert.False(form.SaveButton.Visible); // just opened: nothing to save
            Rectangle preview = form.Preview.RectangleToScreen(form.Preview.ClientRectangle);

            form.Editor.AppendText(" ");
            window.Pump();
            Assert.True(form.SaveButton.Visible);
            Assert.True(Top(form.SaveButton) >= Bottom(form.XmlViewButton), "below Template (xml) (Daan, 2026-11-21)");
            Assert.Equal(RightEdge(form.XmlViewButton), RightEdge(form.SaveButton)); // right below it, against the panel's edge
            Assert.True(form.SaveButton.Width >= form.XmlViewButton.Width);
            Assert.True(Bottom(form.SaveButton) <= Top(form.Editor.Parent!), "above the editor, not over it");
            Assert.Equal(preview, form.Preview.RectangleToScreen(form.Preview.ClientRectangle)); // the right panel does not move

            Assert.True(form.SaveFile());
            window.Pump();
            Assert.False(form.SaveButton.Visible); // saved: nothing to save any more

            Assert.DoesNotContain(AllButtons(form), button => button.Text == "Sluiten" || button.Text == "Close");
        });
    }

    private static IEnumerable<Button> AllButtons(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Button button)
            {
                yield return button;
            }

            foreach (Button inner in AllButtons(child))
            {
                yield return inner;
            }
        }
    }

    [Fact]
    public void TheTiles_ShowWhereTheShownLabelSits_AndItsSheet_AndFollowTheArrows()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.Equal(Strings.NoValue, form.PositionTile.Value); // nothing open
            Assert.Equal(Strings.NoValue, form.SheetTile.Value);
            Assert.Equal(Strings.PositionTile, form.PositionTile.Caption);
            Assert.Equal(Strings.SheetTile, form.SheetTile.Caption);

            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();
            LabelItem first = form.Preview.Labels!.Labels[0];
            Assert.Equal(string.Format(Strings.PositionFormat, "500", "1000"), form.PositionTile.Value);
            Assert.Equal(Strings.Millimetres, form.PositionTile.Line1);
            Assert.Equal(Strings.LabelDetails(first.Id, "0"), form.PositionTile.Line2);
            Assert.Equal(string.Format(Strings.SheetSizeFormat, "2800", "2100"), form.SheetTile.Value);

            form.NextButton.PerformClick();
            Assert.Equal(string.Format(Strings.PositionFormat, "650", "300"), form.PositionTile.Value);
            Assert.Contains(form.Preview.Labels.Labels[1].Id, form.PositionTile.Line2);

            // The tiles are as high as their text needs, and the same for both.
            Assert.Equal(form.PositionTile.Height, form.SheetTile.Height);
            Assert.True(form.PositionTile.Height >= form.PositionTile.HeightNeeded());
        });
    }

    [Fact]
    public void TheSheetMap_IsLeftOfTheLabel_WhenThePreviewIsWiderThanHigh_OtherwiseBelowIt()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            LabelPreview preview = window.Form.Preview;

            var (label, map) = preview.SplitLabelAndMap(new Rectangle(0, 0, 600, 300), drawMap: true);
            Assert.True(map.Right < label.Left, "the map is left of the label");
            Assert.InRange(map.Width / (double)(map.Width + label.Width), 0.38, 0.42); // 40 % of the width
            Assert.Equal(map.Height, label.Height);

            (label, map) = preview.SplitLabelAndMap(new Rectangle(0, 0, 300, 600), drawMap: true);
            Assert.True(map.Top > label.Bottom, "a narrow preview: the map below the label");

            (label, map) = preview.SplitLabelAndMap(new Rectangle(0, 0, 600, 300), drawMap: false);
            Assert.Equal(new Rectangle(0, 0, 600, 300), label); // no map (the board is shown): all for the label
            Assert.True(map.IsEmpty);
        });
    }

    [Fact]
    public void TheVersion_IsShownAsInTheProjectFile_WithoutTheBuildsCommitNumber()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            Assert.Equal(Strings.AppVersion(AppInfo.Version), window.Form.VersionLabel.Text);
            Assert.DoesNotContain("+", AppInfo.Version); // "2.0.0-alpha", not "2.0.0-alpha+3f2a…"
            Assert.Matches(@"^\d+\.\d+\.\d+(-[A-Za-z0-9.]+)?$", AppInfo.Version);
        });
    }

    [Fact]
    public void ThePrintButton_IsAtTheBottomRight_WithItsIconAndText_AndOnlyWhenThereIsALabel()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.False(form.PrintButton.Visible); // nothing to print yet

            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            Assert.True(form.PrintButton.Visible && form.NextButton.Visible);
            Assert.True(Top(form.PrintButton) >= Bottom(form.Tiles), "in the bottom bar, below the panels");
            Assert.True(form.PrintButton.Height >= form.LogicalToDeviceUnits(56), "big enough for a finger");
            Assert.InRange(Top(form.PrintButton) + form.PrintButton.Height / 2 - (Top(form.NextButton) + form.NextButton.Height / 2), -1, 1);
            Assert.True(Left(form.PrintButton) > RightEdge(form.NextButton), "print is right of the arrows");
            Assert.Equal(Strings.PrintButtonText, form.PrintButton.Text);
            Assert.Null(form.PrintButton.Image); // the printer of the mockup is drawn in code, not a picture
            Assert.False(form.PrintButton.TabStop); // Tab only goes through the four fields
            Assert.True(form.LabelCounter.Font.Bold, "the counter is bold");
        });
    }

    [Fact]
    public void TheBottomBar_FollowsTheWindowsWidth_PrintStaysAtTheRight_TheArrowsAtTheEndOfTheBand()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();
            Size disc = form.NextButton.Size;

            foreach (int wider in new[] { 0, 400, 1200 })
            {
                form.Width += wider;
                window.Pump();

                int pad = form.LogicalToDeviceUnits(24);
                Assert.Equal(RightEdge(form.BottomBar) - pad, RightEdge(form.PrintButton));
                Assert.Equal(form.PrintButton.Left - pad, form.BottomBar.BandRight);
                // "next" in the middle of the band's round end
                Assert.InRange(form.NextButton.Left + form.NextButton.Width / 2 - (form.BottomBar.BandRight - form.BottomBar.Height / 2), -1, 1);
                Assert.Equal(disc, form.NextButton.Size); // the round buttons keep their size
            }
        });
    }

    [Fact]
    public void ThePrintButton_IsThereForASingleLabelToo_ButNotForBrokenXml()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), null); // a template without csv: one label
            window.Pump();

            Assert.False(form.NextButton.Visible);
            Assert.True(form.PrintButton.Visible);

            form.Editor.Text = "<DSSPrintJob><PrintLabel"; // broken xml
            form.OpenCsvFile(null); // refreshes the preview at once
            window.Pump();

            Assert.False(form.PrintButton.Visible);
        });
    }

    [Fact]
    public void DraggingTheColumnDivider_WidensOneColumn_AndTheShareIsSavedWhenReleased()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Control editorFrame = form.Editor.Parent!;
            ColumnSplitter divider = form.ColumnDivider;
            int saves = window.SaveCount;

            // A drag with the mouse to 58% of the width (the events a real mouse gives, in the divider's coordinates).
            int target = (int)(form.Workspace.Width * 0.58);
            Mouse(divider, "OnMouseDown", 20);
            Mouse(divider, "OnMouseMove", target - divider.Left);
            Mouse(divider, "OnMouseUp", target - divider.Left);
            window.Pump();

            Assert.True(editorFrame.Width > form.Preview.Width + 50, $"editor {editorFrame.Width}, preview {form.Preview.Width}");
            Assert.InRange(Math.Abs(divider.Left + divider.Width / 2 - target), 0, 3); // the line is where the mouse is
            Assert.InRange(window.Settings.EditorColumnShare, 0.55, 0.62);
            Assert.True(window.SaveCount > saves, "saved when the mouse button was released");

            // A double-click: two equal halves again.
            Mouse(divider, "OnMouseDoubleClick", 20);
            window.Pump();
            Assert.InRange(Math.Abs(editorFrame.Width - form.Preview.Width), 0, 1);
            Assert.Equal(AppSettings.DefaultEditorColumnShare, window.Settings.EditorColumnShare);
        });
    }

    [Fact]
    public void TheColumnDivider_CannotSqueezeAColumnBelowItsMinimumWidth()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv)); // the view buttons and the arrows are shown
            form.Editor.AppendText(" "); // a change: "Save template" is shown too
            window.Pump();

            form.DragColumnDivider(0); // all the way to the left
            window.Pump();
            Control editorFrame = form.Editor.Parent!;
            Assert.True(form.XmlViewButton.Visible && form.SaveButton.Visible);
            Assert.True(RightEdge(form.XmlViewButton) <= RightEdge(editorFrame), "the buttons still fit above the editor");
            Assert.True(form.SaveButton.PointToScreen(Point.Empty).X >= editorFrame.PointToScreen(Point.Empty).X, "save still fits");

            form.DragColumnDivider(form.Workspace.Width); // all the way to the right
            window.Pump();
            Assert.True(form.Preview.Width >= 330, "preview " + form.Preview.Width);
        });
    }

    [Fact]
    public void TheSheetRotationOfSettingsJson_TurnsTheSheetMap()
    {
        Sta.Run(() =>
        {
            using var window = new Window(new AppSettings { SheetRotationInViewer = 270 });
            Assert.Equal(270, window.Form.Preview.SheetRotation);
        });
    }

    [Fact]
    public void ThePrintButtonsText_SaysWhetherADialogFollows()
    {
        // (The print button itself is never pressed here: without the dialog it would really print on the default printer.)
        Sta.Run(() =>
        {
            using (var window = new Window(new AppSettings()))
            {
                Assert.Equal(Strings.PrintTooltip, window.Form.PrintButton.AccessibleName); // "Print label(s)… (Ctrl+P)"
            }

            using (var window = new Window(new AppSettings { HideWindowsPrintDialog = true }))
            {
                Assert.Equal(Strings.PrintTooltipWithoutDialog, window.Form.PrintButton.AccessibleName); // "Print this label (Ctrl+P)"
            }
        });
    }

    [Fact]
    public void TheColumnShareOfTheLastTime_IsUsedAtTheStart_WithinTheMinimumWidths()
    {
        Sta.Run(() =>
        {
            using (var window = new Window(new AppSettings { EditorColumnShare = 0.58 }))
            {
                int editor = window.Form.Editor.Parent!.Width, preview = window.Form.Preview.Width;
                Assert.InRange(editor / (double)(editor + preview), 0.56, 0.60);
            }

            // A share from a wider (maximised) window that would squeeze the preview in the standard window: the minimum
            // wins on screen, but the share the user chose stays remembered, also after opening files (which saves the
            // settings), and comes back when the window is big enough again.
            using (var window = new Window(new AppSettings { EditorColumnShare = 0.8 }))
            {
                MainForm form = window.Form;
                Assert.True(form.Preview.Width >= 330, "preview " + form.Preview.Width);

                form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
                window.Pump();
                Assert.Equal(0.8, window.Settings.EditorColumnShare);

                form.Width = 2400; // room for 80 / 20 now
                window.Pump();
                int editor = form.Editor.Parent!.Width, preview = form.Preview.Width;
                Assert.InRange(editor / (double)(editor + preview), 0.78, 0.82);
            }
        });
    }

    // ---------------------------------------------------------------- the place of the window

    [Fact]
    public void TheWindow_OpensWhereItWasClosed_WithTheSameSize()
    {
        Sta.Run(() =>
        {
            Rectangle area = Screen.PrimaryScreen!.WorkingArea;
            var settings = new AppSettings { WindowLeft = area.Left + 40, WindowTop = area.Top + 30, WindowWidth = 1000, WindowHeight = 680 };
            using var window = new Window(settings);
            MainForm form = window.Form;

            Assert.Equal(new Point(area.Left + 40, area.Top + 30), form.Location);
            Assert.Equal(new Size(1000, 680), form.Size); // the test process runs at 100%
            Assert.Equal(FormWindowState.Normal, form.WindowState);

            // Moved and made bigger, then closed: remembered for the next start.
            form.Bounds = new Rectangle(area.Left + 60, area.Top + 50, 1000, 700);
            int saves = window.SaveCount;
            form.Close();

            Assert.True(window.SaveCount > saves);
            Assert.Equal((area.Left + 60, area.Top + 50, 1000, 700, false),
                (settings.WindowLeft, settings.WindowTop, settings.WindowWidth, settings.WindowHeight, settings.WindowMaximized));
        });
    }

    [Fact]
    public void AMaximisedWindow_OpensMaximisedAgain_AndRemembersItsNormalSize()
    {
        Sta.Run(() =>
        {
            Rectangle area = Screen.PrimaryScreen!.WorkingArea;
            var settings = new AppSettings { WindowLeft = area.Left + 40, WindowTop = area.Top + 30, WindowWidth = 1000, WindowHeight = 680, WindowMaximized = true };
            using var window = new Window(settings);

            Assert.Equal(FormWindowState.Maximized, window.Form.WindowState);

            window.Form.Close();

            Assert.True(settings.WindowMaximized);
            Assert.Equal((1000, 680), (settings.WindowWidth, settings.WindowHeight)); // the size to go back to
        });
    }

    [Fact]
    public void APlaceThatIsOnNoScreen_OrNoPlaceAtAll_GivesTheStandardWindowInTheMiddleOfTheScreen()
    {
        Sta.Run(() =>
        {
            using var standard = new Window(); // never saved: the standard size
            Size standardSize = standard.Form.Size;
            Rectangle area = Screen.FromControl(standard.Form).WorkingArea;
            Assert.True(area.Contains(standard.Form.Bounds));

            // A monitor that was removed: the saved place is far outside every screen.
            using var gone = new Window(new AppSettings { WindowLeft = -50000, WindowTop = -50000, WindowWidth = 1200, WindowHeight = 800 });
            Assert.Equal(standardSize, gone.Form.Size);
            Assert.True(Screen.FromControl(gone.Form).WorkingArea.Contains(gone.Form.Bounds));
        });
    }

    // ---------------------------------------------------------------- the view of the left column, the top of row 1

    [Fact]
    public void ChoosingALabelCsv_ShowsItsTable_ClosingItShowsTheTemplateText()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenXmlFile(Samples.File(Samples.Template));
            Assert.False(form.ShowingCsv); // a template without data: its text

            form.OpenCsvFile(Samples.File(Samples.Csv));
            Assert.True(form.ShowingCsv);

            form.OpenCsvFile(null);
            Assert.False(form.ShowingCsv);
        });
    }

    [Fact]
    public void TheFilesPanel_HasItsTitleAboveTheFileLines_AndTheTwoBigPanelsBelowIt()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            // The description of the two kinds of files is the hover text of the "?" icons (Daan, 2026-10-26).
            Assert.Equal(Strings.FilesCaption, form.FilesCaption.Text);
            Assert.True(Top(form.FilesCaption) > Bottom(form.TopBar));
            Assert.InRange(Top(form.XmlFileBox.Parent!) - Bottom(form.FilesCaption), 0, form.LogicalToDeviceUnits(20));
            Assert.True(Top(form.EditorCaption) > Bottom(form.CsvBrowseButton), "room between the file lines and the panels");
            Assert.Equal(Left(form.FilesCaption), Left(form.XmlFoldButton)); // the title and the lines start in line
        });
    }

    [Theory]
    [InlineData(AppLanguage.Dutch)]
    [InlineData(AppLanguage.French)]
    [InlineData(AppLanguage.English)]
    public void TheOneHowToIcon_SitsRightAfterTheTitle_AndShowsTheDescription(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;
            PictureBox icon = form.FilesHowToPicture;

            // Daan, 2026-11-21: one "?" after "Label files" instead of one before each file line.
            Assert.NotNull(icon.Image);
            Assert.Equal(icon.Width, icon.Height); // a round icon, not stretched
            Assert.InRange(Left(icon) - RightEdge(form.FilesCaption), 0, form.LogicalToDeviceUnits(12)); // right after the title
            Assert.InRange(Top(icon) + icon.Height / 2 - (Top(form.FilesCaption) + form.FilesCaption.Height / 2), -1, 1); // in line with it
            Assert.Equal(MainForm.WrapForToolTip(Strings.FileDescription, MainForm.HowToLineLength), form.HowToToolTip.GetToolTip(icon));

            // The fold buttons start at the left edge of the panel's content, like the status line below them.
            Assert.Equal(Left(form.XmlFoldButton), Left(form.CsvFoldButton));
        });
    }

    [Fact]
    public void AFoldedFileLine_ShowsOnlyItsCaptionAndPath_GivesThePanelsBelowMoreHeight_AndIsRemembered()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();
            int previewHeight = form.Preview.Height;
            int saves = window.SaveCount;

            form.XmlFoldButton.PerformClick(); // Daan, 2026-11-21: the "v" folds the line up
            window.Pump();
            Assert.True(form.XmlFoldButton.Folded);
            Assert.False(form.XmlFileBox.Visible || form.XmlBrowseButton.Visible);
            Assert.True(form.XmlFoldedName.Visible);
            Assert.Equal(form.XmlFileBox.Text, form.XmlFoldedName.Text);
            Assert.True(form.XmlFileCaption.Visible, "the caption stays, to know what to unfold");
            Assert.True(form.Preview.Height > previewHeight, "the panels below get the height");
            Assert.True(window.Settings.XmlFileLineFolded);
            Assert.True(window.SaveCount > saves);

            form.CsvFoldButton.PerformClick();
            window.Pump();
            int bothFolded = form.Preview.Height;
            Assert.True(bothFolded >= previewHeight + form.LogicalToDeviceUnits(30), $"{bothFolded} after {previewHeight}");
            Assert.True(window.Settings.CsvFileLineFolded);

            form.XmlFoldButton.PerformClick(); // open again
            form.CsvFoldButton.PerformClick();
            window.Pump();
            Assert.True(form.XmlFileBox.Visible && form.XmlBrowseButton.Visible);
            Assert.Equal(previewHeight, form.Preview.Height);
            Assert.False(window.Settings.XmlFileLineFolded || window.Settings.CsvFileLineFolded);
        });

        Sta.Run(() =>
        {
            using var window = new Window(new AppSettings { CsvFileLineFolded = true }); // folded the last time
            Assert.True(window.Form.CsvFoldButton.Folded);
            Assert.False(window.Form.CsvFileBox.Visible);
            Assert.False(window.Form.XmlFoldButton.Folded);
        });
    }

    [Fact]
    public void TheCsvTableTitle_EndsWithTheCsvsFileName_InBold()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();

            // Daan, 2026-11-21: "Données : label-example.csv | 3 étiquette(s)", the file name bold.
            Assert.Equal(Strings.CsvCaption, form.EditorCaption.Text);
            Assert.Equal(Samples.Csv, form.EditorCaption.Emphasis);
            Assert.Equal(Strings.LabelCount(3), form.EditorCaption.Suffix);

            form.XmlViewButton.PerformClick(); // the template text: no file name, no count
            Assert.Equal(Strings.EditorCaption, form.EditorCaption.Text);
            Assert.Equal("", form.EditorCaption.Emphasis);
            Assert.Equal("", form.EditorCaption.Suffix);

            form.BoardButton.PerformClick(); // the board: the same title as the table
            Assert.Equal(Strings.CsvCaption, form.EditorCaption.Text);
            Assert.Equal(Samples.Csv, form.EditorCaption.Emphasis);
            Assert.Equal(Strings.LabelCount(3), form.EditorCaption.Suffix);
        });

        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null); // a label-xml on the board: its own name
            form.BoardButton.PerformClick();
            Assert.Equal(Samples.LabelXml, form.EditorCaption.Emphasis);
            Assert.Equal(Strings.LabelCount(2), form.EditorCaption.Suffix);
        });
    }

    [Fact]
    public void TheHowToText_IsCutIntoShortLines_WithoutLosingAWord()
    {
        foreach (AppLanguage language in Enum.GetValues<AppLanguage>())
        {
            using var scope = new LanguageScope(language);
            string text = Strings.FileDescription;
            string wrapped = MainForm.WrapForToolTip(text, MainForm.HowToLineLength);

            Assert.All(wrapped.Split('\n'), line => Assert.True(line.Length <= MainForm.HowToLineLength, line));
            Assert.Contains("\n\n", wrapped); // an empty line between the two paragraphs
            Assert.Equal(
                text.Split((char[])[' ', '\n'], StringSplitOptions.RemoveEmptyEntries),
                wrapped.Split((char[])[' ', '\n'], StringSplitOptions.RemoveEmptyEntries));
        }
    }

    /// <summary>Gives a control a mouse event as Windows would (left button), at x in its own coordinates.</summary>
    private static void Mouse(Control control, string handler, int x)
    {
        var e = new MouseEventArgs(MouseButtons.Left, handler == "OnMouseDoubleClick" ? 2 : 1, x, 10, 0);
        typeof(Control).GetMethod(handler, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(control, new object[] { e });
    }

    [Fact]
    public void TheWorkspace_GrowsWithTheWindow_TheFileRowDoesNot()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            int previewHeight = form.Preview.Height;
            int fileBoxTop = Top(form.XmlFileBox) - Top(form);

            form.Height += 100;
            window.Pump();

            Assert.Equal(previewHeight + 100, form.Preview.Height);
            Assert.Equal(fileBoxTop, Top(form.XmlFileBox) - Top(form));
        });
    }

    [Fact]
    public void ThePreview_DoesNotJump_WhenTheButtonsBelowItAppear()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.False(form.NextButton.Visible || form.PrintButton.Visible);
            Rectangle before = form.Preview.RectangleToScreen(form.Preview.ClientRectangle);

            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            Assert.True(form.NextButton.Visible && form.PrintButton.Visible);
            Assert.Equal(before, form.Preview.RectangleToScreen(form.Preview.ClientRectangle));
        });
    }

    [Fact]
    public void TheTitleTab_ShowsTheNameAndTheSubtitle_WholeInTheStandardWindow_AndTheTopBarHoldsTheRest()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            Assert.Equal(Strings.AppName, form.TitleTab.Text);
            Assert.Equal(Strings.BannerSubtitle, form.TitleTab.Subtitle);
            Assert.True(form.TitleTab.WidthNeeded() <= form.TitleTab.Width, "nothing cut off in the standard window");
            Assert.Equal(form.TitleTab.WidthNeeded(), form.TitleTab.TabBounds().Width);

            // Right of it, in this order: the language, the credit and the version, the logo, the user guide.
            foreach (Control control in new Control[] { form.LanguageBox, form.VersionLabel, form.LogoPicture, form.UserGuideButton })
            {
                Assert.True(form.TopBar.Contains(control));
                Assert.True(Left(control) >= Left(form.TitleTab) + form.TitleTab.WidthNeeded(), "right of the white tab");
            }

            Assert.True(RightEdge(form.LanguageBox) < Left(form.VersionLabel));
            Assert.True(RightEdge(form.VersionLabel) < Left(form.LogoPicture));
            Assert.True(RightEdge(form.LogoPicture) < Left(form.UserGuideButton));
            Assert.Equal(RightEdge(form.CreditLabel), RightEdge(form.VersionLabel)); // two lines, against the right
            Assert.True(RightEdge(form.UserGuideButton) <= RightEdge(form.TopBar));
        });
    }

    [Theory]
    [InlineData(AppLanguage.Dutch)]
    [InlineData(AppLanguage.French)]
    [InlineData(AppLanguage.English)]
    public void EveryButtonText_FitsItsButton(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;

            int needed = TextRenderer.MeasureText(form.SaveButton.Text, form.SaveButton.Font).Width;
            Assert.True(needed + 8 <= form.SaveButton.Width, $"'{form.SaveButton.Text}' needs {needed} in a button of {form.SaveButton.Width}");
            foreach (RoundedButton browse in new[] { form.XmlBrowseButton, form.CsvBrowseButton })
            {
                Assert.True(browse.WidthNeededForIconAndText() <= browse.Width, $"'{browse.Text}' and its folder in {browse.Width}");
            }

            TabButton xml = form.XmlViewButton;
            Assert.True(xml.WidthNeeded() <= xml.Width, $"'{xml.Text}' (bold) needs {xml.WidthNeeded()} in a tab of {xml.Width}");

            Assert.True(form.PrintButton.WidthNeeded() <= form.PrintButton.Width, $"'{form.PrintButton.Text}' in {form.PrintButton.Width}");
        });
    }

    [Fact]
    public void ChoosingAnotherLanguage_ChangesTheTexts_AndSavesTheSetting()
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(AppLanguage.Dutch);
            using var window = new Window();
            Assert.Equal("Template opslaan", window.Form.SaveButton.Text);

            window.Form.LanguageBox.SelectedIndex = (int)AppLanguage.English;

            Assert.Equal("Save template", window.Form.SaveButton.Text);
            Assert.Equal(MainForm.WrapForToolTip(Strings.FileDescription, MainForm.HowToLineLength),
                window.Form.HowToToolTip.GetToolTip(window.Form.FilesHowToPicture));
            Assert.Equal(Strings.PreviewCaption, window.Form.PreviewCaption.Text);
            Assert.Equal("en", window.Settings.Language);
            Assert.True(window.SaveCount > 0);
        });
    }

    // ---------------------------------------------------------------- the label size

    [Theory]
    [InlineData(AppLanguage.Dutch)]
    [InlineData(AppLanguage.French)]
    [InlineData(AppLanguage.English)]
    public void TheLabelSize_SitsRightOfThePreviewTitle_AndTheStatusLineHasTheWholeWidth(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv)); // a long status line
            window.Pump();

            Control sizeFrame = form.LabelWidthBox.Parent!;
            Assert.Equal(RightEdge(form.Preview) - form.LogicalToDeviceUnits(12), RightEdge(sizeFrame));
            int sizeMiddle = Top(sizeFrame) + sizeFrame.Height / 2, tabMiddle = Top(form.XmlViewButton) + form.XmlViewButton.Height / 2;
            Assert.InRange(sizeMiddle - tabMiddle, -1, 1); // in the title row, in line with the tabs of the left panel
            Assert.True(RightEdge(form.PreviewCaption) <= form.LabelSizePanel.PointToScreen(Point.Empty).X, "the title runs into the label size");
        });
    }

    [Fact]
    public void TheLabelSize_StartsAt100By100_AndAValidChangeIsUsedAndSaved()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.Equal("100", form.LabelLengthBox.Text);
            Assert.Equal("100", form.LabelWidthBox.Text);
            Assert.Equal(new SizeF(100, 100), form.Preview.LabelSize);
            int saves = window.SaveCount;

            form.LabelLengthBox.Text = "80";
            form.LabelWidthBox.Text = "50,5"; // a comma is forgiven (Belgian keyboard habit)

            Assert.Equal(new SizeF(80, 50.5f), form.Preview.LabelSize);
            Assert.Equal(80, window.Settings.LabelLength);
            Assert.Equal(50.5, window.Settings.LabelWidth);
            Assert.True(window.SaveCount > saves);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("1500")]
    public void AnInvalidLabelSize_TurnsRed_AndTheLastValidSizeStays(string typed)
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            form.LabelLengthBox.Text = typed;

            Assert.Equal(Theme.Error, form.LabelLengthBox.ForeColor);
            Assert.Equal(100, window.Settings.LabelLength);
            Assert.Equal(new SizeF(100, 100), form.Preview.LabelSize);

            form.LabelLengthBox.Text = "70";
            Assert.Equal(Theme.Text, form.LabelLengthBox.ForeColor);
        });
    }

    [Fact]
    public void ASmallerLabel_ReportsTheObjectsOutsideIt()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Form.Preview.Refresh(); // paint now
            Assert.Equal(0, form.Preview.ObjectsOutside);

            form.LabelLengthBox.Text = "60";
            form.Preview.Refresh();

            Assert.True(form.Preview.ObjectsOutside > 0);
        });
    }

    // ---------------------------------------------------------------- the two kinds of files and the arrows

    [Fact]
    public void ALabelXml_ShowsItsFirstLabel_AndTheArrowsGoThroughAllLabels()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            Assert.True(form.OpenLabelFiles(Samples.File(Samples.LabelXml), null));

            Assert.Equal(2, form.Preview.Count);
            Assert.Equal("5436280", form.Preview.Labels!.Labels[form.Preview.Index].Id);
            Assert.True(form.PreviousButton.Visible && form.NextButton.Visible);
            Assert.Equal("1 / 2", form.LabelCounter.Text);
            Assert.False(form.PreviousButton.Enabled); // at the first label
            Assert.True(form.NextButton.Enabled);

            form.NextButton.PerformClick();

            Assert.Equal(1, form.Preview.Index);
            Assert.Equal("2 / 2", form.LabelCounter.Text);
            Assert.True(form.PreviousButton.Enabled);
            Assert.False(form.NextButton.Enabled);   // at the last label

            form.ShowLabel(99); // never past the end
            Assert.Equal(1, form.Preview.Index);
        });
    }

    [Fact]
    public void ATemplateWithItsCsv_ShowsOneLabelPerCsvLine_AndRemembersBothFiles()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            string template = Samples.File(Samples.Template), csv = Samples.File(Samples.Csv);

            Assert.True(form.OpenLabelFiles(template, csv));

            Assert.Equal(3, form.Preview.Count);
            Assert.Equal("1 / 3", form.LabelCounter.Text);
            Assert.Equal(template, form.XmlFileBox.Text);   // the editor holds the template
            Assert.Equal(csv, form.CsvFileBox.Text);
            Assert.Equal(Strings.LabelCount(3), form.EditorCaption.Suffix); // "Données : label-example.csv | 3 étiquette(s)"
            Assert.Equal(template, window.Settings.LastFile);
            Assert.Equal(csv, window.Settings.LastCsvFile);
            Assert.Contains("$ORDERNO$", form.Editor.Text);
        });
    }

    [Fact]
    public void ATemplateWithoutCsv_IsOneLabel_WithoutArrows()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            form.OpenLabelFiles(Samples.File(Samples.Template), null);

            Assert.Equal(1, form.Preview.Count);
            Assert.False(form.NextButton.Visible || form.PreviousButton.Visible || form.LabelCounter.Visible);
            Assert.Equal("", window.Settings.LastCsvFile);
        });
    }

    [Fact]
    public void Typing_KeepsTheSameLabel_AndThePreviewFollowsTheText()
    {
        using var folder = new TempFolder();
        string path = folder.File("labels.xml");
        File.Copy(Samples.File(Samples.LabelXml), path);

        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(path, null);
            form.ShowLabel(1);

            form.Editor.Text = form.Editor.Text.Replace("ID=\"5436281\"", "ID=\"777\"");
            Thread.Sleep(450); // the preview follows 300 ms after the last change
            window.Pump();

            Assert.Equal(1, form.Preview.Index);
            Assert.Equal("777", form.Preview.Labels!.Labels[1].Id);
        });
    }

    [Fact]
    public void BrokenXml_IsShownAsAnError_NotAsACrash()
    {
        using var folder = new TempFolder();
        string path = folder.File("broken.xml");
        File.WriteAllText(path, "<DSSPrintJob><PrintLabel></DSSPrintJob>");

        Sta.Run(() =>
        {
            using var window = new Window();

            Assert.True(window.Form.OpenLabelFiles(path, null));
            window.Pump(); // paints the preview with the error

            Assert.NotNull(window.Form.Preview.Labels!.Error);
            Assert.False(window.Form.NextButton.Visible);
        });
    }

    // ---------------------------------------------------------------- opening and saving

    [Fact]
    public void Editing_MarksTheFileAsChanged_AndSave_WritesItBack()
    {
        using var folder = new TempFolder();
        string path = folder.File("label.xml");
        File.WriteAllText(path, "old\nline");

        Sta.Run(() =>
        {
            using var window = new Window();
            window.Form.OpenLabelFiles(path, null);

            window.Form.Editor.Text = "new\r\nline";
            Assert.True(window.Form.IsModified);
            Assert.EndsWith("*", window.Form.Text);

            Assert.True(window.Form.SaveFile());

            Assert.False(window.Form.IsModified);
            Assert.DoesNotContain("*", window.Form.Text);
        });

        Assert.Equal("new\nline", File.ReadAllText(path)); // the file had LF line endings: it keeps them
    }

    [Fact]
    public void AFileThatCannotBeRead_IsReportedQuietly_WhenAsked_AndNothingChanges()
    {
        Sta.Run(() =>
        {
            using var window = new Window();

            Assert.False(window.Form.OpenLabelFiles(@"C:\does\not\exist\label.xml", null, quiet: true));
            Assert.False(window.Form.OpenLabelFiles(Samples.File(Samples.Template), @"C:\does\not\exist\labels.csv", quiet: true));

            Assert.Null(window.Form.CurrentFile);
            Assert.Null(window.Form.CurrentCsvFile);
            Assert.Equal("", window.Form.Editor.Text);
        });
    }
}
