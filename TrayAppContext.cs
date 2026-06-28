using Microsoft.Win32;

namespace GridOverlay;

/// <summary>
/// The application "controller". Owns the tray icon and its menu, the overlay window,
/// the global hotkeys, and the app lifetime. There is no visible main window -
/// everything is driven from the tray and from global hotkeys.
/// <para>
/// Each user action (menu click or hotkey) follows the same pattern: update
/// <see cref="_settings"/>, push the change onto <see cref="_overlay"/>, refresh the menu
/// checkmarks, and <see cref="Persist"/> to disk. <see cref="HotKeyManager"/> hotkeys and
/// the matching menu items therefore call into the very same action methods.
/// </para>
/// </summary>
public sealed class TrayAppContext : ApplicationContext
{
    private readonly Settings _settings;
    private readonly OverlayForm _overlay;
    private readonly NotifyIcon _trayIcon;
    private readonly HotKeyManager _hotKeys;

    // Menu items whose checked/state we keep in sync with the settings.
    private ToolStripMenuItem _visibleItem = null!;
    private ToolStripMenuItem _centerLinesItem = null!;
    private ToolStripMenuItem _majorLinesMenu = null!;
    private ToolStripMenuItem _gridSizeMenu = null!;
    private ToolStripMenuItem _lineWidthMenu = null!;
    private ToolStripMenuItem _opacityMenu = null!;
    private ToolStripMenuItem _activeScreenMenu = null!;

    public TrayAppContext()
    {
        _settings = Settings.Load();

        _overlay = new OverlayForm();
        _overlay.ApplySettings(_settings);

        _hotKeys = new HotKeyManager();
        _hotKeys.HotKeyPressed += OnHotKey;

        _trayIcon = new NotifyIcon
        {
            Icon = CreateGridIcon(),
            Text = "GridOverlay",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        _trayIcon.DoubleClick += (_, _) => ToggleVisible();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        ShowOverlay(_settings.Visible);
        UpdateMenuChecks();
        WarnIfHotkeysFailed();
    }

    // ---------------------------------------------------------------- menu ----

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        _visibleItem = new ToolStripMenuItem("Visible (Ctrl+Shift+Home)", null, (_, _) => ToggleVisible());
        menu.Items.Add(_visibleItem);

        menu.Items.Add(new ToolStripMenuItem("Redraw + Reset Size (Ctrl+Shift+Insert)", null, (_, _) => Redraw()));

        menu.Items.Add(new ToolStripSeparator());

        _activeScreenMenu = new ToolStripMenuItem("Toggle Active Screen");
        menu.Items.Add(_activeScreenMenu);

        _gridSizeMenu = new ToolStripMenuItem("Grid Size");
        _gridSizeMenu.DropDownItems.Add(MakeSpacingItem("Small", Settings.SmallSpacing));
        _gridSizeMenu.DropDownItems.Add(MakeSpacingItem("Medium", Settings.MediumSpacing));
        _gridSizeMenu.DropDownItems.Add(MakeSpacingItem("Large", Settings.LargeSpacing));
        _gridSizeMenu.DropDownItems.Add(new ToolStripSeparator());
        _gridSizeMenu.DropDownItems.Add(new ToolStripMenuItem("Custom…", null, (_, _) => PromptCustomSpacing()));
        menu.Items.Add(_gridSizeMenu);

        _lineWidthMenu = new ToolStripMenuItem("Line Width");
        _lineWidthMenu.DropDownItems.Add(MakeWidthItem("Thin", 1));
        _lineWidthMenu.DropDownItems.Add(MakeWidthItem("Medium", 2));
        _lineWidthMenu.DropDownItems.Add(MakeWidthItem("Thick", 3));
        menu.Items.Add(_lineWidthMenu);

        menu.Items.Add(new ToolStripMenuItem("Line Color…", null, (_, _) => PickColor()));

        _majorLinesMenu = new ToolStripMenuItem("Major Lines");
        _majorLinesMenu.DropDownItems.Add(new ToolStripMenuItem("Off", null, (_, _) => SetMajorEvery(0)) { Tag = 0 });
        foreach (int n in new[] { 2, 4, 5, 8, 10 })
            _majorLinesMenu.DropDownItems.Add(new ToolStripMenuItem($"Every {n}th line", null, (_, _) => SetMajorEvery(n)) { Tag = n });
        _majorLinesMenu.DropDownItems.Add(new ToolStripSeparator());
        _majorLinesMenu.DropDownItems.Add(new ToolStripMenuItem("Color…", null, (_, _) => PickMajorColor()));
        menu.Items.Add(_majorLinesMenu);

        _centerLinesItem = new ToolStripMenuItem("Center Lines (red)", null, (_, _) => ToggleCenterLines());
        menu.Items.Add(_centerLinesItem);

        _opacityMenu = new ToolStripMenuItem("Opacity");
        foreach (int pct in new[] { 25, 50, 75, 100 })
            _opacityMenu.DropDownItems.Add(MakeOpacityItem(pct));
        menu.Items.Add(_opacityMenu);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(new ToolStripMenuItem("Hotkeys…", null, (_, _) => HotkeysDialog.Show(_trayIcon.Icon)));
        menu.Items.Add(new ToolStripMenuItem("About…", null, (_, _) => AboutDialog.Show(_trayIcon.Icon)));
        menu.Items.Add(new ToolStripMenuItem("Exit (Ctrl+Shift+End)", null, (_, _) => ExitApp()));

        RebuildActiveScreenMenu();
        return menu;
    }

