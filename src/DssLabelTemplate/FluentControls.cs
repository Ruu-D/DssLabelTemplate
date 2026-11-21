using System.Drawing.Drawing2D;

namespace DssLabelTemplate;

// The three controls in this file give the window a softer, Windows 11 ("Fluent") look:
// rounded corners, a fill that changes when the mouse is over them, and a thin accent line on a text box that has
// the focus. WinForms draws its own controls in the old square style; these controls paint themselves instead.
// They are still a normal Button, CheckBox and TextBox for the rest of the program (Text, Checked, Click, ...).
// All colours come from Theme, and every size is written for 96 dpi and scaled with LogicalToDeviceUnits.

/// <summary>Small drawing helper shared by the controls below.</summary>
internal static class FluentDrawing
{
    /// <summary>The corner radius of every control, in pixels at 100% scaling.</summary>
    public const int CornerRadius = 4;

    /// <summary>A rectangle with rounded corners, as a path that can be filled and drawn.</summary>
    public static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
    {
        float diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>The colour behind a control, so the corners that are not painted look right.</summary>
    public static Color BehindColor(Control control)
    {
        return control.Parent?.BackColor ?? Theme.White;
    }
}

/// <summary>
/// A button with rounded corners. The colours are set by Theme.StylePrimaryButton / StyleSecondaryButton.
/// Keyboard focus is shown with a navy ring, so the window can still be used without a mouse.
/// </summary>
internal class RoundedButton : Button
{
    private bool _hover;
    private bool _pressed;

    internal Color NormalFill = Theme.White;
    internal Color HoverFill = Theme.VeryPaleBlue;
    internal Color PressedFill = Theme.PaleBlue;

    /// <summary>Border colour; Color.Empty = no border.</summary>
    internal Color BorderColor = Color.Empty;

    internal Color NormalText = Theme.Navy;
    internal Color HoverText = Theme.Navy;

    /// <summary>The look of a button that cannot be pressed now: Color.Empty = its normal fill, with pale text.</summary>
    internal Color DisabledFill = Color.Empty;
    internal Color DisabledText = Theme.PaleBlue;

    /// <summary>The corner radius in pixels at 100% (the navy buttons of the Maestro skin are almost square).</summary>
    internal int Radius = FluentDrawing.CornerRadius;

    public RoundedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _pressed = e.Button == MouseButtons.Left;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(FluentDrawing.BehindColor(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color fill = _pressed ? PressedFill : _hover ? HoverFill : NormalFill;
        Color text = _pressed || _hover ? HoverText : NormalText;
        Color border = BorderColor;
        if (!Enabled)
        {
            // A button that cannot be pressed now (e.g. Browse of the csv next to a label-xml): pale text and border.
            fill = DisabledFill == Color.Empty ? NormalFill : DisabledFill;
            text = DisabledText;
            border = border == Color.Empty ? border : Theme.PaleBlue;
        }

        float radius = LogicalToDeviceUnits(Radius);
        var outline = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1); // half a pixel in: the border stays inside

        using (GraphicsPath path = FluentDrawing.RoundedRectangle(outline, radius))
        using (var brush = new SolidBrush(fill))
        {
            g.FillPath(brush, path);

            if (border != Color.Empty)
            {
                using var pen = new Pen(border, 1);
                g.DrawPath(pen, path);
            }
        }

        if (Image != null && Text == "")
        {
            DrawIcon(g);
        }
        else if (Image != null)
        {
            DrawIconAndText(g, text);
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        if (Focused && ShowFocusCues)
        {
            float inset = LogicalToDeviceUnits(2);
            var ringRect = new RectangleF(inset, inset, Width - 1 - 2 * inset, Height - 1 - 2 * inset);
            using GraphicsPath ring = FluentDrawing.RoundedRectangle(ringRect, radius);
            using var ringPen = new Pen(Theme.Navy, 1.5f);
            g.DrawPath(ringPen, ring);
        }
    }

    /// <summary>The usual height of a picture on a button, in pixels at 100% (as high as the text of a normal button).</summary>
    internal const int DefaultIconHeight = 18;

    /// <summary>The height of the picture on this button (the print icon), in pixels at 100%.</summary>
    internal int IconHeight = DefaultIconHeight;

    /// <summary>
    /// A button with a picture (Image) instead of a text: the picture in the middle, <see cref="IconHeight"/> high with its
    /// own proportions. A button that cannot be pressed shows it pale, like the pale text of the other buttons.
    /// </summary>
    private void DrawIcon(Graphics g)
    {
        float height = LogicalToDeviceUnits(IconHeight);
        float width = height * Image!.Width / Image.Height;
        var place = new RectangleF((Width - width) / 2, (Height - height) / 2, width, height);

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        if (Enabled)
        {
            g.DrawImage(Image, place);
            return;
        }

        using var pale = new System.Drawing.Imaging.ImageAttributes();
        pale.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.3f }); // 30% opaque
        g.DrawImage(Image, Rectangle.Round(place), 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, pale);
    }

