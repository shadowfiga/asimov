using Graphite.Engine.Configuration;
using Graphite.Engine.Core;
using Graphite.Engine.Persistence;
using Graphite.Engine.UI;
using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Theming;
using Graphite.Game.Configuration;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

public sealed class MainMenuScreen : UIScreen
{
    private readonly MenuAssets _assets = new();
    private readonly List<MenuButton> _rows = [];
    private MenuButton _settingsButton = null!;
    private MenuButton _quitButton = null!;
    private MenuButton _creditsButton = null!;
    private MenuButton _creditsBackButton = null!;
    private MenuButton _settingsBackButton = null!;
    private HorizontalSlider _crtIntensity = null!;
    private HorizontalProgressBar _crtMeter = null!;
    private Label _crtPercentage = null!;
    private UIMaterialHost _screen = null!;
    private UIMaterialHost _menu = null!;
    private UIMaterialHost _credits = null!;
    private UIMaterialHost _settings = null!;
    private VerticalStackPanel _content = null!;
    private VerticalStackPanel _brand = null!;
    private VerticalStackPanel _buttons = null!;
    private VerticalStackPanel _settingsContent = null!;
    private VerticalStackPanel _creditsContent = null!;
    private MenuTitle _title = null!;
    private Label? _staging;
    private Panel _root = null!;
    private Point _lastSize;

    internal MenuButton SettingsButton => _settingsButton;
    internal bool CrtEnabled => _crtIntensity.Value > 0;
    internal HorizontalSlider CrtIntensityControl => _crtIntensity;
    internal bool CrtControlVisible => _crtIntensity.Visible;
    internal UIMaterialHost ScreenHost => _screen;
    internal bool SettingsVisible => _settings.Visible;

    protected override Widget Build()
    {
        try
        {
            return BuildMenu();
        }
        catch
        {
            _assets.Dispose();
            throw;
        }
    }

