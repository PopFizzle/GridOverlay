namespace GridOverlay;

/// <summary>
/// Application entry point. GridOverlay has no main window — it lives entirely in the
/// system tray, so <see cref="Main"/> just wires up WinForms and hands control to
/// <see cref="TrayAppContext"/>, which owns everything from there.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Applies the high-DPI mode and default fonts/styles configured in the .csproj.
        ApplicationConfiguration.Initialize();

        // Quick "show the About box and exit" path (e.g. `GridOverlay.exe --about`),
        // also handy for confirming the version from a script.
        if (args.Contains("--about", StringComparer.OrdinalIgnoreCase))
        {
            using var icon = TrayAppContext.CreateGridIcon();
            AboutDialog.Show(icon);
            return;
        }

        // Single-instance guard: if another copy already holds the named mutex, exit
        // quietly so two overlays don't stack on top of each other.
        using var mutex = new Mutex(true, "GridOverlay.SingleInstance", out bool isNew);
        if (!isNew)
            return;

        // Run the tray application until the user chooses Exit (or the End hotkey).
        Application.Run(new TrayAppContext());
    }
}
