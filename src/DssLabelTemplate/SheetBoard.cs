namespace DssLabelTemplate;

/// <summary>
/// The board view (Daan, 2026-11-08): the "View board" button below the preview replaces the left column with this — the
/// sheet seen from above with ONLY the label positions, as big as the column allows, on the same very pale blue panel as
/// the preview. It is the small sheet map below the label, made big: drawn by the same code
/// (<see cref="LabelPreview.DrawSheetMap"/>), so it follows the same corner, turn and label size, and the label shown in
/// the preview is the one in the brand colour.
/// </summary>
internal class SheetBoard : Control
{
    // A tap this far (at 100%) outside a label's square still chooses it: a finger is bigger than the small square of a
    // label on a big sheet (touch screen at the reprint station).
    private const int TapReach = 16;

    private readonly LabelPreview _preview;

    /// <summary>A label was tapped or clicked on the board (Daan, 2026-11-08): its index, to show it in the preview.</summary>
    public event Action<int>? LabelChosen;

    public SheetBoard(LabelPreview preview)
    {
        _preview = preview;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false; // nothing to type here
        Cursor = Cursors.Hand; // the labels can be clicked

        // Everything the board shows comes from the preview. The preview is drawn again whenever any of it changes (another
        // label, another file, the label size, the corner or turn from settings.json): then the board is drawn again too.
        _preview.Invalidated += (sender, e) => Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        Rectangle inside = LabelPreview.DrawPanel(g, this);

        if (!_preview.ShownLabelHasSheet)
        {
            // Normally not seen: the button is only there when the labels know their sheet.
            TextRenderer.DrawText(g, Strings.BoardWithoutSheet, Font, inside, Theme.Navy,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            return;
        }

        LabelPreview.DrawSheetMap(g, this, inside, _preview.Labels!.Labels, _preview.Index, _preview.LabelSize,
            _preview.SheetRotation, _preview.ReferenceCorner);
    }

    /// <summary>
    /// The label at this point of the board (in the board's own pixels), or -1: the same squares as drawn, plus
    /// <see cref="TapReach"/> around them for a finger.
    /// </summary>
    internal int LabelAt(Point point)
    {
        if (!_preview.ShownLabelHasSheet)
        {
            return -1;
        }

        return LabelPreview.FindLabelAt(this, LabelPreview.PanelInside(this), _preview.Labels!.Labels, _preview.Index,
            _preview.LabelSize, _preview.SheetRotation, _preview.ReferenceCorner, point, LogicalToDeviceUnits(TapReach));
    }

    /// <summary>
    /// A tap or click on a label shows that label. On the mouse button going DOWN (not up): on a touch screen that is the
    /// moment the finger touches, so the preview answers at once.
    /// </summary>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        ChooseLabelAt(e.Location);
    }

    /// <summary>Shows the label at this point (a tap); nothing when there is no label near it. Returns the label or -1.</summary>
    internal int ChooseLabelAt(Point point)
    {
        int index = LabelAt(point);
        if (index >= 0)
        {
            LabelChosen?.Invoke(index);
        }

        return index;
    }
}
