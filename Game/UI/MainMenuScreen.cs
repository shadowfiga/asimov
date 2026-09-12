using System.Diagnostics;
using Graphite.Engine.Configuration;
using Graphite.Engine.Core;
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
    private readonly List<(MenuButton Button, UIMaterialHost Host)> _rows = [];
    private MenuButton _quitButton = null!;
    private MenuButton _creditsButton = null!;
    private MenuButton _discordButton = null!;
    private MenuButton _backButton = null!;
    private UIMaterialHost _menu = null!;
    private UIMaterialHost _footer = null!;
    private UIMaterialHost _credits = null!;
    private UIMaterialHost _back = null!;
    private UIMaterialHost _discord = null!;
    private VerticalStackPanel _content = null!;
    private VerticalStackPanel _brand = null!;
    private VerticalStackPanel _buttons = null!;
    private MenuTitle _title = null!;
    private Image _logo = null!;
    private Label? _demo;
    private Panel _root = null!;
    private Point _lastSize;

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
        var theme = GameThemes.Aftergreen;
        var spacing = theme.Spacing;
        _logo = _assets.Icon("leaf", 56, Color.Lerp(theme.Color4, theme.Gray5, .5f));
        _logo.HorizontalAlignment = HorizontalAlignment.Center;
        _title = new MenuTitle();
        _brand = new VerticalStackPanel { Spacing = spacing.Md };
        _brand.Widgets.Add(_logo);
        _brand.Widgets.Add(_title);
        _buttons = new VerticalStackPanel { Spacing = spacing.Sm };

        AddRow("CONTINUE", "play", enabled: false, primary: true, arrow: false);
        AddRow("NEW GAME", "leaf", enabled: false);
        AddRow("LOAD GAME", "folder-open", enabled: false);
        AddRow("SETTINGS", "settings");
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
        _menu = new UIMaterialHost(_content, [MenuPresentation.Flowers], MenuPresentation.ContentStyle)
        { OverflowPadding = FlowerMaterial.Padding };

        _discordButton = new MenuButton(_assets, "DISCORD", "message-circle", arrow: false)
        { Enabled = GameSettings.Instance.Menu.DiscordUrl is not null };
        _discord = new UIMaterialHost(_discordButton);
        var footer = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        footer.Widgets.Add(_discord);
        _footer = new UIMaterialHost(footer, interactions: MenuPresentation.FadeStyle);

        var credits = new VerticalStackPanel
        {
            Spacing = spacing.Md,
            Padding = new Thickness(spacing.Xl),
            Background = new RoundedRectangleBrush(theme.BackgroundDark * .94f, theme.BorderRadius.Lg, theme.Gray3, 1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false
        };
        credits.Widgets.Add(new Label { Text = "CREDITS", Font = ThemeAssets.Font(30), HorizontalAlignment = HorizontalAlignment.Center });
        foreach (var entry in GameSettings.Instance.Menu.Credits)
        {
            credits.Widgets.Add(new Label { Text = entry, HorizontalAlignment = HorizontalAlignment.Center });
        }
        _backButton = new MenuButton(_assets, "BACK", "arrow-left", arrow: false);
        _back = new UIMaterialHost(_backButton);
        credits.Widgets.Add(_back);
        _credits = new UIMaterialHost(credits, [MenuPresentation.Flowers], MenuPresentation.ContentStyle)
        { OverflowPadding = FlowerMaterial.Padding };

        var root = _root = new Panel(styleName: "root") { Background = _assets };
        root.Widgets.Add(_menu);
        root.Widgets.Add(_footer);
        root.Widgets.Add(_credits);
        if (GameSettings.Instance.Environment == SettingsEnvironment.Staging)
        {
            _demo = new Label
            {
                Text = "DEMO",
                Font = ThemeAssets.Font(16),
                TextColor = theme.BackgroundDark,
                Background = new RoundedRectangleBrush(theme.Info, theme.BorderRadius.Xs),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(spacing.Sm, spacing.Xs)
            };
            root.Widgets.Add(_demo);
        }

        Resize();
        var host = new UIMaterialHost(root, [MenuPresentation.Crt], MenuPresentation.FadeStyle);
        host.Animation.BaseParameters.Set(CrtMaterial.Strength, .22f);
        return host;
    }

    private MenuButton AddRow(string text, string icon, bool enabled = true, bool primary = false, bool arrow = true)
    {
        var button = new MenuButton(_assets, text, icon, primary, arrow) { Enabled = enabled };
        var host = new UIMaterialHost(button);
        _rows.Add((button, host));
        _buttons.Widgets.Add(host);
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
        var spacing = GameThemes.Aftergreen.Spacing;
        var scale = Math.Clamp(size.BackBufferHeight / 900f, .45f, 1.2f);
        var edge = Math.Max(spacing.Md, (int)(spacing.Xl * 2 * scale));
        var width = Math.Max(200, Math.Min((int)(440 * scale), size.BackBufferWidth - edge * 2));
        _menu.Width = width + edge;
        _menu.Margin = new Thickness(edge, spacing.Xl, 0, spacing.Xl);
        _content.Spacing = Math.Max(spacing.Md, (int)(spacing.Xl * scale));
        _brand.Spacing = Math.Max(spacing.Xs, (int)(spacing.Md * scale));
        _buttons.Spacing = Math.Max(spacing.Xs, (int)(spacing.Sm * scale));
        _logo.Width = _logo.Height = (int)(64 * scale);
        _title.Fit(width, Math.Max(24, (int)(48 * scale)));
        foreach (var (button, host) in _rows)
        {
            host.Width = width;
            host.Height = Math.Max(34, (int)(76 * scale));
            button.Resize(scale);
        }

        _footer.Margin = new Thickness(spacing.Xl);
        _discord.Width = Math.Max(140, (int)(180 * scale));
        _discord.Height = Math.Max(36, (int)(50 * scale));
        _discordButton.Resize(Math.Min(.8f, scale), 26);
        _back.Width = Math.Min(320, width);
        _back.Height = Math.Max(40, (int)(64 * scale));
        _backButton.Resize(scale);
        if (_demo is not null)
        {
            _demo.Margin = new Thickness(edge, spacing.Lg);
        }
    }

    private void OnResize(object sender, MyraEventArgs args) => Resize();

    protected override void Awake()
    {
        _quitButton.Click += Quit;
        _creditsButton.Click += ShowCredits;
        _discordButton.Click += OpenDiscord;
        _backButton.Click += BackClicked;
        _root.ArrangeUpdated += OnResize;
    }

    protected override void OnDestroy()
    {
        _quitButton.Click -= Quit;
        _creditsButton.Click -= ShowCredits;
        _discordButton.Click -= OpenDiscord;
        _backButton.Click -= BackClicked;
        _root.ArrangeUpdated -= OnResize;
        _assets.Dispose();
    }

    private async void ShowCredits(object sender, MyraEventArgs args)
    {
        var results = await Task.WhenAll(_menu.Hide(), _footer.Hide());
        Engine.UI.UI.Post(() =>
        {
            if (IsOpen && !IsClosing && results.All(result => result == UIPlaybackState.Completed))
            {
                _credits.Show();
            }
        });
    }

    public async void BackToMenu()
    {
        if (!_credits.Visible || IsClosing)
        {
            return;
        }

        var result = await _credits.Hide();
        Engine.UI.UI.Post(() =>
        {
            if (IsOpen && !IsClosing && result == UIPlaybackState.Completed)
            {
                _menu.Show();
                _footer.Show();
            }
        });
    }

    private void BackClicked(object sender, MyraEventArgs args) => BackToMenu();

    private void OpenDiscord(object sender, MyraEventArgs args)
    {
        var discordUrl = GameSettings.Instance.Menu.DiscordUrl;
        if (discordUrl is null)
        {
            return;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(discordUrl.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"Could not open Discord: {exception.Message}");
        }
    }

    private static void Quit(object sender, MyraEventArgs args) => Application.Quit();
}
