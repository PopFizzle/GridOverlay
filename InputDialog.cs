namespace GridOverlay;

/// <summary>A tiny modal numeric prompt - WinForms has no built-in InputBox.</summary>
internal static class InputDialog
{
    /// <summary>
    /// Show a modal dialog with a numeric spinner and OK/Cancel.
    /// </summary>
    /// <param name="title">Dialog caption.</param>
    /// <param name="prompt">Label text above the spinner.</param>
    /// <param name="current">Initial value (clamped into range).</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <returns>The chosen value, or <c>null</c> if the user cancelled.</returns>
    public static int? PromptForInt(string title, string prompt, int current, int min, int max)
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            // Auto-size to content so labels and buttons never clip at any DPI.
            AutoScaleMode = AutoScaleMode.Font,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16),
        };

        var label = new Label
        {
            Text = prompt,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        var numeric = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(current, min, max),
            Width = 240,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, 8),
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(88, 30),
            Margin = new Padding(6, 0, 0, 0),
        };

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(88, 30),
            Margin = new Padding(6, 0, 0, 0),
        };

        // Buttons right-aligned: with RightToLeft flow, the first added sits rightmost,
        // so Cancel ends up on the right and OK to its left.
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 8, 0, 0),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
        };
        root.Controls.Add(label, 0, 0);
        root.Controls.Add(numeric, 0, 1);
        root.Controls.Add(buttons, 0, 2);

        form.Controls.Add(root);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog() == DialogResult.OK ? (int)numeric.Value : null;
    }
}
