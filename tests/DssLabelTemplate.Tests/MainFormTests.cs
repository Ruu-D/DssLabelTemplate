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
            Assert.Equal(2 * form.XmlBrowseButton.Height, form.NextButton.Height);
            Assert.Equal(form.NextButton.Size, form.PreviousButton.Size);
            Assert.Equal(form.NextButton.Size, form.PrintButton.Size);
            Assert.Equal(Top(form.NextButton), Top(form.PrintButton));
            Assert.True(form.PrintButton.PointToScreen(Point.Empty).X > RightEdge(form.NextButton), "print is right of the arrow");

            int previewMiddle = form.Preview.PointToScreen(Point.Empty).X + form.Preview.Width / 2;
            int buttonsMiddle = form.PreviewNavigation.PointToScreen(Point.Empty).X + form.PreviewNavigation.Width / 2;
            Assert.InRange(Math.Abs(previewMiddle - buttonsMiddle), 0, 1);

            Assert.NotNull(form.PrintButton.Image);
            Assert.False(form.PrintButton.TabStop); // Tab only goes through the four fields
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
            Assert.True(RightEdge(form.CsvViewButton) <= RightEdge(editorFrame), "the buttons still fit above the editor");
            Assert.True(form.SaveButton.PointToScreen(Point.Empty).X >= editorFrame.PointToScreen(Point.Empty).X, "save still fits");

            form.DragColumnDivider(form.Workspace.Width); // all the way to the right
            window.Pump();
            Assert.True(form.Preview.Width >= 330, "preview " + form.Preview.Width);
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
    public void Row1_StartsWithTheDescription_ThereIsNoBoldTitleAboveIt()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;

            // Between the divider under the language and the description there is nothing else.
            Panel firstDivider = form.Dividers[0];
            int belowDivider = Top(firstDivider) + firstDivider.Height;
            Assert.InRange(Top(form.FileDescription) - belowDivider, 0, 30);
            Assert.DoesNotContain(form.FileDescription.Parent!.Controls.OfType<Label>(),
                label => label != form.FileDescription && Top(label) < Top(form.FileDescription) && Top(label) > belowDivider);
        });
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

    [Theory]
    [InlineData(AppLanguage.Dutch)]
    [InlineData(AppLanguage.French)]
    [InlineData(AppLanguage.English)]
    public void TheDescription_IsAsWideAsTheFileRow_AndHasRoomForAllItsLines(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            Label description = window.Form.FileDescription;
            Control fileRow = window.Form.XmlBrowseButton.Parent!;

            Assert.Equal(fileRow.Width, description.Width);
            Assert.True(description.Height >= description.GetPreferredSize(new Size(description.Width, 0)).Height);
            Assert.True(description.Height >= 2 * description.Font.Height, "two paragraphs: at least two lines");
            Assert.True(Top(description) + description.Height <= Top(fileRow), "the description stays above the path box");
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

            foreach (Button button in new Button[] { form.XmlBrowseButton, form.CsvBrowseButton, form.SaveButton, form.XmlViewButton, form.CsvViewButton, form.PreviousButton, form.NextButton })
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
            Assert.Equal(Strings.FileDescription, window.Form.FileDescription.Text);
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
            Assert.Equal(Top(form.CsvViewButton), Top(sizeFrame)); // in the title row, in line with the view buttons
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
