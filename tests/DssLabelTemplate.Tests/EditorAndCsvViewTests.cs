using ScintillaNET;

namespace DssLabelTemplate.Tests;

/// <summary>The xml editor (Scintilla, coloured) and the csv table in the left column.</summary>
public class EditorAndCsvViewTests
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

    // ---------------------------------------------------------------- the editor

    [Fact]
    public void TheEditor_ColoursTheXml_TagsAttributesAndValues()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            XmlEditor editor = window.Form.Editor;
            editor.LoadText("<Text X=\"10\">Project:</Text>");
            editor.Colorize(0, editor.TextLength); // normally done while painting

            Assert.Equal(Style.Xml.Tag, editor.GetStyleAt(1));                   // T of <Text
            Assert.Equal(Style.Xml.Attribute, editor.GetStyleAt(6));             // X
            Assert.Equal(Style.Xml.DoubleString, editor.GetStyleAt(9));          // 1 of "10"
            Assert.True(editor.Styles[Style.Xml.Tag].Bold);
            Assert.Equal(Theme.XmlTag, editor.Styles[Style.Xml.Tag].ForeColor);
        });
    }

    [Fact]
    public void TheEditorAndItsFrame_HaveTheBackgroundOfTheTheme()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            XmlEditor editor = window.Form.Editor;

            Assert.Equal(Theme.XmlBackground, editor.Styles[Style.Default].BackColor);
            Assert.Equal(Theme.XmlBackground, editor.Styles[Style.Xml.Tag].BackColor); // the text styles too, not only the empty part
            Assert.Equal(Theme.XmlBackground, ((RoundedFrame)editor.Parent!).FillColor);
        });
    }

    /// <summary>
    /// The text colours stay readable on the editor's background: at least 3:1 (the WCAG minimum for bold text, like the
    /// tags), 4.5:1 for the normal text. #A0A0A0 was tried once and gave the dark blue tags only 1.7:1.
    /// </summary>
    [Fact]
    public void TheEditorColours_AreReadableOnItsBackground()
    {
        static double Luminance(Color colour)
        {
            static double Channel(int value)
            {
                double c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(colour.R) + 0.7152 * Channel(colour.G) + 0.0722 * Channel(colour.B);
        }

        static double Contrast(Color text, Color back)
        {
            double a = Luminance(text), b = Luminance(back);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        foreach (Color colour in new[] { Theme.XmlTag, Theme.XmlAttributeName, Theme.XmlComment })
        {
            Assert.True(Contrast(colour, Theme.XmlBackground) >= 3, $"{colour} on {Theme.XmlBackground}: {Contrast(colour, Theme.XmlBackground):0.0}:1");
        }

        Assert.True(Contrast(Theme.XmlText, Theme.XmlBackground) >= 4.5);
    }

    [Fact]
    public void EveryVariable_GetsABox_AndPlainTextDoesNot()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            XmlEditor editor = window.Form.Editor;
            const string text = "<Text X=\"25\">$ORDERNO$ and text</Text>";

            editor.LoadText(text);

            uint variableBit = 1u << 8;
            Assert.NotEqual(0u, editor.IndicatorAllOnFor(text.IndexOf("ORDERNO", StringComparison.Ordinal)) & variableBit);
            Assert.Equal(0u, editor.IndicatorAllOnFor(text.IndexOf("text", StringComparison.Ordinal)) & variableBit);
        });
    }

    [Fact]
    public void OpeningAFile_CannotBeUndone()
    {
        Sta.Run(() =>
        {
            using var window = new Window();

            window.Form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);

            Assert.False(window.Form.Editor.CanUndo); // Ctrl+Z must not empty the editor
            Assert.False(window.Form.IsModified);
        });
    }

    [Fact]
    public void TheEditorFiles_AreWrittenOnce_AndAreTheFilesOfThePackage()
    {
        using var folder = new TempFolder();

        string first = ScintillaFiles.Extract(folder.Path);
        DateTime written = File.GetLastWriteTimeUtc(Path.Combine(first, "Scintilla.dll"));
        string second = ScintillaFiles.Extract(folder.Path);

        Assert.Equal(first, second);
        Assert.Equal(written, File.GetLastWriteTimeUtc(Path.Combine(first, "Scintilla.dll"))); // not written again
        foreach (string name in new[] { "Scintilla.dll", "Lexilla.dll" })
        {
            Assert.Equal(ScintillaFiles.ReadResource(name), File.ReadAllBytes(Path.Combine(first, name)));
        }

        Assert.Empty(Directory.GetFiles(first, "*.tmp"));
    }

    // ---------------------------------------------------------------- the csv table

    [Fact]
    public void ALabelXml_HasNoCsvButtons()
    {
        Sta.Run(() =>
        {
            using var window = new Window();

            window.Form.OpenLabelFiles(Samples.File(Samples.LabelXml), null);

            Assert.False(window.Form.XmlViewButton.Visible || window.Form.CsvViewButton.Visible);
            Assert.False(window.Form.ShowingCsv);
        });
    }

    [Fact]
    public void TheCsvTable_ShowsEveryLineAndColumn_WithTheLabelNumberFirst()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));

            Assert.True(form.XmlViewButton.Visible && form.CsvViewButton.Visible);
            Assert.True(form.ShowingCsv); // with a label-csv the table is shown first (Daan, 2026-10-25)

            form.CsvViewButton.PerformClick();
            window.Pump();

            Assert.True(form.ShowingCsv);
            Assert.True(form.CsvGrid.Visible && !form.Editor.Visible);
            Assert.Equal(Strings.CsvCaption, form.EditorCaption.Text);
            Assert.Equal(3, form.CsvGrid.Rows.Count);
            Assert.Equal(11, form.CsvGrid.Columns.Count); // "#" + the 10 columns of the csv
            Assert.Equal(Strings.CsvNumberColumn, form.CsvGrid.Columns[0].HeaderText);
            Assert.Equal("XPOS", form.CsvGrid.Columns[4].HeaderText);
            Assert.Equal("3", form.CsvGrid.Rows[2].Cells[0].Value?.ToString());
            Assert.Equal("350.2", form.CsvGrid.Rows[2].Cells[9].Value);
            Assert.True(form.CsvGrid.ReadOnly);

            form.XmlViewButton.PerformClick();
            Assert.False(form.ShowingCsv);
            Assert.Equal(Strings.EditorCaption, form.EditorCaption.Text);
        });
    }

    private static List<string> ShownColumns(MainForm form) =>
        form.CsvGrid.Columns.Cast<DataGridViewColumn>().OrderBy(column => column.DisplayIndex).Select(column => column.HeaderText).ToList();

    [Fact]
    public void ADraggedColumn_IsKeptInTheSettings_AndTheNumberColumnStaysFirst()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();
            Assert.Empty(window.Settings.CsvColumnOrder); // opening alone saves nothing

            Assert.True(form.CsvGrid.AllowUserToOrderColumns);
            Assert.True(form.CsvGrid.Columns[0].Frozen); // the label number cannot be dragged among the others

            // What a drag does: the column gets another place (BARCODE1, the last one, to the front).
            form.CsvGrid.Columns.Cast<DataGridViewColumn>().Single(column => column.HeaderText == "BARCODE1").DisplayIndex = 1;
            window.Pump();

            Assert.Equal(new[] { "BARCODE1", "LENGTH", "WIDTH", "ID", "XPOS", "YPOS", "ROTATION", "ORDERNO", "PW", "PH" },
                window.Settings.CsvColumnOrder);
            Assert.Equal(Strings.CsvNumberColumn, ShownColumns(form)[0]);
        });
    }

    [Fact]
    public void TheSavedColumnOrder_IsUsedForEveryCsvThatIsOpened_UnknownNamesAreSkippedAndKept()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            window.Settings.CsvColumnOrder = new List<string> { "id", "NOT_IN_THIS_CSV", "XPOS" }; // any case
            MainForm form = window.Form;

            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();

            Assert.Equal(new[] { Strings.CsvNumberColumn, "ID", "XPOS", "LENGTH", "WIDTH", "YPOS", "ROTATION", "ORDERNO", "PW", "PH", "BARCODE1" },
                ShownColumns(form));
            Assert.Equal("350.2", form.CsvGrid.Rows[2].Cells[9].Value); // only the view moved: the cells are the same

            // A drag in this csv keeps the name this csv does not have, at the end.
            form.CsvGrid.Columns.Cast<DataGridViewColumn>().Single(column => column.HeaderText == "PW").DisplayIndex = 1;
            window.Pump();
            Assert.Equal("PW", window.Settings.CsvColumnOrder[0]);
            Assert.Equal("NOT_IN_THIS_CSV", window.Settings.CsvColumnOrder[^1]);
        });
    }

    [Fact]
    public void TheArrows_SelectTheCsvRow_AndClickingARow_ShowsItsLabel()
    {
        Sta.Run(() =>
        {
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            form.ShowCsvView(true);
            window.Pump();

            form.NextButton.PerformClick();
            Assert.Equal(1, form.CsvGrid.CurrentRow!.Index);

            form.CsvGrid.CurrentCell = form.CsvGrid.Rows[2].Cells[1]; // like a click on the third row
            Assert.Equal(2, form.Preview.Index);
            Assert.Equal("3 / 3", form.LabelCounter.Text);
        });
    }

    [Theory]
    [InlineData(AppLanguage.Dutch, false)]
    [InlineData(AppLanguage.French, false)]
    [InlineData(AppLanguage.English, false)]
    [InlineData(AppLanguage.Dutch, true)]
    [InlineData(AppLanguage.French, true)]
    [InlineData(AppLanguage.English, true)]
    public void TheColumnTitles_StayOneLine_AndBothHeadersAreAsHighAsAButton(AppLanguage language, bool csvView)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            form.ShowCsvView(csvView);
            window.Pump();

            foreach (Label title in new[] { form.EditorCaption, form.PreviewCaption })
            {
                Assert.True(title.Height < 2 * title.Font.Height, $"'{title.Text}' takes more than one line");
            }

            Assert.Equal(form.CsvViewButton.Height, form.EditorHeader.Height);
            Assert.Equal(form.EditorHeader.Height, form.PreviewHeader.Height); // the label size fields are as high as a button
        });
    }

    [Theory]
    [InlineData(AppLanguage.Dutch)]
    [InlineData(AppLanguage.French)]
    [InlineData(AppLanguage.English)]
    public void TheViewButtons_FitTheirText_AndEndInLineWithTheEditor(AppLanguage language)
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(language);
            using var window = new Window();
            MainForm form = window.Form;
            form.OpenLabelFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv));
            window.Pump();

            foreach (Button button in new Button[] { form.XmlViewButton, form.CsvViewButton })
            {
                int needed = TextRenderer.MeasureText(button.Text, button.Font).Width;
                Assert.True(needed + 8 <= button.Width, $"'{button.Text}' needs {needed} in a button of {button.Width}");
            }

            int Right(Control control) => control.PointToScreen(new Point(control.Width, 0)).X;
            Assert.Equal(Right(form.EditorArea), Right(form.CsvViewButton));
        });
    }
}