    private Widget BuildMenu()
    {
        var theme = GameThemes.DeepDrive;
        var spacing = theme.Spacing;
        _title = new MenuTitle();
        _brand = new VerticalStackPanel { Spacing = spacing.Sm };
        _brand.Widgets.Add(new Label
        {
            Text = "K-01 INDUSTRIES // FIELD TERMINAL",
            Font = ThemeAssets.Font(15),
            TextColor = theme.Selection,
            HorizontalAlignment = HorizontalAlignment.Left
        });
        _brand.Widgets.Add(_title);
        _brand.Widgets.Add(new Label
        {
            Text = "RESOURCES TODAY. A BRIGHTER TOMORROW.",
            StyleName = "secondary",
            Font = ThemeAssets.Font(16)
        });

        _buttons = new VerticalStackPanel { Spacing = spacing.Sm };
        AddRow("CONTINUE", "play", enabled: false, primary: true, arrow: false);
        AddRow("NEW OPERATION", "play", enabled: false);
        AddRow("LOAD OPERATION", "folder-open", enabled: false);
        _settingsButton = AddRow("SETTINGS", "settings");
        _creditsButton = AddRow("CREDITS", "users", enabled: GameSettings.Instance.Menu.Credits.Count > 0);
        _quitButton = AddRow("QUIT", "log-out");

        _content = new VerticalStackPanel
        {
            Spacing = spacing.Xl,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        _content.Widgets.Add(_brand);
        _content.Widgets.Add(_buttons);
        _menu = new UIMaterialHost(_content, interactions: MenuPresentation.FadeStyle);

        _settingsContent = BuildSettings(theme);
        _settings = new UIMaterialHost(_settingsContent, interactions: MenuPresentation.FadeStyle);
        _creditsContent = BuildCredits(theme);
        _credits = new UIMaterialHost(_creditsContent, interactions: MenuPresentation.FadeStyle);

        _root = new Panel(styleName: "root") { Background = _assets };
        _root.Widgets.Add(_menu);
        _root.Widgets.Add(_settings);
        _root.Widgets.Add(_credits);
        if (GameSettings.Instance.Environment == SettingsEnvironment.Staging)
        {
            _staging = new Label
            {
                Text = "STAGING",
                Font = ThemeAssets.Font(14),
                TextColor = theme.DeepBlack,
                Background = new RoundedRectangleBrush(theme.Selection, theme.BorderRadius.Xs),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(spacing.Sm, spacing.Xs)
            };
            _root.Widgets.Add(_staging);
        }

        _screen = new UIMaterialHost(_root, [MenuPresentation.Crt], MenuPresentation.FadeStyle);
        ApplyCrtSettings();
        Resize();
        return _screen;
    }

    private VerticalStackPanel BuildSettings(GameTheme theme)
    {
        var spacing = theme.Spacing;
        var content = DialogContent("SETTINGS", theme);
        content.Widgets.Add(new Label
        {
            Text = "DISPLAY",
            Font = ThemeAssets.Font(15),
            TextColor = theme.Selection
        });

        var intensity = Preferences.Get(PlayerPreferences.CrtIntensity);
        _crtMeter = new HorizontalProgressBar
        {
            Minimum = 0,
            Maximum = 1,
            Value = intensity,
            Width = 240,
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center
        };
        _crtIntensity = new HorizontalSlider
        {
            Minimum = 0,
            Maximum = 1,
            Value = intensity,
            WheelStep = .05f,
            Width = 240,
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center
        };
        _crtPercentage = new Label
        {
            Text = IntensityLabel(intensity),
            Font = ThemeAssets.Font(16),
            TextColor = theme.PrimaryText,
            Width = 54,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Widgets.Add(IntensityControl(theme));
        _settingsBackButton = new MenuButton(_assets, "BACK", "arrow-left", arrow: false)
        {
            Width = 240,
            Height = 54,
            Margin = new Thickness(0, spacing.Md, 0, 0)
        };
        content.Widgets.Add(_settingsBackButton);
        return content;
    }

    private Grid IntensityControl(GameTheme theme)
    {
        var control = new Grid
        {
            ColumnSpacing = theme.Spacing.Sm,
            Height = 54,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        control.ColumnsProportions.Add(Proportion.Auto);
        control.ColumnsProportions.Add(Proportion.Auto);
        control.ColumnsProportions.Add(Proportion.Auto);
        control.ColumnsProportions.Add(Proportion.Auto);
        control.ColumnsProportions.Add(Proportion.Auto);
        control.RowsProportions.Add(new Proportion(ProportionType.Fill));
        control.Widgets.Add(new Label
        {
            Text = "CRT EFFECT",
            Font = ThemeAssets.Font(18),
            TextColor = theme.SecondaryText,
            VerticalAlignment = VerticalAlignment.Center
        });

        var no = EndpointLabel("NO", theme);
        Grid.SetColumn(no, 1);
        control.Widgets.Add(no);

        var meter = new Grid { Width = 240, Height = 24, VerticalAlignment = VerticalAlignment.Center };
        meter.RowsProportions.Add(new Proportion(ProportionType.Fill));
        meter.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        meter.Widgets.Add(_crtMeter);
        meter.Widgets.Add(_crtIntensity);
        Grid.SetColumn(meter, 2);
        control.Widgets.Add(meter);

        var full = EndpointLabel("FULL", theme);
        Grid.SetColumn(full, 3);
        control.Widgets.Add(full);
        Grid.SetColumn(_crtPercentage, 4);
        control.Widgets.Add(_crtPercentage);
        return control;
    }

    private static Label EndpointLabel(string text, GameTheme theme) => new()
    {
        Text = text,
        Font = ThemeAssets.Font(14),
        TextColor = theme.SecondaryText,
        VerticalAlignment = VerticalAlignment.Center
    };

    private VerticalStackPanel BuildCredits(GameTheme theme)
    {
        var content = DialogContent("CREDITS", theme);
        foreach (var entry in GameSettings.Instance.Menu.Credits)
        {
            content.Widgets.Add(new Label { Text = entry, HorizontalAlignment = HorizontalAlignment.Left });
        }
        _creditsBackButton = new MenuButton(_assets, "BACK", "arrow-left", arrow: false)
        {
            Width = 240,
            Height = 54,
            Margin = new Thickness(0, theme.Spacing.Md, 0, 0)
        };
        content.Widgets.Add(_creditsBackButton);
        return content;
    }

    private static VerticalStackPanel DialogContent(string title, GameTheme theme)
    {
        var content = new VerticalStackPanel
        {
            Width = 620,
            Spacing = theme.Spacing.Md,
            Padding = new Thickness(theme.Spacing.Xl),
            Background = new RoundedRectangleBrush(theme.RaisedSurface, theme.BorderRadius.Xs, theme.Border, 1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false
        };
        content.Widgets.Add(new Label
        {
            Text = title,
            Font = ThemeAssets.Font(32),
            TextColor = theme.PrimaryText,
            HorizontalAlignment = HorizontalAlignment.Left
        });
        return content;
    }

    private MenuButton AddRow(string text, string icon, bool enabled = true, bool primary = false, bool arrow = true)
    {
        var button = new MenuButton(_assets, text, icon, primary, arrow) { Enabled = enabled };
        _rows.Add(button);
        _buttons.Widgets.Add(button);
        return button;
    }

    private void Resize()
    {
        var size = MyraEnvironment.GraphicsDevice.PresentationParameters;
        var currentSize = new Point(size.BackBufferWidth, size.BackBufferHeight);
        if (currentSize == _lastSize)
        {
            return;
        }

        _lastSize = currentSize;
        var spacing = GameThemes.DeepDrive.Spacing;
        var scale = Math.Clamp(size.BackBufferHeight / 900f, .45f, 1.2f);
        var edge = Math.Max(spacing.Md, (int)(spacing.Xl * 2 * scale));
        var width = Math.Max(280, Math.Min((int)(520 * scale), size.BackBufferWidth - edge * 2));
        _menu.Width = width + edge;
        _menu.Margin = new Thickness(edge, spacing.Xl, 0, spacing.Xl);
        _content.Spacing = Math.Max(spacing.Md, (int)(spacing.Xl * scale));
        _brand.Spacing = Math.Max(spacing.Xs, (int)(spacing.Sm * scale));
        _buttons.Spacing = Math.Max(spacing.Xs, (int)(spacing.Sm * scale));
        _title.Fit(width, Math.Max(24, (int)(48 * scale)));
        foreach (var button in _rows)
        {
            button.Width = width;
            button.Height = Math.Max(34, (int)(68 * scale));
            button.Resize(scale, 30);
        }

        var dialogWidth = Math.Max(360, Math.Min(620, size.BackBufferWidth - edge * 2));
        _settingsContent.Width = dialogWidth;
        _creditsContent.Width = dialogWidth;
        if (_staging is not null)
        {
            _staging.Margin = new Thickness(edge, spacing.Lg);
        }
    }

    private void OnResize(object sender, MyraEventArgs args) => Resize();

    protected override void Awake()
    {
        _settingsButton.Click += ShowSettings;
        _quitButton.Click += Quit;
        _creditsButton.Click += ShowCredits;
        _creditsBackButton.Click += BackClicked;
        _settingsBackButton.Click += BackClicked;
        _crtIntensity.ValueChanged += ChangeCrtIntensity;
        _root.ArrangeUpdated += OnResize;
    }

    protected override void OnDestroy()
    {
        _settingsButton.Click -= ShowSettings;
        _quitButton.Click -= Quit;
        _creditsButton.Click -= ShowCredits;
        _creditsBackButton.Click -= BackClicked;
        _settingsBackButton.Click -= BackClicked;
        _crtIntensity.ValueChanged -= ChangeCrtIntensity;
        _root.ArrangeUpdated -= OnResize;
        _assets.Dispose();
    }

    private void ShowSettings(object sender, MyraEventArgs args) => ShowPanel(_settings);
    private void ShowCredits(object sender, MyraEventArgs args) => ShowPanel(_credits);

    private async void ShowPanel(UIMaterialHost panel)
    {
        var result = await _menu.Hide();
        Engine.UI.UI.Post(() =>
        {
            if (IsOpen && !IsClosing && result == UIPlaybackState.Completed)
            {
                panel.Show();
            }
        });
    }

    public async void BackToMenu()
    {
        if (IsClosing)
        {
            return;
        }

        var panel = _settings.Visible ? _settings : _credits.Visible ? _credits : null;
        if (panel is null)
        {
            return;
        }

        var result = await panel.Hide();
        Engine.UI.UI.Post(() =>
        {
            if (IsOpen && !IsClosing && result == UIPlaybackState.Completed)
            {
                _menu.Show();
            }
        });
    }

    private void ChangeCrtIntensity(object sender, ValueChangedEventArgs<float> args)
    {
        Preferences.Set(PlayerPreferences.CrtIntensity, _crtIntensity.Value);
        _crtMeter.Value = _crtIntensity.Value;
        _crtPercentage.Text = IntensityLabel(_crtIntensity.Value);
        ApplyCrtSettings();
    }

    private static string IntensityLabel(float intensity) => $"{intensity:P0}";

    private void ApplyCrtSettings()
        => CrtMaterial.Configure(_screen.Animation.BaseParameters, Preferences.Get(PlayerPreferences.CrtIntensity));

    private void BackClicked(object sender, MyraEventArgs args) => BackToMenu();
    private static void Quit(object sender, MyraEventArgs args) => Application.Quit();
}
