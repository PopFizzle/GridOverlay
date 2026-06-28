namespace GridOverlay;

/// <summary>A tiny modal numeric prompt — WinForms has no built-in InputBox.</summary>
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
            ClientSize = new Size(300, 110),
            TopMost = true,
        };

        var label = new Label { Left = 12, Top = 12, Width = 276, Text = prompt };
        var numeric = new NumericUpDown
        {
            Left = 12,
            Top = 38,
            Width = 276,
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(current, min, max),
        };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 132, Top = 72, Width = 70 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 218, Top = 72, Width = 70 };

        form.Controls.Add(label);
        form.Controls.Add(numeric);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog() == DialogResult.OK ? (int)numeric.Value : null;
    }
}