    /// <summary>The room between the picture and the text of a button that has both (Browse: a folder + "Browse…"), at 100%.</summary>
    internal const int IconTextGap = 8;

    /// <summary>
    /// A picture AND a text (the Browse buttons of 2.0.0: a folder before "Browse…"): the two together in the middle of the
    /// button, the picture <see cref="IconHeight"/> high.
    /// </summary>
    private void DrawIconAndText(Graphics g, Color textColor)
    {
        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        float height = LogicalToDeviceUnits(IconHeight);
        float width = height * Image!.Width / Image.Height;
        int gap = LogicalToDeviceUnits(IconTextGap);
        Size textSize = TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, int.MaxValue), flags);
        float left = Math.Max(0, (Width - (width + gap + textSize.Width)) / 2f);

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var place = new RectangleF(left, (Height - height) / 2, width, height);
        if (Enabled)
        {
            g.DrawImage(Image, place);
        }
        else
        {
            using var pale = new System.Drawing.Imaging.ImageAttributes();
            pale.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.4f });
            g.DrawImage(Image, Rectangle.Round(place), 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, pale);
        }

        g.PixelOffsetMode = PixelOffsetMode.Default;
        var textArea = new Rectangle((int)(left + width + gap), 0, Math.Max(0, Width - (int)(left + width + gap)), Height);
        TextRenderer.DrawText(g, Text, Font, textArea, textColor, flags | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    /// <summary>The width a button with a picture and a text needs at least (both, the gap, and room on both sides), in pixels.</summary>
    internal int WidthNeededForIconAndText()
    {
        float height = LogicalToDeviceUnits(IconHeight);
        float width = Image == null ? 0 : height * Image.Width / Image.Height;
        int text = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
        return (int)Math.Ceiling(width) + LogicalToDeviceUnits(IconTextGap) + text + LogicalToDeviceUnits(2 * 14);
    }
}

/// <summary>
/// The handle between the two columns that can be dragged with the mouse (Daan, 2026-10-25: "make it possible to move the
/// vertical divider"). Since 2.0.0 (the Maestro skin) it is the grey gap between the two white panels: at rest nothing is
/// drawn on it, under the mouse a light-blue line shows that it can be moved. The window decides what dragging does: it
/// gets the mouse position and sets the column widths.
/// </summary>
internal class ColumnSplitter : Control
{
    /// <summary>The width of the handle at 100%: the grey gap between the two panels.</summary>
    public const int HandleWidth = 14;

    private bool _hover;
    private bool _dragging;

    /// <summary>While dragging: the mouse position, as X in the coordinates of the parent (the table with the columns).</summary>
    public event Action<int>? Dragged;

    /// <summary>The mouse button was released after dragging.</summary>
    public event Action? Released;

