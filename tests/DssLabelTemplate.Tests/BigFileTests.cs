namespace DssLabelTemplate.Tests;

/// <summary>
/// Opening a big label-csv (Daan, 2026-10-26: the window froze for almost 6 seconds): the csv table no longer measures
/// every cell, and a gray layer with "loading" catches the clicks while a big file is opened.
/// </summary>
public class BigFileTests
{
    /// <summary>A label-csv of TsDssConverter (33 columns) with the 13 lines of the sample repeated to <paramref name="lines"/>.</summary>
    private static string WriteBigCsv(TempFolder folder, int lines)
    {
        string[] sample = File.ReadAllLines(Samples.File("DAAN_ROGIERS-P2026.09_001.csv"));
        var all = new List<string> { sample[0] };
        for (int i = 0; all.Count <= lines; i++)
        {
            all.Add(sample[1 + i % (sample.Length - 1)]);
        }

        string path = folder.File("big.csv");
        File.WriteAllLines(path, all);
        return path;
    }

    [Fact]
    public void ABigCsv_IsShownInTheTable_AndEveryColumnFitsItsTitleAndItsLongestText()
    {
        Sta.Run(() =>
        {
            using var folder = new TempFolder();
            string csv = WriteBigCsv(folder, 1000);
            Assert.True(BusyOverlay.SizeOfFiles(csv) >= BusyOverlay.FromBytes, "big enough for the gray layer");
            int formsBefore = Application.OpenForms.Count;

            using var form = new MainForm(new AppSettings(), saved => { }, fileToOpen: null) { Opacity = 0, ShowInTaskbar = false };
            form.Show();
            Application.DoEvents();
            Assert.True(form.OpenLabelFiles(Samples.File("HUPPY-PINCKAERS-60x60.xml"), csv));
            Application.DoEvents();

            Assert.Equal(formsBefore + 1, Application.OpenForms.Count); // only the window: the gray layer is gone again
            Assert.Equal(1000, form.CsvGrid.Rows.Count);
            Assert.Equal(1000, form.Preview.Count);
            Assert.Equal(DataGridViewAutoSizeColumnsMode.None, form.CsvGrid.AutoSizeColumnsMode); // not every cell measured

            foreach (DataGridViewColumn column in form.CsvGrid.Columns)
            {
                Assert.True(column.Width >= column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true), column.HeaderText);
                DataGridViewCell longest = form.CsvGrid.Rows.Cast<DataGridViewRow>().Take(13)
                    .Select(row => row.Cells[column.Index]).MaxBy(cell => cell.Value?.ToString()?.Length ?? 0)!;
                Assert.True(column.Width >= longest.PreferredSize.Width, $"{column.HeaderText}: {longest.Value}");
            }

            Assert.True(form.CsvGrid.Columns[0].Width >= form.CsvGrid.Rows[999].Cells[0].PreferredSize.Width, "room for \"1000\"");
        });
    }

    [Fact]
    public void SmallFiles_GiveNoGrayLayer()
    {
        Sta.Run(() =>
        {
            using var form = new Form { Opacity = 0, ShowInTaskbar = false };
            form.Show();

            long sample = BusyOverlay.SizeOfFiles(Samples.File(Samples.Template), Samples.File(Samples.Csv), null, @"C:\does\not\exist.csv");
            Assert.InRange(sample, 1, BusyOverlay.FromBytes - 1);
            Assert.Null(BusyOverlay.ShowIfBig(form, sample));
        });
    }

    [Fact]
    public void TheGrayLayer_CoversTheWindowAndItsTitleBar_WithTheTextInTheMiddle_AndIsGoneAfterwards()
    {
        Sta.Run(() =>
        {
            using var scope = new LanguageScope(AppLanguage.English);
            using var form = new Form { Opacity = 0, ShowInTaskbar = false, Size = new Size(800, 600) };
            form.Show();
            int formsBefore = Application.OpenForms.Count;

            Form shade, box;
            using (BusyOverlay? busy = BusyOverlay.ShowIfBig(form, BusyOverlay.FromBytes))
            {
                Assert.NotNull(busy);
                shade = busy.Shade;
                box = busy.Box;
                Rectangle inside = form.RectangleToScreen(form.ClientRectangle);

                Assert.True(shade.Visible && box.Visible);
                Assert.Equal(Rectangle.FromLTRB(inside.Left, form.Top, inside.Right, inside.Bottom), shade.Bounds); // also the X
                Assert.Equal(0, shade.Opacity); // the window is invisible (a test): so is its layer
                Assert.Contains(box.Controls.OfType<Label>(), label => label.Text == "Loading…");
                Assert.InRange(box.Left + box.Width / 2 - (inside.Left + inside.Width / 2), -1, 1);
                Assert.InRange(box.Top + box.Height / 2 - (inside.Top + inside.Height / 2), -1, 1);
                Assert.True(shade.ContainsFocus || Form.ActiveForm != form, "the keys go to the layer, not to the window");
            }

            Assert.True(shade.IsDisposed && box.IsDisposed);
            Assert.Equal(formsBefore, Application.OpenForms.Count);
        });
    }
}
