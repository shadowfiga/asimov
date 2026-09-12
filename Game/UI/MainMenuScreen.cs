using Graphite.Game.Configuration;
using Graphite.Engine.Configuration;
using System.Diagnostics;
using Graphite.Engine.Core;
using Graphite.Engine.UI;
using Graphite.Game.UI.Theming;
using Graphite.Game.UI.Materials;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

public sealed class MainMenuScreen : UIScreen
{
    private Button _quitButton = null!;
    private Button _creditsButton = null!;
    private Button _discordButton = null!;
    private Button _backButton = null!;
    private UIMaterialHost _menu = null!;
    private UIMaterialHost _footer = null!;
    private UIMaterialHost _credits = null!;
    private MenuSettings _menuContent = null!;

    protected override Widget Build()
    {
        _menuContent = GameSettings.Instance.Menu;
        var playButton = Button.CreateTextButton("NEW GAME / CONTINUE");
        playButton.Width = 350;
        playButton.Height = 48;
        playButton.Enabled = false;
        var settingsButton = Button.CreateTextButton("Settings");
        settingsButton.Width = 350;
        settingsButton.Height = 48;
        _quitButton = Button.CreateTextButton("QUIT");
        _quitButton.Width = 350;
        _quitButton.Height = 48;

        var content = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 18
        };
        content.Widgets.Add(new Label
        {
            Text = "AFTERGREEN",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        content.Widgets.Add(new UIMaterialHost(playButton));
        content.Widgets.Add(new UIMaterialHost(settingsButton));
        content.Widgets.Add(new UIMaterialHost(_quitButton));
        _menu = new UIMaterialHost(content, [MenuPresentation.Flowers], MenuPresentation.ContentStyle) { OverflowPadding = FlowerMaterial.Padding };

        _creditsButton = Button.CreateTextButton("CREDITS");
        _creditsButton.Width = 130;
        _creditsButton.Height = 40;
        _creditsButton.Enabled = _menuContent.Credits.Count > 0;
        _discordButton = Button.CreateTextButton("DISCORD");
        _discordButton.Width = 130;
        _discordButton.Height = 40;
        _discordButton.Enabled = _menuContent.DiscordUrl != null;
        var footer = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(24),
            Spacing = 12
        };
        footer.Widgets.Add(new UIMaterialHost(_creditsButton));
        footer.Widgets.Add(new UIMaterialHost(_discordButton));
        _footer = new UIMaterialHost(footer, interactions: MenuPresentation.FadeStyle);

        var credits = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 18,
            Visible = false
        };
        credits.Widgets.Add(new Label { Text = "CREDITS", HorizontalAlignment = HorizontalAlignment.Center });
        foreach (var entry in _menuContent.Credits)
        {
            credits.Widgets.Add(new Label { Text = entry, HorizontalAlignment = HorizontalAlignment.Center });
        }
        _backButton = Button.CreateTextButton("BACK");
        _backButton.Height = 48;
        credits.Widgets.Add(new UIMaterialHost(_backButton));
        _credits = new UIMaterialHost(credits, [MenuPresentation.Flowers], MenuPresentation.ContentStyle) { OverflowPadding = FlowerMaterial.Padding };

        var root = new Panel(styleName: "root");
        root.Widgets.Add(_menu);
        root.Widgets.Add(_footer);
        root.Widgets.Add(_credits);
        if (GameSettings.Instance.Environment == SettingsEnvironment.Staging)
        {
            root.Widgets.Add(new Label
            {
                Text = "DEMO",
                TextColor = GameThemes.Aftergreen.BackgroundDark,
                Background = new SolidBrush(GameThemes.Aftergreen.Info),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(24),
                Padding = new Thickness(18, 10)
            });
        }
        return new UIMaterialHost(root, [MenuPresentation.Crt], MenuPresentation.FadeStyle);
    }

    protected override void Awake()
    {
        _quitButton.Click += Quit;
        _creditsButton.Click += ShowCredits;
        _discordButton.Click += OpenDiscord;
        _backButton.Click += BackClicked;
    }

    protected override void OnDestroy()
    {
        _quitButton.Click -= Quit;
        _creditsButton.Click -= ShowCredits;
        _discordButton.Click -= OpenDiscord;
        _backButton.Click -= BackClicked;
    }

    private async void ShowCredits(object sender, MyraEventArgs args)
    {
        var results = await Task.WhenAll(_menu.Hide(), _footer.Hide());
        Engine.UI.UI.Post(() =>
        {
            if (IsOpen && !IsClosing && results.All(result => result == Engine.UI.Animation.UIPlaybackState.Completed))
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
            if (IsOpen && !IsClosing && result == Engine.UI.Animation.UIPlaybackState.Completed)
            {
                _menu.Show(); _footer.Show();
            }
        });
    }

    private void BackClicked(object sender, MyraEventArgs args) => BackToMenu();

    private void OpenDiscord(object sender, MyraEventArgs args)
    {
        if (_menuContent.DiscordUrl == null)
        {
            return;
        }
        try
        {
            using var process = Process.Start(new ProcessStartInfo(_menuContent.DiscordUrl.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"Could not open Discord: {exception.Message}");
        }
    }

    private static void Quit(object sender, MyraEventArgs args) => Application.Quit();
}