    /// <summary>A double-click: back to two equal halves.</summary>
    public event Action? ResetRequested;

    public ColumnSplitter()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.VSplit;
        TabStop = false;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            Capture = true; // keep getting the mouse, also when it is moved faster than the line follows
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            Dragged?.Invoke(Left + e.X);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            Capture = false;
            Invalidate();
            Released?.Invoke();
        }

        base.OnMouseUp(e);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ResetRequested?.Invoke();
        }

        base.OnMouseDoubleClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(FluentDrawing.BehindColor(this));

        // At rest only the grey gap; under the mouse a light-blue line, so it is clear that it can be moved.
        if (_hover || _dragging)
        {
            int thickness = LogicalToDeviceUnits(4);
            using var line = new SolidBrush(Theme.Selection);
            g.FillRectangle(line, (Width - thickness) / 2, 0, thickness, Height);
        }
    }
}

/// <summary>
/// A check box in the Windows 11 style: a rounded square that fills with the brand colour with a tick when checked.
/// </summary>
internal class FluentCheckBox : CheckBox
{
    private const int BoxSize = 18; // at 100%
    private const int TextGap = 8;  // between the box and the text, at 100%

    private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

    private bool _hover;

    public FluentCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
    }

    /// <summary>The size for AutoSize: the box, the gap and the text. (The standard size is for the old square box.)</summary>
    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), MeasureFlags);
        int box = LogicalToDeviceUnits(BoxSize);
        int width = box + LogicalToDeviceUnits(TextGap) + text.Width + LogicalToDeviceUnits(2);
        int height = Math.Max(box, text.Height) + LogicalToDeviceUnits(4);
        return new Size(width, height);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        Invalidate();
        base.OnCheckedChanged(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(FluentDrawing.BehindColor(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int box = LogicalToDeviceUnits(BoxSize);
        float radius = LogicalToDeviceUnits(FluentDrawing.CornerRadius - 1);
        float top = (Height - box) / 2f;
        var outline = new RectangleF(0.5f, top + 0.5f, box - 1, box - 1);

        // Unchecked: a white box with a blue border. Checked: filled with the brand colour (dark blue under the mouse).
        Color fill = Checked ? (_hover ? Theme.DarkBlue : Theme.Brand) : (_hover ? Theme.VeryPaleBlue : Theme.White);
        Color border = Checked ? fill : Theme.DarkBlue;

        using (GraphicsPath path = FluentDrawing.RoundedRectangle(outline, radius))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border, 1))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        if (Checked)
        {
            // The tick, as three points inside the box. Navy on the brand colour, white on dark blue (readable both ways).
            using var tickPen = new Pen(_hover ? Theme.White : Theme.Navy, Math.Max(1.5f, box / 9f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round,
            };
            g.DrawLines(tickPen, new[]
            {
                new PointF(box * 0.24f, top + box * 0.53f),
                new PointF(box * 0.43f, top + box * 0.72f),
                new PointF(box * 0.77f, top + box * 0.30f),
            });
        }

        int textLeft = box + LogicalToDeviceUnits(TextGap);
        var textArea = new Rectangle(textLeft, 0, Math.Max(0, Width - textLeft), Height);
        TextRenderer.DrawText(g, Text, Font, textArea, ForeColor, MeasureFlags | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            float grow = LogicalToDeviceUnits(2);
            var ringRect = new RectangleF(outline.Left - grow, outline.Top - grow, outline.Width + 2 * grow, outline.Height + 2 * grow);
            using GraphicsPath ring = FluentDrawing.RoundedRectangle(ringRect, radius + grow);
            using var ringPen = new Pen(Theme.Navy, 1.5f);
            g.DrawPath(ringPen, ring);
        }
    }
}

