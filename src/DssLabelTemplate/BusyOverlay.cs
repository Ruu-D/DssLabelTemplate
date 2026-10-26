namespace DssLabelTemplate;

/// <summary>
/// A gray layer over the window with "Bezig met laden…" in a white box in the middle, while a big file is opened
/// (Daan, 2026-10-26: the window froze for seconds, and clicks made during that time were carried out afterwards).
/// The work runs on the window's own thread, so the window cannot react anyway; the layer shows that, and it catches
/// the clicks and keys of that time: they reach the layer (which does nothing with them) instead of the buttons below.
///
/// Use: <c>using (BusyOverlay.ShowIfBig(this, bytes)) { ... the work ... }</c> — for small files nothing is shown
/// (ShowIfBig gives null), so a fast open does not flash gray.
/// </summary>
internal sealed class BusyOverlay : IDisposable
{
    /// <summary>
    /// Files that are together smaller than this open in about 0.2 s: no layer. 200 KB is about 650 lines of a label-csv
    /// of TsDssConverter (33 columns).
    /// </summary>
    internal const long FromBytes = 200 * 1024;

    // How much of the window still shows through the gray (0 = only gray, 1 = no gray).
    private const double ShadeOpacity = 0.45;

    private readonly Form _owner;
    private readonly Form _shade = new();
    private readonly Form _box = new();
    private readonly Label _text = new();

    /// <summary>The gray layer and the box with the text (the tests check them).</summary>
    internal Form Shade => _shade;
    internal Form Box => _box;

    /// <summary>
    /// Shows the layer over <paramref name="owner"/> when <paramref name="bytes"/> (the size of the files that will be
    /// read) is big, and paints it at once (the window's thread is about to be busy). Null when nothing is shown.
    /// </summary>
    public static BusyOverlay? ShowIfBig(Form owner, long bytes)
    {
        if (bytes < FromBytes || !owner.Visible || owner.WindowState == FormWindowState.Minimized)
        {
            return null;
        }

        return new BusyOverlay(owner);
    }

    /// <summary>The size in bytes of the files that exist (a path that is null or cannot be read counts 0).</summary>
    public static long SizeOfFiles(params string?[] paths)
    {
        long total = 0;
        foreach (string? path in paths)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    total += new FileInfo(path).Length;
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException)
            {
                // the reading itself will say what is wrong with the file
            }
        }

        return total;
    }

    private BusyOverlay(Form owner)
    {
        _owner = owner;
        owner.Update(); // first paint what is still waiting (e.g. the place where the Browse dialog was)

        // The gray layer covers the inside of the window AND its title bar: a click on the X during the work must not
        // close the window halfway through opening a file. (Not the invisible resize border around the window.)
        Rectangle inside = owner.RectangleToScreen(owner.ClientRectangle);
        var covered = Rectangle.FromLTRB(inside.Left, owner.Top, inside.Right, inside.Bottom);

        // An invisible window (the tests) gets an invisible layer: Opacity follows the window's own.
        SetUpLayer(_shade, Theme.BusyShade, ShadeOpacity * owner.Opacity);
        _shade.Bounds = covered;
        _shade.Cursor = Cursors.WaitCursor;

        _text.Text = Strings.Loading;
        _text.Font = new Font(owner.Font.FontFamily, owner.Font.SizeInPoints * 1.5f, FontStyle.Bold, GraphicsUnit.Point);
        _text.ForeColor = Theme.Navy;
        _text.BackColor = Theme.White;
        _text.TextAlign = ContentAlignment.MiddleCenter;
        _text.Dock = DockStyle.Fill;
        _text.Cursor = Cursors.WaitCursor;

        Size textSize = TextRenderer.MeasureText(Strings.Loading, _text.Font);
        var boxSize = new Size(textSize.Width + 2 * owner.LogicalToDeviceUnits(32), textSize.Height + 2 * owner.LogicalToDeviceUnits(20));
        SetUpLayer(_box, Theme.White, owner.Opacity);
        _box.Bounds = new Rectangle(inside.Left + (inside.Width - boxSize.Width) / 2, inside.Top + (inside.Height - boxSize.Height) / 2, boxSize.Width, boxSize.Height);
        _box.Padding = new Padding(1); // room for the pale blue border, drawn below
        _box.BackColor = Theme.PaleBlue;
        _box.Controls.Add(_text);

        // Shown and activated: from now on the clicks AND the keys (e.g. Ctrl+O) go to the layer, not to the window.
        _shade.Show(owner);
        _box.Show(_shade);
        _shade.Refresh();
        _box.Refresh();
        Cursor.Current = Cursors.WaitCursor;
    }

    private static void SetUpLayer(Form layer, Color color, double opacity)
    {
        layer.FormBorderStyle = FormBorderStyle.None;
        layer.ShowInTaskbar = false;
        layer.StartPosition = FormStartPosition.Manual;
        layer.BackColor = color;
        layer.Opacity = opacity;
        layer.ControlBox = false;
        layer.Text = "";
    }

    /// <summary>
    /// The work is done: first the clicks and keys that were made meanwhile are handled (by the layer, which ignores
    /// them), then the layer goes away and the window is active again.
    /// </summary>
    public void Dispose()
    {
        Application.DoEvents();
        Cursor.Current = Cursors.Default;
        _box.Dispose();
        _shade.Dispose();
        _text.Font.Dispose();
        if (!_owner.IsDisposed)
        {
            _owner.Activate(); // the keys go to the window again (with the focus where it was)
        }
    }
}
