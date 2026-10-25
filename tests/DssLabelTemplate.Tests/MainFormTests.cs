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
        public AppSettings Settings { get; } = new();
        public int SaveCount { get; private set; }
        public MainForm Form { get; }

        public Window()
        {
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
    public void BrowseClosePreviewArrowAndDividers_EndAtTheSameRightEdge()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            int edge = RightEdge(form.CloseButton);
            Assert.Equal(edge, RightEdge(form.XmlBrowseButton));
            Assert.Equal(edge, RightEdge(form.CsvBrowseButton));
            Assert.Equal(edge, RightEdge(form.Preview));
            Assert.Equal(edge, RightEdge(form.NextButton));
            foreach (Panel divider in form.Dividers)
            {
                Assert.Equal(edge, RightEdge(divider));
            }
        });
    }

    [Fact]
    public void TheEditorAndThePreview_HaveTheSameWidth_AndTheSameHeight()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            Control editorFrame = window.Form.Editor.Parent!;

            Assert.InRange(Math.Abs(editorFrame.Width - window.Form.Preview.Width), 0, 1);
            Assert.Equal(editorFrame.Height, window.Form.Preview.Height);
            Assert.True(window.Form.Preview.Height > 200, "row 2 is the big row: " + window.Form.Preview.Height);
        });
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
    public void ThePreviewHeader_KeepsItsHeight_WithAndWithoutArrows()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            int withoutArrows = form.PreviewHeader.Height;
            int previewTop = Top(form.Preview);

            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            Assert.True(form.NextButton.Visible);
            Assert.Equal(withoutArrows, form.PreviewHeader.Height);
            Assert.Equal(previewTop, Top(form.Preview)); // the preview (and the editor) do not jump
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

            foreach (Button button in new Button[] { form.XmlBrowseButton, form.CsvBrowseButton, form.SaveButton, form.CloseButton, form.PreviousButton, form.NextButton })
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
            Assert.Equal("Opslaan", window.Form.SaveButton.Text);

            window.Form.LanguageBox.SelectedIndex = (int)AppLanguage.English;

            Assert.Equal("Save", window.Form.SaveButton.Text);
            Assert.Equal("Label files", window.Form.FileCaption.Text);
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
    public void TheLabelSize_EndsInLineWithBrowse_AndTheStatusLineStopsBeforeIt(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv)); // a long status line
            window.Pump();

            Assert.Equal(RightEdge(form.XmlBrowseButton), RightEdge(form.LabelWidthBox.Parent!));
            int statusRight = form.FileStatus.PointToScreen(new Point(form.FileStatus.Width, 0)).X;
            Assert.True(statusRight <= form.LabelSizePanel.PointToScreen(Point.Empty).X, "the status line runs into the label size");
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
