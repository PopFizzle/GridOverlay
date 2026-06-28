using System.Text.Json;
using System.Text.Json.Serialization;

namespace GridOverlay;

/// <summary>
/// User-configurable state, persisted to %AppData%\GridOverlay\settings.json so
/// the overlay comes back the way the user left it.
/// </summary>
public sealed class Settings
{
    // Spacing presets (pixels) surfaced in the Grid Size menu.
    public const int SmallSpacing = 25;
    public const int MediumSpacing = 50;
    public const int LargeSpacing = 100;

    /// <summary>The "starting" grid size Redraw resets to, so the user can always get a known baseline.</summary>
    public const int DefaultSpacing = MediumSpacing;

    public const int MinSpacing = 5;
    public const int MaxSpacing = 500;
    public const int SpacingStep = 5;

    public const int ThinWidth = 1;
    public const int ThickWidth = 3;

    public int GridSpacing { get; set; } = DefaultSpacing;
    public int LineThickness { get; set; } = ThinWidth;

    /// <summary>Line color stored as ARGB so System.Text.Json can round-trip it.</summary>
    public int LineColorArgb { get; set; } = unchecked((int)0xFF00C8FF); // cyan

    /// <summary>Whole-overlay opacity, 0.05 .. 1.0. Defaults to 25% to keep the grid subtle.</summary>
    public double Opacity { get; set; } = 0.25;

    public bool Visible { get; set; } = true;

    /// <summary>Draw the per-monitor center cross (vertical + horizontal) in a distinct color.</summary>
    public bool ShowCenterLines { get; set; } = true;

    /// <summary>Every Nth line out from center is drawn in <see cref="MajorLineColor"/>. 0 disables.</summary>
    public int MajorLineEvery { get; set; } = 5;

    /// <summary>Color of the major (every-Nth) lines, stored as ARGB. Defaults to gold.</summary>
    public int MajorLineColorArgb { get; set; } = unchecked((int)0xFFFFC800);

    /// <summary>Color of the center cross, stored as ARGB. Defaults to red.</summary>
    public int CenterLineColorArgb { get; set; } = unchecked((int)0xFFFF0000);

    /// <summary>Index into <see cref="System.Windows.Forms.Screen.AllScreens"/>, or -1 for "all monitors".</summary>
    public int ActiveScreenIndex { get; set; } = -1;

    // Colors are persisted as ARGB ints (the *Argb properties above) because that
    // round-trips cleanly through System.Text.Json. These [JsonIgnore] wrappers expose a
    // convenient System.Drawing.Color view of each for the rest of the app, and are not
    // themselves serialized.

    [JsonIgnore]
    public Color LineColor
    {
        get => Color.FromArgb(LineColorArgb);
        set => LineColorArgb = value.ToArgb();
    }

    [JsonIgnore]
    public Color CenterLineColor
    {
        get => Color.FromArgb(CenterLineColorArgb);
        set => CenterLineColorArgb = value.ToArgb();
    }

    [JsonIgnore]
    public Color MajorLineColor
    {
        get => Color.FromArgb(MajorLineColorArgb);
        set => MajorLineColorArgb = value.ToArgb();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Full path to the settings file: <c>%AppData%\GridOverlay\settings.json</c>.</summary>
    public static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GridOverlay");
            return Path.Combine(dir, "settings.json");
        }
    }

    /// <summary>
    /// Load settings from disk, or return defaults if the file is missing, unreadable, or
    /// corrupt. Never throws - a bad file should not stop the app from starting.
    /// </summary>
    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                Settings? s = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
                if (s is not null)
                {
                    s.Clamp();
                    return s;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable settings fall back to defaults.
        }

        return new Settings();
    }

    /// <summary>Write the current settings to disk (best-effort; never throws).</summary>
    public void Save()
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Best-effort persistence; never crash the app over settings I/O.
        }
    }

    /// <summary>Force loaded values into valid ranges, in case the file was hand-edited.</summary>
    private void Clamp()
    {
        GridSpacing = Math.Clamp(GridSpacing, MinSpacing, MaxSpacing);
        LineThickness = Math.Clamp(LineThickness, 1, 10);
        Opacity = Math.Clamp(Opacity, 0.05, 1.0);
        MajorLineEvery = Math.Clamp(MajorLineEvery, 0, 100);
    }
}
