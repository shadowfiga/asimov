using Graphite.Engine.Configuration;
using Graphite.Engine.Persistence;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Graphics;

public enum WindowMode
{
    Windowed, BorderlessFullscreen, Fullscreen
}

public readonly record struct DisplayConfiguration(WindowMode Mode, Point Resolution)
{
    public override string ToString() => $"{Mode}:{Resolution.X}x{Resolution.Y}";

    internal static bool TryParse(string value, out DisplayConfiguration configuration)
    {
        configuration = default;
        var parts = value.Split([':', 'x']);
        if (parts.Length != 3 || !Enum.TryParse<WindowMode>(parts[0], out var mode) || !Enum.IsDefined(mode)
            || !int.TryParse(parts[1], out var width) || !int.TryParse(parts[2], out var height)
            || width is < 640 or > 16384 || height is < 360 or > 16384)
        {
            return false;
        }
        configuration = new(mode, new Point(width, height));
        return true;
    }
}

/// <summary>Game-wide display settings. Preview changes are saved only after confirmation.</summary>
public static class DisplaySettings
{
    private static Microsoft.Xna.Framework.Game? _game;
    private static GraphicsDeviceManager? _graphics;
    private static DisplayConfiguration _defaults;
    private static DisplayConfiguration? _requested, _previous;
    private static Point[] _supported = [];
    private static bool _resizable;
    private static float _remaining;
    private static long _preferencesRevision;
    private static string _saved = "";
    public static DisplayConfiguration Current
    {
        get; private set;
    }
    public static Point DesktopResolution
    {
        get; private set;
    }
    public static bool IsInitialized => _graphics is not null;
    public static bool NeedsConfirmation => _previous.HasValue;
    public static int SecondsRemaining => Math.Max(0, (int)MathF.Ceiling(_remaining));
    public static string? Error
    {
        get; private set;
    }
    public static event Action? Changed;

    internal static void Initialize(Microsoft.Xna.Framework.Game game, GraphicsDeviceManager graphics, WindowSettings defaults)
    {
        Shutdown();
        _game = game;
        _graphics = graphics;
        _resizable = defaults.Resizable;
        var adapter = graphics.GraphicsDevice.Adapter;
        DesktopResolution = new Point(adapter.CurrentDisplayMode.Width, adapter.CurrentDisplayMode.Height);
        _supported = adapter.SupportedDisplayModes.Select(mode => new Point(mode.Width, mode.Height))
            .Where(size => size.X >= 640 && size.Y >= 360).Distinct().OrderBy(size => size.X).ThenBy(size => size.Y).ToArray();
        _defaults = new(defaults.Fullscreen
            ? defaults.Borderless ? WindowMode.BorderlessFullscreen : WindowMode.Fullscreen
            : WindowMode.Windowed, new Point(defaults.Width, defaults.Height));
        _saved = Preferences.Get(RuntimePreferences.Display);
        _preferencesRevision = Preferences.Revision;
        var desired = DisplayConfiguration.TryParse(_saved, out var saved) ? saved : _defaults;
        Apply(Normalize(desired));
    }

    public static IReadOnlyList<Point> Resolutions(WindowMode mode)
    {
        if (mode == WindowMode.BorderlessFullscreen)
        {
            return [DesktopResolution];
        }
        if (mode == WindowMode.Fullscreen)
        {
            return _supported;
        }
        Point[] common =
        [
            new(800, 600),
            new(1024, 768),
            new(1280, 720),
            new(1366, 768),
            new(1600, 900),
            new(1920, 1080),
            new(2560, 1440),
            new(3840, 2160)
        ];
        return common.Concat(_supported).Append(Current.Resolution).Append(_defaults.Resolution)
            .Where(size => size.X >= 640 && size.Y >= 360 && size.X <= DesktopResolution.X && size.Y <= DesktopResolution.Y)
            .Distinct().OrderBy(size => size.X).ThenBy(size => size.Y).ToArray();
    }

