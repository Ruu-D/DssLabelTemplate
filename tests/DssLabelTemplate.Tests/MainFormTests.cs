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
    private static int Top(Control control) => control.PointToScreen(Point.Empty).Y;

    // ---------------------------------------------------------------- the board view

    [Fact]
    public void TheBoardButton_SitsBelowTheLeftColumn_AgainstTheLine_LeftOfTheCounter_AsBigAsTheArrows()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.False(form.BoardButton.Visible); // nothing open: no sheet

            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();

            Assert.True(form.BoardButton.Visible);
            Assert.Equal(form.PreviousButton.Size, form.BoardButton.Size);
            Assert.Equal(Top(form.PreviousButton), Top(form.BoardButton));
            Assert.Equal(RightEdge(form.EditorArea), RightEdge(form.BoardButton)); // the right edge of the left column
            Assert.True(RightEdge(form.BoardButton) < form.LabelCounter.PointToScreen(Point.Empty).X, "left of the counter");
            Assert.True(RightEdge(form.LogoPicture) < form.BoardButton.PointToScreen(Point.Empty).X, "right of the logo");
            Assert.Equal(Top(form.LogoPicture) + form.LogoPicture.Height, Top(form.BoardButton) + form.BoardButton.Height);
            Assert.NotNull(form.BoardButton.Image);
            Assert.False(form.BoardButton.TabStop);

            // The csv button (2026-11-08): just left of the board button, as big, an icon without text.
            Assert.True(form.CsvViewButton.Visible);
            Assert.Equal(form.BoardButton.Size, form.CsvViewButton.Size);
            Assert.Equal(Top(form.BoardButton), Top(form.CsvViewButton));
            Assert.True(RightEdge(form.CsvViewButton) < form.BoardButton.PointToScreen(Point.Empty).X, "left of the board button");
            Assert.True(RightEdge(form.LogoPicture) < form.CsvViewButton.PointToScreen(Point.Empty).X, "right of the logo");
            Assert.NotNull(form.CsvViewButton.Image);
            Assert.Equal("", form.CsvViewButton.Text);
            Assert.False(form.CsvViewButton.TabStop);
            Assert.True(form.EditorHeader.Contains(form.XmlViewButton), "Template (xml) stays above the column");
            Assert.False(form.EditorHeader.Contains(form.CsvViewButton));

            form.DragColumnDivider(0); // the left column as narrow as it can be: the logo and the buttons still fit
            window.Pump();
            Assert.True(RightEdge(form.LogoPicture) < form.CsvViewButton.PointToScreen(Point.Empty).X);
            Assert.True(RightEdge(form.CsvViewButton) < form.BoardButton.PointToScreen(Point.Empty).X);
            Assert.Equal(RightEdge(form.EditorArea), RightEdge(form.BoardButton));
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
            Assert.Equal(Theme.Brand, form.BoardButton.NormalFill);
            Assert.NotEqual(Theme.Brand, form.XmlViewButton.NormalFill);

            form.BoardButton.PerformClick(); // closed: the table, not the text that was shown before
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.True(form.ShowingCsv);
            Assert.Equal(Theme.Brand, form.CsvViewButton.NormalFill);

            form.BoardButton.PerformClick();
            form.CsvViewButton.PerformClick(); // the csv button closes the board too
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.True(form.CsvGrid.Parent!.Visible);
            Assert.NotEqual(Theme.Brand, form.BoardButton.NormalFill);

            form.CsvViewButton.PerformClick(); // again: the table stays (the template is chosen above the column)
            Assert.True(form.ShowingCsv);
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
                Assert.True(browse.Height > form.XmlViewButton.Height, "higher than a normal button");
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
    public void TheLabelCounter_IsOneAndAHalfTimesTheWindowsFont()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            Assert.InRange(window.Form.LabelCounter.Font.SizeInPoints / window.Form.Font.SizeInPoints, 1.45, 1.55);
            Assert.True(window.Form.LabelCounter.Font.Bold);
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
            Rectangle area = LabelPreview.PanelInside(form.Board);
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
            Color normalFill = form.BoardButton.NormalFill;

            form.BoardButton.PerformClick();
            window.Pump();

            Assert.True(form.ShowingBoard);
            Assert.True(form.Board.Visible);
            Assert.False(form.CsvGrid.Parent!.Visible);
            Assert.False(form.Editor.Parent!.Visible);
            Assert.Equal(Strings.BoardCaption, form.EditorCaption.Text);
            Assert.Equal(Theme.Brand, form.BoardButton.NormalFill); // "pressed": the same colour as the chosen view button
            Assert.NotEqual(Theme.Brand, form.CsvViewButton.NormalFill);
            Assert.Equal(form.EditorArea.Size, form.Board.Size); // the whole left column
            Assert.False(form.Preview.ShowSheetMap); // the small map below the label is gone: the label gets its room

            form.ShowLabel(1); // the board follows the preview, and opening the next label does not close it
            window.Pump();
            Assert.True(form.ShowingBoard);

            form.BoardButton.PerformClick(); // again: back to the table it showed before
            window.Pump();
            Assert.False(form.ShowingBoard);
            Assert.False(form.Board.Visible);
            Assert.True(form.CsvGrid.Parent!.Visible);
            Assert.True(form.Preview.ShowSheetMap); // the small map is back
            Assert.Equal(normalFill, form.BoardButton.NormalFill);
            Assert.Equal(Theme.Brand, form.CsvViewButton.NormalFill);

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
    public void BrowseClosePreviewLabelSizeAndDividers_EndAtTheSameRightEdge()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            int edge = RightEdge(form.XmlBrowseButton);
            Assert.Equal(edge, RightEdge(form.XmlBrowseButton));
            Assert.Equal(edge, RightEdge(form.CsvBrowseButton));
            Assert.Equal(edge, RightEdge(form.Preview));
            Assert.Equal(edge, RightEdge(form.LabelWidthBox.Parent!));
            foreach (Panel divider in form.Dividers)
            {
                Assert.Equal(edge, RightEdge(divider));
            }
        });
    }

    [Fact]
    public void TheEditorAndThePreview_HaveTheSameWidthAndHeight_AndTheBigButtonsAndLogoEndTheWindow()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Control editorFrame = form.Editor.Parent!;

            Assert.InRange(Math.Abs(editorFrame.Width - form.Preview.Width), 0, 1);
            Assert.Equal(Top(editorFrame), Top(form.Preview));
            Assert.Equal(editorFrame.Height, form.Preview.Height); // the bottoms are in line again
            Assert.True(form.Preview.Height > 200, "row 2 is the big row: " + form.Preview.Height);

            // Below them the last row of the window: the logo at the left, the big buttons below the preview, their
            // bottoms in line, with as much white room below them as at the left and right of the window.
            int navigationBottom = Top(form.PreviewNavigation) + form.PreviewNavigation.Height;
            Assert.True(Top(form.PreviewNavigation) > Top(form.Preview) + form.Preview.Height, "below the preview");
            Assert.Equal(navigationBottom, Top(form.LogoPicture) + form.LogoPicture.Height);
            int roomBelow = form.PointToScreen(new Point(0, form.ClientSize.Height)).Y - navigationBottom;
            int roomLeft = editorFrame.PointToScreen(Point.Empty).X - form.PointToScreen(Point.Empty).X;
            Assert.Equal(roomLeft, roomBelow);
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
    public void SaveTemplate_IsOnlyShownAfterAChange_LeftOfTheViewButtons_AndThereIsNoCloseButton()
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

            form.Editor.AppendText(" ");
            window.Pump();
            Assert.True(form.SaveButton.Visible);
            Assert.Equal(Top(form.XmlViewButton), Top(form.SaveButton));
            Assert.True(RightEdge(form.SaveButton) <= form.XmlViewButton.PointToScreen(Point.Empty).X, "left of Template (xml)");

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
    public void TheArrowsAndPrint_SitBelowThePreview_InTheMiddle_TwiceAsHighAsTheOtherButtons()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.False(form.PrintButton.Visible); // nothing to print yet

            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            Assert.True(form.PrintButton.Visible && form.NextButton.Visible);
            Assert.True(Top(form.NextButton) >= Top(form.Preview) + form.Preview.Height, "below the preview");
            Assert.Equal(2 * form.XmlViewButton.Height, form.NextButton.Height); // twice a normal button
            Assert.Equal(form.NextButton.Size, form.PreviousButton.Size);
            Assert.Equal(form.NextButton.Size, form.PrintButton.Size);
            Assert.Equal(Top(form.NextButton), Top(form.PrintButton));
            Assert.True(form.PrintButton.PointToScreen(Point.Empty).X > RightEdge(form.NextButton), "print is right of the arrow");

            // The counter against the preview's left edge, print against its right edge, the arrows in the middle.
            int previewLeft = form.Preview.PointToScreen(Point.Empty).X;
            Assert.Equal(previewLeft, form.LabelCounter.PointToScreen(Point.Empty).X);
            Assert.Equal(RightEdge(form.Preview), RightEdge(form.PrintButton));
            int previewMiddle = previewLeft + form.Preview.Width / 2;
            int arrowsMiddle = (form.PreviousButton.PointToScreen(Point.Empty).X + RightEdge(form.NextButton)) / 2;
            Assert.InRange(Math.Abs(previewMiddle - arrowsMiddle), 0, 3);
            Assert.True(RightEdge(form.LabelCounter) < form.PreviousButton.PointToScreen(Point.Empty).X, "the counter is left of the arrows");
            Assert.True(form.LabelCounter.Font.Bold, "the counter is bold");

            Assert.NotNull(form.PrintButton.Image);
            Assert.False(form.PrintButton.TabStop); // Tab only goes through the four fields
        });
    }

    [Fact]
    public void TheArrowsAndPrint_AreWiderThanBefore_AndGrowWithThePreview_ButNotTooMuch()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            int height = form.NextButton.Height;
            int standard = form.NextButton.Width;
            Assert.True(standard > 72, "wider than the 72 of 1.1.1: " + standard); // the standard window, at 100%

            form.Width += 400; // the preview column grows
            window.Pump();
            Assert.True(form.NextButton.Width > standard, $"{form.NextButton.Width} after {standard}");
            Assert.Equal(height, form.NextButton.Height); // the height stays

            form.Width += 2000; // a very wide window: not wider than the maximum
            window.Pump();
            Assert.Equal(120, form.NextButton.Width);
            foreach (Button button in new[] { form.PreviousButton, form.PrintButton })
            {
                Assert.Equal(form.NextButton.Size, button.Size);
            }

            form.DragColumnDivider(form.Workspace.Width); // the preview as narrow as it can be: everything still fits
            window.Pump();
            Assert.True(RightEdge(form.LabelCounter) < form.PreviousButton.PointToScreen(Point.Empty).X);
            Assert.True(RightEdge(form.NextButton) < form.PrintButton.PointToScreen(Point.Empty).X);
            Assert.Equal(RightEdge(form.Preview), RightEdge(form.PrintButton));
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
            var settings = new AppSettings { WindowLeft = area.Left + 40, WindowTop = area.Top + 30, WindowWidth = 900, WindowHeight = 640 };
            using var window = new Window(settings);
            MainForm form = window.Form;

            Assert.Equal(new Point(area.Left + 40, area.Top + 30), form.Location);
            Assert.Equal(new Size(900, 640), form.Size); // the test process runs at 100%
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
            var settings = new AppSettings { WindowLeft = area.Left + 40, WindowTop = area.Top + 30, WindowWidth = 900, WindowHeight = 640, WindowMaximized = true };
            using var window = new Window(settings);

            Assert.Equal(FormWindowState.Maximized, window.Form.WindowState);

            window.Form.Close();

            Assert.True(settings.WindowMaximized);
            Assert.Equal((900, 640), (settings.WindowWidth, settings.WindowHeight)); // the size to go back to
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
    public void Row1_StartsWithTheFileLines_AndOnlyTheLineBelowTheLanguageIsLeft()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            // The description is no longer a text above the path boxes (it is the hover text of the "?" icons), and the
            // divider between row 1 and row 2 is gone (Daan, 2026-10-26).
            Panel divider = Assert.Single(form.Dividers);
            int belowDivider = Top(divider) + divider.Height;
            Assert.InRange(Top(form.XmlFileBox.Parent!) - belowDivider, 0, 20);
            Assert.True(Top(form.EditorCaption) > Top(form.FileStatus) + form.FileStatus.Height, "room between the status line and row 2");
        });
    }

    [Theory]
    [InlineData(AppLanguage.Dutch)]
    [InlineData(AppLanguage.French)]
    [InlineData(AppLanguage.English)]
    public void TheHowToIcons_SitBeforeTheCaptions_AndShowTheDescription(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;

            foreach ((PictureBox icon, Label caption) in new[] { (form.XmlHowToPicture, form.XmlFileCaption), (form.CsvHowToPicture, form.CsvFileCaption) })
            {
                Assert.NotNull(icon.Image);
                Assert.Equal(form.UserGuidePicture.Size, icon.Size); // the same small icon size as the user guide
                Assert.True(RightEdge(icon) <= caption.PointToScreen(Point.Empty).X, "the icon comes before the caption");
                Assert.InRange(Top(icon) + icon.Height / 2 - (Top(caption) + caption.Height / 2), -1, 1); // in line with it

                string? tip = form.HowToToolTip.GetToolTip(icon);
                Assert.Equal(MainForm.WrapForToolTip(Strings.FileDescription, MainForm.HowToLineLength), tip);
            }

            // The icons start at the left edge of the window's content, like the status line below them.
            Assert.Equal(form.XmlHowToPicture.PointToScreen(Point.Empty).X, form.FileStatus.PointToScreen(Point.Empty).X);
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
    public void TheBanner_KeepsTheProportionsOfThePicture()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            PictureBox banner = window.Form.BannerPicture;

            Assert.Equal(new Size(1080, 168), banner.Image!.Size); // drawn at twice 540 x 84, like TsDssConverter's banner
            Assert.Equal(540.0 / 84.0, (double)banner.Width / banner.Height, 1);
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

            foreach (Button button in new Button[] { form.XmlBrowseButton, form.CsvBrowseButton, form.SaveButton, form.XmlViewButton, form.PreviousButton, form.NextButton })
            {
                int needed = TextRenderer.MeasureText(button.Text, button.Font).Width;
                Assert.True(needed + 8 <= button.Width, $"'{button.Text}' needs {needed} in a button of {button.Width}");
            }
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
                window.Form.HowToToolTip.GetToolTip(window.Form.CsvHowToPicture));
            Assert.Equal(Strings.StatusNothingOpen, window.Form.FileStatus.Text);
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
            Assert.Equal(RightEdge(form.Preview), RightEdge(sizeFrame));
            Assert.Equal(Top(form.XmlViewButton), Top(sizeFrame)); // in the title row, in line with the view button
            Assert.True(RightEdge(form.PreviewCaption) <= form.LabelSizePanel.PointToScreen(Point.Empty).X, "the title runs into the label size");
            Assert.Equal(form.XmlBrowseButton.Parent!.Width, form.FileStatus.Width);
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
            Assert.Equal(Theme.Navy, form.LabelLengthBox.ForeColor);
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
            Assert.Equal(Strings.StatusLabelXml(2), form.FileStatus.Text);

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
            Assert.Equal(Strings.StatusTemplateWithCsv(3), form.FileStatus.Text);
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
            Assert.Equal(Strings.StatusTemplateWithoutCsv, form.FileStatus.Text);
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
            Assert.Equal(Strings.StatusXmlError, window.Form.FileStatus.Text);
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
