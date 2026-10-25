namespace DssLabelTemplate.Tests;

/// <summary>The two file lines (template / label-xml and label-csv, each with its own Browse) and the Tab order.</summary>
public class FileLinesTests
{
    /// <summary>A window with fake settings, shown invisibly (Opacity 0) so the layout is really done.</summary>
    private sealed class Window : IDisposable
    {
        public AppSettings Settings { get; } = new();
        public MainForm Form { get; }

        public Window()
        {
            Form = new MainForm(Settings, saved => { }, fileToOpen: null) { Opacity = 0, ShowInTaskbar = false };
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

        public void Dispose() => Form.Dispose();
    }

    private static int Left(Control control) => control.PointToScreen(Point.Empty).X;
    private static int Right(Control control) => control.PointToScreen(new Point(control.Width, 0)).X;

    /// <summary>The controls Tab stops at, in order (the same walk as WinForms' own Tab key).</summary>
    private static List<Control> TabStops(Form form)
    {
        var stops = new List<Control>();
        for (Control? control = form.GetNextControl(form, true); control != null; control = form.GetNextControl(control, true))
        {
            if (control.TabStop && control.CanSelect)
            {
                stops.Add(control);
            }
        }

        return stops;
    }

    // ---------------------------------------------------------------- Tab

    [Fact]
    public void Tab_GoesFromTheTemplate_ToTheCsv_ToTheLabelLengthAndWidth_AndNowhereElse()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();

            Assert.Equal(new Control[] { form.XmlFileBox, form.CsvFileBox, form.LabelLengthBox, form.LabelWidthBox }, TabStops(form));
        });
    }

    [Fact]
    public void WithALabelXml_TabSkipsTheGreyCsvBox()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);
            window.Pump();

            Assert.Equal(new Control[] { form.XmlFileBox, form.LabelLengthBox, form.LabelWidthBox }, TabStops(form));
        });
    }

    [Fact]
    public void EnteringAField_SelectsAllItsText_SoTypingReplacesIt()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.Activate();

            foreach (TextBox box in new[] { form.LabelLengthBox, form.LabelWidthBox, form.XmlFileBox })
            {
                box.Text = "123.5";
                box.Select(0, 0); // the caret at the start, nothing selected
                box.Focus();
                window.Pump();

                Assert.True(box.Focused, "the test window could not take the focus");
                Assert.Equal(box.TextLength, box.SelectionLength);
            }
        });
    }

    // ---------------------------------------------------------------- the csv line

    [Fact]
    public void ALabelXml_GreysOutTheCsvLine_ATemplateDoesNot()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            Assert.True(form.CsvFileBox.Enabled); // nothing open yet: a csv may be chosen first

            form.OpenXmlFile(Samples.File(Samples.LabelXml));
            Assert.False(form.CsvFileBox.Enabled || form.CsvBrowseButton.Enabled);
            Assert.Equal(Strings.CsvNotNeededPlaceholder, form.CsvFileBox.PlaceholderText);

            form.OpenXmlFile(Samples.File(Samples.Template));
            Assert.True(form.CsvFileBox.Enabled && form.CsvBrowseButton.Enabled);
            Assert.Equal(Strings.CsvPlaceholder, form.CsvFileBox.PlaceholderText);
        });
    }

    [Fact]
    public void TheCsvMayComeFirst_ThenTheTemplate()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            string csv = Samples.File(Samples.Csv);

            Assert.True(form.OpenCsvFile(csv));
            Assert.Equal(csv, form.CsvFileBox.Text);
            Assert.Equal(Strings.StatusCsvWithoutTemplate, form.FileStatus.Text);
            Assert.Equal("", form.Editor.Text); // the editor is not touched

            Assert.True(form.OpenXmlFile(Samples.File(Samples.Template)));
            Assert.Equal(3, form.Preview.Count);
            Assert.Equal(Strings.StatusTemplateWithCsv(3), form.FileStatus.Text);
            Assert.Equal(csv, window.Settings.LastCsvFile);
        });
    }

    [Fact]
    public void AnotherTemplate_KeepsTheCsv_ALabelXml_ClosesIt()
    {
        using var folder = new TempFolder();
        string otherTemplate = folder.File("other-template.xml");
        File.Copy(Samples.File(Samples.Template), otherTemplate);

        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            string csv = Samples.File(Samples.Csv);
            form.OpenLabelFiles(Samples.File(Samples.Template), csv);

            form.OpenXmlFile(otherTemplate);
            Assert.Equal(csv, form.CurrentCsvFile);
            Assert.Equal(3, form.Preview.Count);

            form.OpenXmlFile(Samples.File(Samples.LabelXml));
            Assert.Null(form.CurrentCsvFile);
            Assert.Equal("", form.CsvFileBox.Text);
            Assert.Equal("", window.Settings.LastCsvFile);
            Assert.False(form.CsvViewButton.Visible);
        });
    }

    [Fact]
    public void ClosingTheCsv_LeavesTheTemplateWithoutData()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            form.ShowCsvView(true);

            Assert.True(form.OpenCsvFile(null)); // what an empty box + Enter does

            Assert.Null(form.CurrentCsvFile);
            Assert.Equal(1, form.Preview.Count);
            Assert.False(form.ShowingCsv);
            Assert.Equal(Strings.StatusTemplateWithoutCsv, form.FileStatus.Text);
            Assert.NotNull(form.CurrentFile); // the template stays open
        });
    }

    [Fact]
    public void ALabelXmlFromTheLastTime_IgnoresTheCsvOfTheLastTime()
    {
        Sta.Run(() =>
        {
            using var window = new Window();

            Assert.True(window.Form.OpenLabelFiles(Samples.File(Samples.LabelXml), Samples.File(Samples.Csv)));

            Assert.Null(window.Form.CurrentCsvFile);
            Assert.Equal(2, window.Form.Preview.Count); // the two labels of the label-xml, not three csv lines
        });
    }

    // ---------------------------------------------------------------- layout

    [Theory]
    [InlineData(AppLanguage.Dutch)]
    [InlineData(AppLanguage.French)]
    [InlineData(AppLanguage.English)]
    public void ThePathBoxesStartAtTheSamePlace_AndTheCaptionsStayOneLine(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;

            Assert.Equal(Left(form.XmlFileBox.Parent!), Left(form.CsvFileBox.Parent!));
            Assert.Equal(Right(form.XmlFileBox.Parent!), Right(form.CsvFileBox.Parent!));
            Assert.Equal(Right(form.XmlBrowseButton), Right(form.CsvBrowseButton));
            foreach (Label caption in new[] { form.XmlFileCaption, form.CsvFileCaption })
            {
                Assert.True(caption.Height < 2 * caption.Font.Height, $"'{caption.Text}' takes more than one line");
                Assert.True(Right(caption) <= Left(form.XmlFileBox.Parent!), $"'{caption.Text}' runs into the path box");
            }
        });
    }
}