    public static void Preview(WindowMode mode, Point resolution)
    {
        if (_graphics is null)
        {
            throw new InvalidOperationException("Display settings are not initialized.");
        }
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
        if (resolution.X is < 640 or > 16384 || resolution.Y is < 360 or > 16384)
        {
            throw new ArgumentOutOfRangeException(nameof(resolution));
        }
        _requested = Normalize(new(mode, resolution));
    }

    public static void KeepChanges()
    {
        if (!NeedsConfirmation)
        {
            return;
        }
        try
        {
            Preferences.Set(RuntimePreferences.Display, Current.ToString());
            _saved = Preferences.Get(RuntimePreferences.Display);
            _preferencesRevision = Preferences.Revision;
            _previous = null;
            _remaining = 0;
            Error = null;
            Changed?.Invoke();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Revert();
            Error = "DISPLAY SETTINGS COULD NOT BE SAVED";
            Changed?.Invoke();
        }
    }

    public static void Revert()
    {
        _requested = null;
        if (_previous is not { } previous)
        {
            return;
        }
        _previous = null;
        _remaining = 0;
        Apply(previous);
        Changed?.Invoke();
    }

    internal static void Update(float dt)
    {
        if (_graphics is null)
        {
            return;
        }
        if (_preferencesRevision != Preferences.Revision)
        {
            _preferencesRevision = Preferences.Revision;
            var saved = Preferences.Get(RuntimePreferences.Display);
            if (saved != _saved)
            {
                _saved = saved;
                _previous = _requested = null;
                _remaining = 0;
                Apply(Normalize(DisplayConfiguration.TryParse(saved, out var config) ? config : _defaults));
                Changed?.Invoke();
            }
        }
        if (_requested is { } requested)
        {
            _requested = null;
            if (requested != Current)
            {
                var previous = Current;
                try
                {
                    Apply(requested);
                    _previous ??= previous;
                    _remaining = 15;
                    Error = null;
                }
                catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
                {
                    Apply(previous);
                    Error = "DISPLAY MODE IS NOT AVAILABLE";
                }
                Changed?.Invoke();
            }
        }
        else if (NeedsConfirmation)
        {
            var seconds = SecondsRemaining;
            _remaining -= dt;
            if (_remaining <= 0)
            {
                Revert();
            }
            else if (SecondsRemaining != seconds)
            {
                Changed?.Invoke();
            }
        }

        var actual = new Point(_graphics.GraphicsDevice.PresentationParameters.BackBufferWidth,
            _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight);
        if (actual != Current.Resolution)
        {
            Current = Current with
            {
                Resolution = actual
            };
            Changed?.Invoke();
        }
    }

    private static DisplayConfiguration Normalize(DisplayConfiguration desired)
    {
        var available = Resolutions(desired.Mode);
        var resolution = available.Contains(desired.Resolution) ? desired.Resolution
            : available.OrderBy(size => Math.Abs(size.X - desired.Resolution.X) + Math.Abs(size.Y - desired.Resolution.Y))
                .FirstOrDefault(DesktopResolution);
        return desired with
        {
            Resolution = resolution
        };
    }

    private static void Apply(DisplayConfiguration configuration)
    {
        var graphics = _graphics!;
        graphics.HardwareModeSwitch = configuration.Mode == WindowMode.Fullscreen;
        graphics.IsFullScreen = configuration.Mode != WindowMode.Windowed;
        _game!.Window.IsBorderless = configuration.Mode == WindowMode.BorderlessFullscreen;
        _game.Window.AllowUserResizing = _resizable && configuration.Mode == WindowMode.Windowed;
        graphics.PreferredBackBufferWidth = configuration.Resolution.X;
        graphics.PreferredBackBufferHeight = configuration.Resolution.Y;
        graphics.ApplyChanges();
        var presentation = graphics.GraphicsDevice.PresentationParameters;
        Current = configuration with
        {
            Resolution = new Point(presentation.BackBufferWidth, presentation.BackBufferHeight)
        };
        Graphite.Engine.UI.UI.RefreshLayout();
    }

    internal static void Shutdown()
    {
        _game = null;
        _graphics = null;
        _requested = _previous = null;
        _supported = [];
        _remaining = 0;
        _saved = "";
        Error = null;
        DesktopResolution = Point.Zero;
        Current = default;
        Changed = null;
    }
}
