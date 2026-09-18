using System.Globalization;
using Chisel.Generated;
using Graphite.Engine.UI;
using Graphite.Engine.UI.Theming;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Run;
using Graphite.Game.Sessions;
using Graphite.Game.UI;
using Graphite.Game.UI.Hud;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Graphite.Game.Scenes;

public sealed class SandboxUI : UIScreen
{
    private readonly MenuAssets _assets = new();
    private Run _run = null!;
    private Texture2D _mapTexture = null!;
    private Label _experience = null!;
    private Label _timer = null!;
    private Label _healthValue = null!;
    private HorizontalProgressBar _healthBar = null!;
    private HealthComponent? _health;
    private int _shownExperience = -1;
    private int _shownSeconds = -1;
    internal ResourceHud Resources { get; private set; } = null!;

    protected override Widget Build()
    {
        var theme = GameThemes.DeepDrive;
        var spacing = theme.Spacing;
        _run = SessionManager.ActiveSession.CurrentRun;
        var root = new Panel(styleName: "root")
        {
            Background = null,
            Padding = new Thickness(spacing.Lg, spacing.Lg, spacing.Lg, spacing.Xl * 2)
        };
        Resources = new ResourceHud(_run)
        {
            Id = "hud-ore",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
        root.Widgets.Add(Resources);
        root.Widgets.Add(BuildExperience());
        root.Widgets.Add(BuildMap());
        root.Widgets.Add(BuildPilot());
        Refresh();
        return root;
    }

    private Widget BuildExperience()
    {
        var theme = GameThemes.DeepDrive;
        var content = new VerticalStackPanel
        {
            Id = "hud-experience",
            Width = theme.Layout.HudExperienceWidth,
            Spacing = theme.Spacing.Sm,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        };
        var frame = Surface();
        _experience = Text("0 XP", theme.ResourceHud.LabelFontSize, theme.SecondaryText);
        frame.Widgets.Add(Readout("EXPERIENCE", _experience));
        // Placeholder only: no level curve or upgrade-card system is defined yet.
        frame.Widgets.Add(Bar("hud-xp-bar", 0));
        content.Widgets.Add(frame);
        _timer = Text("10:00", theme.Layout.HudTimerFontSize, theme.PrimaryText);
        _timer.Id = "hud-timer";
        _timer.HorizontalAlignment = HorizontalAlignment.Center;
        content.Widgets.Add(_timer);
        return content;
    }

    private Widget BuildPilot()
    {
        var theme = GameThemes.DeepDrive;
        var frame = new Grid
        {
            Id = "hud-pilot",
            Width = theme.Layout.HudPilotWidth,
            Padding = new Thickness(theme.Spacing.Md),
            ColumnSpacing = theme.Spacing.Md,
            Background = SurfaceBrush(),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        frame.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, theme.Layout.HudPortraitSize));
        frame.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        var portrait = new Panel
        {
            Id = "hud-portrait",
            Width = theme.Layout.HudPortraitSize,
            Height = theme.Layout.HudPortraitSize,
            Background = new SolidBrush(theme.ControlSurface)
        };
        // Use a bundled icon until the selected pilot has portrait artwork.
        var icon = _assets.Icon("users", theme.Layout.HudPortraitSize - theme.Spacing.Lg, theme.SecondaryText);
        icon.OverRenderable = icon.FocusedRenderable = icon.PressedRenderable = icon.Renderable;
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        portrait.Widgets.Add(icon);
        frame.Widgets.Add(portrait);
        var details = new VerticalStackPanel { Spacing = theme.Spacing.Sm, VerticalAlignment = VerticalAlignment.Center };
        var pilot = SessionManager.ActiveSession.CurrentLoadout.PilotId;
        details.Widgets.Add(Text(ChiselPilot.DisplayName[pilot.ToInt()].ToUpperInvariant(), theme.Layout.HudTitleFontSize, theme.PrimaryText));
        var health = new VerticalStackPanel { Spacing = theme.Spacing.Xs };
        _healthValue = Text("100%", theme.ResourceHud.LabelFontSize, theme.SecondaryText);
        _healthValue.Id = "hud-hp-value";
        health.Widgets.Add(Readout("HP", _healthValue));
        _healthBar = Bar("hud-hp-bar", 1);
        health.Widgets.Add(_healthBar);
        details.Widgets.Add(health);
        Grid.SetColumn(details, 1);
        frame.Widgets.Add(details);
        return frame;
    }

