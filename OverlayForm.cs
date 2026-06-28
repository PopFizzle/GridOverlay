using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace GridOverlay;

/// <summary>
/// The full-screen, borderless, always-on-top, click-through overlay that renders the
/// grid. Everything except the grid lines is transparent.
/// <para>
/// Rendering model: the form is a layered window painted entirely in
/// <see cref="TransparencyKeyColor"/> (which Windows shows as transparent), so only the
/// lines drawn in <see cref="OnPaint"/> are visible. The <c>WS_EX_TRANSPARENT</c> style
/// (set in <see cref="ApplyExtendedStyles"/>) lets mouse/keyboard pass through to whatever
/// is underneath. The grid is anchored to the center of each monitor and stepped outward.
/// </para>
/// This class is pure rendering + window plumbing; all menu/hotkey logic lives in
/// <see cref="TrayAppContext"/>, which pushes state in via the public properties and
/// <see cref="ApplySettings"/>.
/// </summary>
public sealed class OverlayForm : Form
{
    // Any pixel painted in this exact color becomes fully transparent (and
    // click-through). Chosen to be a color no sane grid line would use.
    private static readonly Color TransparencyKeyColor = Color.FromArgb(255, 0, 254);

    private int _gridSpacing = Settings.MediumSpacing;
    private Color _lineColor = Color.FromArgb(0, 200, 255);
    private int _lineThickness = Settings.ThinWidth;

    private bool _showCenterLines = true;
    private Color _centerLineColor = Color.Red;

    private int _majorLineEvery = 5;
    private Color _majorLineColor = Color.Gold;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.None; // we work in raw physical pixels
        DoubleBuffered = true;
        Text = "GridOverlay";

        // Make the whole window transparent; only painted grid lines show.
        BackColor = TransparencyKeyColor;
        TransparencyKey = TransparencyKeyColor;

