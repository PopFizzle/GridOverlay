using System.Reflection;

namespace GridOverlay;

/// <summary>A small "About" box showing app name, version, author, and date.</summary>
internal static class AboutDialog
{
    public const string Author = "PopFizzle";

    public static void Show(Icon? appIcon)
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        string date = BuildDate().ToString("yyyy-MM-dd");

        var form = new Form
        {
            Text = "About GridOverlay",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            Icon = appIcon,
            // Fixed layout in raw pixels (the width that comfortably fit the icon
            // + text). The OK button auto-sizes so its text never clips at any DPI.
            AutoScaleMode = AutoScaleMode.None,
            ClientSize = new Size(340, 200),
        };

        var iconBox = new PictureBox
        {
            Image = appIcon?.ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Left = 18,
            Top = 22,
            Width = 56,
            Height = 56,
        };

        var title = new Label
        {
            Text = "GridOverlay",
            AutoSize = true,
            Left = 86,
            Top = 20,
            Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 13f, FontStyle.Bold),
        };

        var details = new Label
        {
            Text = $"Version {version}\r\nAuthor: {Author}\r\nDate: {date}",
            AutoSize = true,
            Left = 88,
            Top = 54,
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            AutoSize = true,                       // grows to fit text -> never clips
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(92, 30),
            Anchor = AnchorStyles.Bottom,
        };

        form.Controls.Add(iconBox);
        form.Controls.Add(title);
        form.Controls.Add(details);
        form.Controls.Add(ok);
        form.AcceptButton = ok;
        form.CancelButton = ok;

        // Center the OK button horizontally and pin it near the bottom, once its
        // auto-sized dimensions are known.
        form.Load += (_, _) =>
        {
            ok.Left = (form.ClientSize.Width - ok.Width) / 2;
            ok.Top = form.ClientSize.Height - ok.Height - 16;
        };

        form.ShowDialog();
        form.Dispose();
    }

    /// <summary>Best-effort build/publish date from the executable's timestamp.</summary>
    private static DateTime BuildDate()
    {
        try
        {
            string? path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                return File.GetLastWriteTime(path);
        }
        catch
        {
            // fall through
        }
        return DateTime.Now;
    }
}