/// <summary>
/// A frame around one control: rounded border, and a dark blue line at the bottom while the control has the focus.
/// The control inside stays itself (its own border is switched off), so Text, Leave and so on work as before.
/// </summary>
internal abstract class RoundedFrame : Panel
{
    protected readonly Control Content;
    private bool _focused;

    /// <summary>The colour inside the border: white, or the background of the control inside (the grey editor).</summary>
    internal Color FillColor = Theme.White;

    /// <summary>The border; Color.Empty = none (the flat fields of the Maestro skin, <see cref="Theme.StyleField"/>).</summary>
    internal Color BorderColor = Theme.LightBlue;

    /// <summary>The corner radius in pixels at 100%; 0 = square.</summary>
    internal int Radius = FluentDrawing.CornerRadius;

    /// <summary>The line at the bottom while the control inside has the focus.</summary>
    internal Color FocusColor = Theme.DarkBlue;

    protected RoundedFrame(Control content)
    {
        Content = content;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        Content.Enter += (sender, e) => { _focused = true; Invalidate(); };
        Content.Leave += (sender, e) => { _focused = false; Invalidate(); };
        Controls.Add(Content);

        // A click on the frame (outside the control) also puts the focus in it.
        MouseDown += (sender, e) => Content.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(FluentDrawing.BehindColor(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (Radius == 0)
        {
            // Flat and square (the Maestro skin): no smoothing, so the edges stay crisp.
            g.SmoothingMode = SmoothingMode.None;
            using var flat = new SolidBrush(FillColor);
            g.FillRectangle(flat, ClientRectangle);
            if (BorderColor != Color.Empty)
            {
                using var flatPen = new Pen(BorderColor, 1);
                g.DrawRectangle(flatPen, 0, 0, Width - 1, Height - 1);
            }

            if (_focused)
            {
                int thick = LogicalToDeviceUnits(2);
                using var accentBrush = new SolidBrush(FocusColor);
                g.FillRectangle(accentBrush, 0, Height - thick, Width, thick);
            }

            return;
        }

        float radius = LogicalToDeviceUnits(Radius);
        var outline = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);

        using GraphicsPath path = FluentDrawing.RoundedRectangle(outline, radius);
        using (var brush = new SolidBrush(FillColor))
        {
            g.FillPath(brush, path);
        }

        if (BorderColor != Color.Empty)
        {
            using var pen = new Pen(BorderColor, 1);
            g.DrawPath(pen, path);
        }

        if (_focused)
        {
            // Only the bottom strip of the border is drawn again, thicker and darker.
            float line = LogicalToDeviceUnits(2);
            g.SetClip(new RectangleF(0, Height - line, Width, line));
            using var accent = new Pen(FocusColor, line);
            g.DrawPath(accent, path);
            g.ResetClip();
        }
    }
}

/// <summary>A one-line TextBox in a rounded frame: in the middle of the frame's height, filling its width.</summary>
internal class TextBoxFrame : RoundedFrame
{
    private const int SidePadding = 8; // between the border and the text, at 100%

    public TextBoxFrame(TextBox box)
        : base(box)
    {
        box.BorderStyle = BorderStyle.None;
        box.BackColor = Theme.White;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        int side = LogicalToDeviceUnits(SidePadding);
        int height = ((TextBox)Content).PreferredHeight;
        Content.SetBounds(side, (Height - height) / 2, Math.Max(0, Width - 2 * side), height);
    }
}

/// <summary>A big control (the XML editor, the csv table) in a rounded frame: it fills the frame, just inside the border.</summary>
internal class ContentFrame : RoundedFrame
{
    /// <summary>The room between the frame and the control inside, at 100% (inside the rounded corners; 0 for a flat frame).</summary>
    internal int Inset = 3;

    public ContentFrame(Control content)
        : base(content)
    {
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        int inset = LogicalToDeviceUnits(Inset);
        Content.SetBounds(inset, inset, Math.Max(0, Width - 2 * inset), Math.Max(0, Height - 2 * inset));
    }
}
