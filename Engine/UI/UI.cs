using System.Collections.Concurrent;
using FontStashSharp;
using Graphite.Engine.Persistence;
using Graphite.Engine.UI.Audio;
using Graphite.Engine.UI.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

public static class UI
{
    private static Desktop? _desktop;
    private static Point _viewportSize;
    private static long _preferencesRevision = -1;
    private static float _userScale = 1;
    private static Func<SpriteFontBase, float, SpriteFontBase>? _fontResolver;
    internal static void SetFontResolver(Func<SpriteFontBase, float, SpriteFontBase> resolver) => _fontResolver = resolver;
    /// <summary>Logical authoring resolution used by automatic viewport scaling.</summary>
    public static Point ReferenceSize => new(1600, 900);
    /// <summary>Current viewport in logical UI units. Do not use physical pixels for widget layout.</summary>
    public static Point LayoutSize
    {
        get; private set;
    }
    /// <summary>Effective physical pixels per UI unit, including RuntimePreferences.UiScale.</summary>
    public static float Scale { get; private set; } = 1;
    /// <summary>Preference bindings refresh on the UI thread, independently of resizing.</summary>
    public static event Action? PreferencesChanged;
    private static readonly List<UIMaterialHost> Hosts = [];
    private static readonly List<UIScreen> Screens = [];
    private static readonly ConcurrentQueue<Action> Pending = new();
    public static void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Pending.Enqueue(action);
    }
    private static UIMaterialRenderer? _renderer;
    public static UIInteractionDefaults Interactions { get; } = new();
    public static IUIAudioService Audio { get; private set; } = new UIAudioService();
    internal static GraphicsDevice GraphicsDevice => MyraEnvironment.Game.GraphicsDevice;
    internal static UIMaterialRenderer MaterialRenderer => _renderer ?? throw new InvalidOperationException("UI is not initialized.");
    internal static int ViewportWidth => GraphicsDevice.PresentationParameters.BackBufferWidth;
    internal static int ViewportHeight => GraphicsDevice.PresentationParameters.BackBufferHeight;
    internal static int HostCount => Hosts.Count;
    internal static void Register(UIMaterialHost host) => Hosts.Add(host);
    internal static void Unregister(UIMaterialHost host) => Hosts.Remove(host);

    public static void SetAudioService(IUIAudioService audio)
    {
        ArgumentNullException.ThrowIfNull(audio);
        if (Hosts.Count != 0)
        {
            throw new InvalidOperationException("Set the UI audio service before constructing hosts.");
        }

        if (ReferenceEquals(Audio, audio))
        {
            return;
        }

        Audio.Dispose();
        Audio = audio;
    }

    internal static void Initialize(Microsoft.Xna.Framework.Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        Shutdown();
        MyraEnvironment.Game = game;
        // Fractional desktop scales need filtered glyphs rather than nearest-neighbor text.
        MyraEnvironment.SmoothText = true;
        _desktop = new Desktop
        {
            TransformOrigin = Vector2.Zero,
            BoundsFetcher = () => new Rectangle(Point.Zero, LayoutSize)
        };
        _renderer = new UIMaterialRenderer(game.GraphicsDevice);
        Audio = new UIAudioService();
        RefreshLayout();
    }

    public static T Open<T>() where T : UIScreen, new()
    {
        RefreshLayout();
        var existingHosts = Hosts.ToHashSet();
        try
        {
            var screen = new T();
            screen.OpenInternal();
            Screens.Add(screen);
            return screen;
        }
        catch
        {
            foreach (var host in Hosts.Where(host => !existingHosts.Contains(host)).ToArray())
            {
                host.Dispose();
            }

            throw;
        }
    }

    public static void Close(UIScreen screen, bool immediate = false) => screen.CloseInternal(immediate);

    internal static void Attach(Widget root)
    {
        var desktop = _desktop
            ?? throw new InvalidOperationException("Graphite UI has not been initialized.");
        desktop.Widgets.Add(root);
    }

    internal static void Detach(Widget root)
    {
        _desktop?.Widgets.Remove(root);
    }

    internal static IReadOnlyList<UIMaterialHost> ExitHostsWithin(Widget root)
    {
        var hosts = HostsWithin(root);
        return hosts.Where(host => !hosts.Any(parent => !ReferenceEquals(parent, host) && IsWithin(host, parent))).ToArray();
    }
    internal static IReadOnlyList<UIMaterialHost> HostsWithin(Widget root)
        => Hosts.Where(host => IsWithin(host, root)).ToArray();
    private static bool IsWithin(Widget widget, Widget root)
    {
        for (Widget? current = widget; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, root))
            {
                return true;
            }
        }

        return false;
    }
    internal static void Update(float dt)
    {
        if (_desktop is null)
        {
            return;
        }

        while (Pending.TryDequeue(out var action))
        {
            action();
        }

        RefreshLayout();
        MyraInput.Update(_desktop);
        foreach (var host in Hosts.ToArray())
        {
            host.Update(dt);
        }

        foreach (var screen in Screens.ToArray())
        {
            screen.UpdateClose();
        }

        Screens.RemoveAll(screen => !screen.IsOpen);
        Audio.Update();
        RefreshLayout();
        _desktop.UpdateLayout();
    }

    internal static void Draw()
    {
        RefreshLayout();
        _desktop?.UpdateLayout();
        _desktop?.RenderVisual();
    }

    internal static void RefreshLayout()
    {
        if (_desktop is null)
        {
            return;
        }

        var preferencesChanged = _preferencesRevision != Preferences.Revision;
        if (preferencesChanged)
        {
            _userScale = Preferences.IsInitialized ? RuntimePreferences.GetUiScale() : 1;
            _preferencesRevision = Preferences.Revision;
        }

        var viewport = new Point(ViewportWidth, ViewportHeight);
        if (viewport.X <= 0 || viewport.Y <= 0)
        {
            return;
        }

        var scale = Math.Min(viewport.X / (float)ReferenceSize.X, viewport.Y / (float)ReferenceSize.Y) * _userScale;
        if (viewport != _viewportSize || scale != Scale)
        {
            _viewportSize = viewport;
            Scale = scale;
            LayoutSize = new Point((int)MathF.Ceiling(viewport.X / scale), (int)MathF.Ceiling(viewport.Y / scale));
            // Myra's measure/arrange pass fits declared sizes inside parents and transforms input globally.
            _desktop.Scale = new Vector2(scale);
            _desktop.InvalidateLayout();
            foreach (var screen in Screens.ToArray())
            {
                screen.NotifyLayoutChanged();
            }
        }
        if (preferencesChanged)
        {
            PreferencesChanged?.Invoke();
        }
        RefreshFonts();
    }

    private static void RefreshFonts()
    {
        var resolve = _fontResolver;
        if (_desktop is null || resolve is null)
        {
            return;
        }
        // Include newly opened popups and dynamically added labels even when scale is unchanged.
        foreach (var root in _desktop.Widgets)
        {
            Refresh(root);
            foreach (var child in root.GetChildren(true))
            {
                Refresh(child);
            }
        }

        void Refresh(Widget widget)
        {
            switch (widget)
            {
                case Label { Font: { } font } label:
                    var labelFont = resolve(font, Scale);
                    if (!ReferenceEquals(label.Font, labelFont))
                    {
                        label.Font = labelFont;
                    }
                    break;
                case TextBox { Font: { } font } textBox:
                    var inputFont = resolve(font, Scale);
                    if (!ReferenceEquals(textBox.Font, inputFont))
                    {
                        textBox.Font = inputFont;
                    }
                    break;
            }
        }
    }

    internal static void Shutdown()
    {
        foreach (var screen in Screens.ToArray())
        {
            screen.CloseInternal(true);
        }

        Screens.Clear();
        foreach (var host in Hosts.ToArray())
        {
            host.Dispose();
        }

        Hosts.Clear();
        _renderer?.Dispose();
        _renderer = null;
        Audio.Dispose();
        Interactions.Clear();
        Pending.Clear();
        _desktop?.Dispose();
        _desktop = null;
        _viewportSize = LayoutSize = Point.Zero;
        _preferencesRevision = -1;
        _userScale = Scale = 1;
        PreferencesChanged = null;
        _fontResolver = null;
    }
}