        FitToVirtualScreen();
    }

    /// <summary>Pixels between adjacent grid lines.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int GridSpacing
    {
        get => _gridSpacing;
        set { _gridSpacing = Math.Clamp(value, Settings.MinSpacing, Settings.MaxSpacing); Invalidate(); }
    }

    /// <summary>Color of the grid lines.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color LineColor
    {
        get => _lineColor;
        set { _lineColor = value; Invalidate(); }
    }

    /// <summary>Width of the grid lines, in pixels.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LineThickness
    {
        get => _lineThickness;
        set { _lineThickness = Math.Max(1, value); Invalidate(); }
    }

    /// <summary>Draw the per-monitor center cross in <see cref="CenterLineColor"/>.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCenterLines
    {
        get => _showCenterLines;
        set { _showCenterLines = value; Invalidate(); }
    }

    /// <summary>Color of the per-monitor center cross.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CenterLineColor
    {
        get => _centerLineColor;
        set { _centerLineColor = value; Invalidate(); }
    }

    /// <summary>Every Nth line out from center uses <see cref="MajorLineColor"/>. 0 disables.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MajorLineEvery
    {
        get => _majorLineEvery;
        set { _majorLineEvery = Math.Max(0, value); Invalidate(); }
    }

    /// <summary>Color of the major (every-Nth) lines.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color MajorLineColor
    {
        get => _majorLineColor;
        set { _majorLineColor = value; Invalidate(); }
    }

    // Add the extended styles that WinForms' CreateParams can express up front:
    // tool window (out of alt-tab) and no-activate (never steal game focus).
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    // Prevent the overlay from ever taking activation/focus.
    protected override bool ShowWithoutActivation => true;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyExtendedStyles();
    }

    /// <summary>
    /// Apply the layered + click-through + topmost + tool-window extended styles
    /// via P/Invoke. Re-asserted after operations (e.g. opacity changes) that can
    /// cause WinForms to rewrite the extended style word.
    /// </summary>
    public void ApplyExtendedStyles()
    {
        if (!IsHandleCreated) return;

        IntPtr exStyle = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GWL_EXSTYLE);
        int merged = exStyle.ToInt32()
                     | NativeMethods.WS_EX_LAYERED
                     | NativeMethods.WS_EX_TRANSPARENT
                     | NativeMethods.WS_EX_TOPMOST
                     | NativeMethods.WS_EX_TOOLWINDOW
                     | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GWL_EXSTYLE, new IntPtr(merged));
    }

    /// <summary>Apply a full settings snapshot to the overlay.</summary>
    public void ApplySettings(Settings s)
    {
        _gridSpacing = Math.Clamp(s.GridSpacing, Settings.MinSpacing, Settings.MaxSpacing);
        _lineColor = s.LineColor;
        _lineThickness = Math.Max(1, s.LineThickness);
        _showCenterLines = s.ShowCenterLines;
        _centerLineColor = s.CenterLineColor;
        _majorLineEvery = Math.Max(0, s.MajorLineEvery);
        _majorLineColor = s.MajorLineColor;
        Opacity = Math.Clamp(s.Opacity, 0.05, 1.0);
        FitToActiveScreen(s.ActiveScreenIndex);
        ApplyExtendedStyles();
        Invalidate();
    }

    /// <summary>Resize/reposition the overlay to span the entire virtual screen.</summary>
    public void FitToVirtualScreen()
    {
        Bounds = SystemInformation.VirtualScreen;
        Invalidate();
    }

    /// <summary>
    /// Fit the overlay to a specific monitor, or to the whole virtual screen when
    /// <paramref name="screenIndex"/> is out of range (-1 = all monitors).
    /// </summary>
    public void FitToActiveScreen(int screenIndex)
    {
        Screen[] screens = Screen.AllScreens;
        Bounds = (screenIndex >= 0 && screenIndex < screens.Length)
            ? screens[screenIndex].Bounds
            : SystemInformation.VirtualScreen;
        Invalidate();
    }

    /// <summary>
    /// Write the overlay's computed geometry (DPI, bounds, per-monitor centers) to
    /// <c>%TEMP%\gridoverlay_diag.txt</c>. Not called during normal operation - it's a
    /// manual aid for troubleshooting alignment on multi-monitor / high-DPI setups.
    /// </summary>
    public void DumpDiagnostics()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"--- GridOverlay diagnostics ---");
            sb.AppendLine($"DeviceDpi          = {DeviceDpi}");
            sb.AppendLine($"Form.Bounds        = {Bounds}");
            sb.AppendLine($"Form.ClientSize    = {ClientSize}");
            sb.AppendLine($"VirtualScreen      = {SystemInformation.VirtualScreen}");
            foreach (Screen sc in Screen.AllScreens)
                sb.AppendLine($"Screen {(sc.Primary ? "*" : " ")} Bounds = {sc.Bounds}");
            sb.AppendLine($"GridSpacing        = {_gridSpacing}");
            foreach (Rectangle r in MonitorRects())
            {
                int cx = r.Left + r.Width / 2;
                int cy = r.Top + r.Height / 2;
                sb.AppendLine($"MonitorRect(client)= {r}  centerClient=({cx},{cy})  centerPhysical=({Bounds.X + cx},{Bounds.Y + cy})");
            }
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "gridoverlay_diag.txt"), sb.ToString());
        }
        catch { /* diagnostics only */ }
    }

    /// <summary>
    /// Each monitor's rectangle expressed in the form's client coordinates, clipped
    /// to the visible client area. The grid is anchored to the center of each.
    /// </summary>
    private IEnumerable<Rectangle> MonitorRects()
    {
        var client = new Rectangle(Point.Empty, ClientSize);
        foreach (Screen sc in Screen.AllScreens)
        {
            var r = new Rectangle(
                sc.Bounds.X - Bounds.X,
                sc.Bounds.Y - Bounds.Y,
                sc.Bounds.Width,
                sc.Bounds.Height);
            r.Intersect(client);
            if (r.Width > 0 && r.Height > 0)
                yield return r;
        }
    }

    /// <summary>
    /// Draw a full center-anchored grid for one monitor rect. <paramref name="penFor"/>
    /// picks the pen for each line by its index out from the center (0 = the center line),
    /// so callers can color every Nth line differently.
    /// </summary>
    private void DrawGrid(Graphics g, Rectangle rect, int cx, int cy, Func<int, Pen> penFor)
    {
        // Vertical lines, working outward from the center in both directions.
        for (int x = cx, k = 0; x <= rect.Right; x += _gridSpacing, k++)
            g.DrawLine(penFor(k), x, rect.Top, x, rect.Bottom);
        for (int x = cx - _gridSpacing, k = 1; x >= rect.Left; x -= _gridSpacing, k++)
            g.DrawLine(penFor(k), x, rect.Top, x, rect.Bottom);

        // Horizontal lines, working outward from the center in both directions.
        for (int y = cy, k = 0; y <= rect.Bottom; y += _gridSpacing, k++)
            g.DrawLine(penFor(k), rect.Left, y, rect.Right, y);
        for (int y = cy - _gridSpacing, k = 1; y >= rect.Top; y -= _gridSpacing, k++)
            g.DrawLine(penFor(k), rect.Left, y, rect.Right, y);
    }

    /// <summary>True when the line at index <paramref name="k"/> from center is a major line.</summary>
    private bool IsMajor(int k) => _majorLineEvery > 0 && k > 0 && k % _majorLineEvery == 0;

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;      // crisp 1px lines
        g.PixelOffsetMode = PixelOffsetMode.Half;

        using var pen = new Pen(_lineColor, _lineThickness);
        // Major lines are drawn a touch bolder than minor lines (like graph paper)
        // so they read as section dividers even at a glance.
        using var majorPen = new Pen(_majorLineColor, _lineThickness + 1);
        using var centerPen = new Pen(_centerLineColor, Math.Max(_lineThickness, 2));

        // Pick the minor/major pen for a line by its index out from the center.
        Pen PenFor(int k) => IsMajor(k) ? majorPen : pen;

        // Draw a center-anchored grid per monitor, clipped to that monitor so the
        // grids of adjacent monitors don't bleed into each other.
        foreach (Rectangle rect in MonitorRects())
        {
            g.SetClip(rect);

            int cx = rect.Left + rect.Width / 2;
            int cy = rect.Top + rect.Height / 2;

            DrawGrid(g, rect, cx, cy, PenFor);

            // The center cross on top, in its own color, so the screen center is
            // unmistakable when positioning UI elements.
            if (_showCenterLines)
            {
                g.DrawLine(centerPen, cx, rect.Top, cx, rect.Bottom);
                g.DrawLine(centerPen, rect.Left, cy, rect.Right, cy);
            }
        }

        g.ResetClip();
    }
}