    private ToolStripMenuItem MakeSpacingItem(string label, int spacing)
        => new(label, null, (_, _) => SetSpacing(spacing)) { Tag = spacing };

    private ToolStripMenuItem MakeWidthItem(string label, int width)
        => new(label, null, (_, _) => SetLineWidth(width)) { Tag = width };

    private ToolStripMenuItem MakeOpacityItem(int pct)
        => new($"{pct}%", null, (_, _) => SetOpacity(pct / 100.0)) { Tag = pct };

    private void RebuildActiveScreenMenu()
    {
        _activeScreenMenu.DropDownItems.Clear();

        var all = new ToolStripMenuItem("All Monitors", null, (_, _) => SetActiveScreen(-1)) { Tag = -1 };
        _activeScreenMenu.DropDownItems.Add(all);
        _activeScreenMenu.DropDownItems.Add(new ToolStripSeparator());

        Screen[] screens = Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            Screen sc = screens[i];
            string label = $"Monitor {i + 1} - {sc.Bounds.Width}×{sc.Bounds.Height}{(sc.Primary ? " (Primary)" : "")}";
            int index = i;
            _activeScreenMenu.DropDownItems.Add(
                new ToolStripMenuItem(label, null, (_, _) => SetActiveScreen(index)) { Tag = index });
        }
    }

    /// <summary>Re-derive every menu checkmark from the current settings.</summary>
    private void UpdateMenuChecks()
    {
        _visibleItem.Checked = _overlay.Visible;
        _centerLinesItem.Checked = _settings.ShowCenterLines;

        foreach (ToolStripItem item in _majorLinesMenu.DropDownItems)
            if (item is ToolStripMenuItem mi && mi.Tag is int n)
                mi.Checked = n == _settings.MajorLineEvery;

        foreach (ToolStripItem item in _gridSizeMenu.DropDownItems)
            if (item is ToolStripMenuItem mi && mi.Tag is int sp)
                mi.Checked = sp == _settings.GridSpacing;

        foreach (ToolStripItem item in _lineWidthMenu.DropDownItems)
            if (item is ToolStripMenuItem mi && mi.Tag is int w)
                mi.Checked = w == _settings.LineThickness;

        foreach (ToolStripItem item in _opacityMenu.DropDownItems)
            if (item is ToolStripMenuItem mi && mi.Tag is int pct)
                mi.Checked = Math.Abs(pct / 100.0 - _settings.Opacity) < 0.001;

        foreach (ToolStripItem item in _activeScreenMenu.DropDownItems)
            if (item is ToolStripMenuItem mi && mi.Tag is int idx)
                mi.Checked = idx == _settings.ActiveScreenIndex;
    }

    // ------------------------------------------------------------- actions ----

    private void ToggleVisible() => ShowOverlay(!_overlay.Visible);

    private void ShowOverlay(bool visible)
    {
        if (visible)
        {
            _overlay.FitToActiveScreen(_settings.ActiveScreenIndex);
            _overlay.Show();
            _overlay.ApplyExtendedStyles(); // re-assert click-through after Show
        }
        else
        {
            _overlay.Hide();
        }

        _settings.Visible = visible;
        _visibleItem.Checked = visible;
        Persist();
    }

    private void Redraw()
    {
        // Reset to the known starting grid size so the user always has a way back
        // to a predictable baseline, then re-fit and repaint.
        SetSpacing(Settings.DefaultSpacing);
        _overlay.FitToActiveScreen(_settings.ActiveScreenIndex);
        _overlay.Invalidate();
    }

    private void ToggleCenterLines()
    {
        _settings.ShowCenterLines = !_settings.ShowCenterLines;
        _overlay.ShowCenterLines = _settings.ShowCenterLines;
        _centerLinesItem.Checked = _settings.ShowCenterLines;
        Persist();
    }

    private void SetMajorEvery(int n)
    {
        _settings.MajorLineEvery = Math.Max(0, n);
        _overlay.MajorLineEvery = _settings.MajorLineEvery;
        UpdateMenuChecks();
        Persist();
    }

    private void PickMajorColor()
    {
        using var dlg = new ColorDialog
        {
            Color = _settings.MajorLineColor,
            FullOpen = true,
            AnyColor = true,
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _settings.MajorLineColor = dlg.Color;
            _overlay.MajorLineColor = dlg.Color;
            Persist();
        }
    }

    private void SetSpacing(int spacing)
    {
        _settings.GridSpacing = Math.Clamp(spacing, Settings.MinSpacing, Settings.MaxSpacing);
        _overlay.GridSpacing = _settings.GridSpacing;
        UpdateMenuChecks();
        Persist();
    }

    private void StepResolution(bool increase)
    {
        // Increase resolution = smaller spacing (more boxes); decrease = larger.
        int delta = increase ? -Settings.SpacingStep : Settings.SpacingStep;
        SetSpacing(_settings.GridSpacing + delta);
    }

    private void SetLineWidth(int width)
    {
        _settings.LineThickness = width;
        _overlay.LineThickness = width;
        UpdateMenuChecks();
        Persist();
    }

    private void ToggleLineWidth()
    {
        // Thin <-> thick toggle (Ctrl+Shift+Delete).
        int next = _settings.LineThickness >= Settings.ThickWidth ? Settings.ThinWidth : Settings.ThickWidth;
        SetLineWidth(next);
    }

    private void SetOpacity(double opacity)
    {
        _settings.Opacity = Math.Clamp(opacity, 0.05, 1.0);
        _overlay.Opacity = _settings.Opacity;
        _overlay.ApplyExtendedStyles(); // opacity change can rewrite ex-style
        UpdateMenuChecks();
        Persist();
    }

    private void SetActiveScreen(int index)
    {
        _settings.ActiveScreenIndex = index;
        _overlay.FitToActiveScreen(index);
        UpdateMenuChecks();
        Persist();
    }

    private void PickColor()
    {
        using var dlg = new ColorDialog
        {
            Color = _settings.LineColor,
            FullOpen = true,
            AnyColor = true,
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _settings.LineColor = dlg.Color;
            _overlay.LineColor = dlg.Color;
            Persist();
        }
    }

    private void PromptCustomSpacing()
    {
        int? value = InputDialog.PromptForInt(
            "Custom Grid Size",
            $"Grid spacing in pixels ({Settings.MinSpacing}-{Settings.MaxSpacing}):",
            _settings.GridSpacing, Settings.MinSpacing, Settings.MaxSpacing);
        if (value is int v)
            SetSpacing(v);
    }

    // ------------------------------------------------------------ plumbing ----

    private void OnHotKey(HotKeyAction action)
    {
        switch (action)
        {
            case HotKeyAction.ToggleVisible:      ToggleVisible(); break;
            case HotKeyAction.IncreaseResolution: StepResolution(increase: true); break;
            case HotKeyAction.DecreaseResolution: StepResolution(increase: false); break;
            case HotKeyAction.Redraw:             Redraw(); break;
            case HotKeyAction.ToggleLineWidth:    ToggleLineWidth(); break;
            case HotKeyAction.Exit:               ExitApp(); break;
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        RebuildActiveScreenMenu();
        UpdateMenuChecks();
        if (_overlay.Visible)
            _overlay.FitToActiveScreen(_settings.ActiveScreenIndex);
    }

    private void WarnIfHotkeysFailed()
    {
        if (_hotKeys.FailedRegistrations.Count == 0)
            return;

        string names = string.Join(", ", _hotKeys.FailedRegistrations);
        _trayIcon.ShowBalloonTip(
            5000,
            "GridOverlay",
            $"Some hotkeys are already in use and couldn't be registered: {names}. " +
            "Use the tray menu instead.",
            ToolTipIcon.Warning);
    }

    private void Persist() => _settings.Save();

    private void ExitApp()
    {
        Persist();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _hotKeys.HotKeyPressed -= OnHotKey;
        _hotKeys.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _overlay.Dispose();
        ExitThread();
    }

    /// <summary>Build a simple grid glyph icon at runtime (no binary asset needed).</summary>
    public static Icon CreateGridIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            using var pen = new Pen(Color.FromArgb(0, 200, 255), 2);
            for (int i = 0; i <= 32; i += 8)
            {
                g.DrawLine(pen, i, 0, i, 32);
                g.DrawLine(pen, 0, i, 32, i);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        using var temp = Icon.FromHandle(hIcon);
        return (Icon)temp.Clone();
    }
}