    private Widget BuildMap()
    {
        var theme = GameThemes.DeepDrive;
        var content = Surface();
        content.Id = "hud-map";
        content.Width = theme.Layout.HudMapSize + theme.Spacing.Md * 2;
        content.HorizontalAlignment = HorizontalAlignment.Left;
        content.VerticalAlignment = VerticalAlignment.Bottom;
        var title = Text("SANDBOX", theme.Layout.HudTitleFontSize, theme.PrimaryText);
        title.Id = "hud-map-name";
        content.Widgets.Add(title);
        content.Widgets.Add(new HorizontalSeparator());
        _mapTexture = CreateMapTexture(theme.Layout.HudMapSize);
        content.Widgets.Add(new Image
        {
            Id = "hud-minimap",
            Renderable = new TextureRegion(_mapTexture),
            Width = theme.Layout.HudMapSize,
            Height = theme.Layout.HudMapSize
        });
        return content;
    }

    private static Texture2D CreateMapTexture(int size)
    {
        // Fixed map sketch, not world discovery, navigation or a live radar.
        string[] cells =
        [
            "............",
            "...####.....",
            "..######....",
            "..#######...",
            "...######...",
            "...#######..",
            "..########..",
            ".#########..",
            "..#######...",
            "...####.....",
            "....##......",
            "............"
        ];
        var theme = GameThemes.DeepDrive;
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var row = y * cells.Length / size;
                var col = x * cells[row].Length / size;
                var floor = cells[row][col] == '#';
                var grid = x * cells.Length % size < cells.Length || y * cells.Length % size < cells.Length;
                pixels[y * size + x] = grid ? theme.Border : floor ? theme.ControlSurface : theme.DeepBlack;
                // A central player cross is the only marker in the stub.
                if ((Math.Abs(x - size / 2) <= 1 && Math.Abs(y - size / 2) <= 5)
                    || (Math.Abs(y - size / 2) <= 1 && Math.Abs(x - size / 2) <= 5))
                {
                    pixels[y * size + x] = theme.PrimaryText;
                }
            }
        }
        var texture = new Texture2D(Myra.MyraEnvironment.GraphicsDevice, size, size);
        texture.SetData(pixels);
        return texture;
    }

    private static VerticalStackPanel Surface() => new()
    {
        Padding = new Thickness(GameThemes.DeepDrive.Spacing.Md),
        Spacing = GameThemes.DeepDrive.Spacing.Sm,
        Background = SurfaceBrush()
    };

    private static RoundedRectangleBrush SurfaceBrush()
    {
        var theme = GameThemes.DeepDrive;
        return new RoundedRectangleBrush(new Color(theme.DeepBlack, theme.ResourceHud.SurfaceOpacity),
            theme.BorderRadius.Zero, theme.Border, 1);
    }

    private static Grid Readout(string name, Label value)
    {
        var theme = GameThemes.DeepDrive;
        var row = new Grid();
        row.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        row.ColumnsProportions.Add(Proportion.Auto);
        row.Widgets.Add(Text(name, theme.ResourceHud.LabelFontSize, theme.SecondaryText));
        Grid.SetColumn(value, 1);
        row.Widgets.Add(value);
        return row;
    }

    private static HorizontalProgressBar Bar(string id, float value) => new()
    {
        Id = id,
        Minimum = 0,
        Maximum = 1,
        Value = value,
        Height = GameThemes.DeepDrive.Layout.HudBarHeight,
        Background = new SolidBrush(GameThemes.DeepDrive.ControlSurface),
        Filler = new SolidBrush(GameThemes.DeepDrive.SecondaryText)
    };

    private static Label Text(string text, int size, Color color) => new()
    {
        Text = text,
        Font = ThemeAssets.Font(size),
        TextColor = color,
        OverTextColor = color,
        FocusedTextColor = color,
        PressedTextColor = color
    };

    public void Refresh()
    {
        if (_health is not null)
        {
            _healthValue.Text = $"{MathF.Round(_health.Ratio * 100)}%";
            _healthBar.Value = _health.Ratio;
        }
        if (_shownExperience != _run.XP)
        {
            _shownExperience = _run.XP;
            _experience.Text = $"{_shownExperience.ToString("N0", CultureInfo.InvariantCulture)} XP";
        }
        // Ten-minute presentation timer; reaching zero does not end the sandbox.
        var remaining = TimeSpan.FromMinutes(10) - _run.Duration;
        var seconds = remaining > TimeSpan.Zero ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
        if (_shownSeconds != seconds)
        {
            _shownSeconds = seconds;
            _timer.Text = TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        }
    }

    internal void BindHealth(HealthComponent health)
    {
        _health = health;
        Refresh();
    }

    protected override void OnDestroy()
    {
        Resources.Dispose();
        _assets.Dispose();
        _mapTexture.Dispose();
    }
}
