namespace GridOverlay;

/// <summary>
/// A small modal dialog listing every global hotkey and what it does, so users have an
/// in-app reminder instead of having to remember them. The content comes from
/// <see cref="HotKeyManager.Help"/>, which sits next to the actual key bindings.
/// </summary>
internal static class HotkeysDialog
{
    public static void Show(Icon? appIcon)
    {
        using var form = new Form
        {
            Text = "GridOverlay Hotkeys",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            Icon = appIcon,
            // Auto-size to content so the list never clips at any DPI.
            AutoScaleMode = AutoScaleMode.Font,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16),
        };

        // Two columns: the key combo (bold) and what it does.
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0),
        };

        var boldFont = new Font(SystemFonts.MessageBoxFont!, FontStyle.Bold);
        foreach ((string keys, string action) in HotKeyManager.Help)
        {
            grid.Controls.Add(new Label
            {
                Text = keys,
                AutoSize = true,
                Font = boldFont,
                Margin = new Padding(0, 3, 24, 3),
            });
            grid.Controls.Add(new Label
            {
                Text = action,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 3),
            });
        }

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(92, 30),
            Anchor = AnchorStyles.None, // centered in its cell
            Margin = new Padding(0, 16, 0, 0),
        };

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        root.Controls.Add(grid, 0, 0);
        root.Controls.Add(ok, 0, 1);

        form.Controls.Add(root);
        form.AcceptButton = ok;
        form.CancelButton = ok;

        form.ShowDialog();
    }
}
