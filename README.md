# GridOverlay

My EverQuest Legends lightweight Windows 10/11 tray application that draws a configurable,
click-through grid over the entire desktop.  An alignment aid for editing game UI layouts.

Built with C# / WinForms on **.NET 10**.

## Features

- **Transparent, click-through overlay** - borderless, always-on-top, layered window
  (`WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW`). Mouse and
  keyboard pass straight through to the game beneath it. Kept out of the alt-tab list.
- **Center-anchored grid** - the grid is anchored to the **center of each monitor** and
  works outward (not from the top-left), so it's easy to position UI elements relative to
  screen center. The center cross is drawn in **red** (toggleable) so the exact center is
  unmistakable, and every Nth line out from center is a bolder **major line** in its own
  color (gold by default) for reading off screen regions at a glance.
- **Multi-monitor & multi-resolution** - spans the full `VirtualScreen`, or target a
  single monitor via *Toggle Active Screen*. Each monitor gets its own centered grid,
  clipped to its bounds. Per-Monitor V2 DPI aware; re-fits on display changes.
- **System tray menu** - no main window. Right-click the tray icon for everything.
- **Global hotkeys** - work even while the game has focus.
- **Persistent settings** - saved to `%AppData%\GridOverlay\settings.json`.

## Tray menu

| Item | Action |
|------|--------|
| Visible | Show/hide the grid |
| Redraw + Reset Size | Re-fit, repaint, and reset grid size to the default (50px) baseline |
| Toggle Active Screen | Draw on all monitors or just one |
| Grid Size | Small (25px) / Medium (50px) / Large (100px) / Custom… |
| Line Width | Thin / Medium / Thick |
| Line Color… | Pick line color (ColorDialog) |
| Center Lines (red) | Toggle the red center cross |
| Major Lines | Every Nth line out from center is drawn bolder in its own color (Off/2/4/5/8/10 + Color…) |
| Opacity | 25% / 50% / 75% / 100% |
| Hotkeys… | Show the list of global hotkeys |
| About… | Show version, author (PopFizzle), and build date |
| Exit | Quit |

## Global hotkeys

| Hotkey | Action |
|--------|--------|
| Ctrl + Shift + Home | Toggle grid visibility |
| Ctrl + Shift + Page Up | Increase resolution (smaller cells) |
| Ctrl + Shift + Page Down | Decrease resolution (larger cells) |
| Ctrl + Shift + Insert | Redraw + reset grid size to default (50px) |
| Ctrl + Shift + Delete | Toggle thin / thick lines |
| Ctrl + Shift + End | Exit the application |

If a hotkey is already owned by another app it is skipped, and a tray balloon lists which
ones - use the menu for those.

## Build & run

Run these from the repository root (the folder containing `GridOverlay.csproj`):

```powershell
dotnet run
```

## Publish a portable single .exe

```powershell
dotnet publish -c Release -o publish
```

Produces a self-contained `publish\GridOverlay.exe` (no .NET install required on the
target machine). Copy it anywhere and run - no installer.

## Project layout

There is no main window; the app runs from the tray. The pieces:

| File | Role |
|------|------|
| `Program.cs` | Entry point. Sets up DPI/WinForms, enforces a single instance, and runs `TrayAppContext`. Also handles the `--about` CLI flag. |
| `TrayAppContext.cs` | The app's "controller". Owns the tray icon + menu, the overlay window, the hotkeys, and the settings. Every user action updates the settings, the overlay, the menu checkmarks, then saves. |
| `OverlayForm.cs` | The transparent, click-through window that actually draws the grid. Pure rendering - it holds no menu logic. |
| `HotKeyManager.cs` | Registers the global hotkeys and raises an event when one is pressed. Self-contained (its own hidden message window). |
| `NativeMethods.cs` | All the Win32 P/Invoke (window styles, `RegisterHotKey`) in one place. |
| `Settings.cs` | The persisted state model + load/save to `%AppData%\GridOverlay\settings.json`. |
| `AboutDialog.cs` / `InputDialog.cs` | Small modal dialogs (About box; numeric prompt for custom grid size). |
| `app.manifest` | Declares Per-Monitor V2 DPI awareness and Windows 10/11 support. |

### How the click-through overlay works

The overlay is a borderless, top-most, **layered** window. Two things make it usable as an
alignment aid:

1. **Only the grid lines are visible.** The whole form is painted in a "transparency key"
   color (`OverlayForm.TransparencyKeyColor`); Windows renders every pixel of that exact
   color as fully transparent, so only the lines you draw remain.
2. **Input passes straight through.** The `WS_EX_TRANSPARENT` extended style means mouse
   clicks and drags go to whatever window is underneath - so you can keep editing the game
   while the grid floats on top. `WS_EX_TOOLWINDOW` keeps it out of Alt-Tab, and
   `WS_EX_NOACTIVATE` stops it ever stealing focus. These are applied in
   `OverlayForm.ApplyExtendedStyles()`.

Line **opacity** is the whole-window alpha (`Form.Opacity`), which is why changing it can
make Windows rewrite the extended-style word - hence we re-assert the styles afterwards.

### How the grid is drawn

The grid is **anchored to the center of each monitor** and steps outward, so cells line up
symmetrically around screen center (see `OverlayForm.OnPaint` / `DrawGrid`). Each monitor's
grid is clipped to that monitor so adjacent displays don't bleed together. Three line types:
the minor grid (`LineColor`), bolder **major** lines every Nth step (`MajorLineColor`), and
a **center cross** (`CenterLineColor`, red by default).

## Notes

- Transparency uses a color key plus whole-window alpha (`Form.Opacity`) for adjustable
  line opacity.
- The app is Per-Monitor V2 DPI aware and works in raw physical pixels
  (`AutoScaleMode.None`), so grid positions map 1:1 to screen pixels on any monitor.
