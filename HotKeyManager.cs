namespace GridOverlay;

/// <summary>The distinct actions bound to global hotkeys.</summary>
public enum HotKeyAction
{
    ToggleVisible,       // Ctrl+Shift+Home
    IncreaseResolution,  // Ctrl+Shift+PageUp  (more, smaller boxes)
    DecreaseResolution,  // Ctrl+Shift+PageDown (fewer, larger boxes)
    Redraw,              // Ctrl+Shift+Insert
    ToggleLineWidth,     // Ctrl+Shift+Delete  (thin <-> thick)
    Exit,                // Ctrl+Shift+End
}

/// <summary>
/// Registers system-wide hotkeys via RegisterHotKey and raises
/// <see cref="HotKeyPressed"/> when one fires — works even while a game has focus.
/// Hosts its own hidden message-only window so it doesn't depend on the overlay.
/// </summary>
public sealed class HotKeyManager : IDisposable
{
    // A hidden window whose only job is to receive WM_HOTKEY messages. RegisterHotKey
    // needs a window handle to deliver to; using our own keeps hotkeys independent of
    // the overlay (they work even while the overlay is hidden).
    private sealed class MessageWindow : NativeWindow
    {
        private readonly Action<int> _onHotKey;

        public MessageWindow(Action<int> onHotKey)
        {
            _onHotKey = onHotKey;
            CreateHandle(new CreateParams()); // hidden window, just to pump messages
        }

        protected override void WndProc(ref Message m)
        {
            // WM_HOTKEY's wParam is the id we passed to RegisterHotKey.
            if (m.Msg == NativeMethods.WM_HOTKEY)
                _onHotKey(m.WParam.ToInt32());

            base.WndProc(ref m);
        }
    }

    // The hotkey table: which virtual key triggers which action. The registration id is
    // simply the action's enum value, so WM_HOTKEY's id maps straight back to an action.
    // All bindings use Ctrl+Shift (see Modifiers below).
    private static readonly (HotKeyAction Action, uint Vk)[] Bindings =
    [
        (HotKeyAction.ToggleVisible,      (uint)Keys.Home),
        (HotKeyAction.IncreaseResolution, (uint)Keys.PageUp),
        (HotKeyAction.DecreaseResolution, (uint)Keys.PageDown),
        (HotKeyAction.Redraw,             (uint)Keys.Insert),
        (HotKeyAction.ToggleLineWidth,    (uint)Keys.Delete),
        (HotKeyAction.Exit,               (uint)Keys.End),
    ];

    private const uint Modifiers =
        NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT;

    /// <summary>
    /// Human-readable hotkey reference, shown in the in-app "Hotkeys" dialog. Kept here
    /// next to the bindings so the displayed list and the registered keys stay in step.
    /// </summary>
    public static readonly (string Keys, string Action)[] Help =
    [
        ("Ctrl + Shift + Home",      "Toggle grid visibility"),
        ("Ctrl + Shift + Page Up",   "Increase resolution (smaller cells)"),
        ("Ctrl + Shift + Page Down", "Decrease resolution (larger cells)"),
        ("Ctrl + Shift + Insert",    "Redraw + reset grid size to default"),
        ("Ctrl + Shift + Delete",    "Toggle thin / thick lines"),
        ("Ctrl + Shift + End",       "Exit the application"),
    ];

    private readonly MessageWindow _window;
    private readonly List<int> _registered = [];

    public event Action<HotKeyAction>? HotKeyPressed;

    /// <summary>Hotkeys that failed to register (e.g. already owned by another app).</summary>
    public IReadOnlyList<HotKeyAction> FailedRegistrations { get; }

    public HotKeyManager()
    {
        _window = new MessageWindow(OnHotKey);

        var failed = new List<HotKeyAction>();
        foreach ((HotKeyAction action, uint vk) in Bindings)
        {
            int id = (int)action;
            if (NativeMethods.RegisterHotKey(_window.Handle, id, Modifiers, vk))
                _registered.Add(id);
            else
                failed.Add(action);
        }
        FailedRegistrations = failed;
    }

    private void OnHotKey(int id)
    {
        if (Enum.IsDefined(typeof(HotKeyAction), id))
            HotKeyPressed?.Invoke((HotKeyAction)id);
    }

    public void Dispose()
    {
        foreach (int id in _registered)
            NativeMethods.UnregisterHotKey(_window.Handle, id);
        _registered.Clear();
        _window.DestroyHandle();
    }
}
