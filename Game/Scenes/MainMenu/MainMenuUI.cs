using Graphite.Engine.Core;
using Graphite.Engine.UI;
using Graphite.Game.Configuration;
using Graphite.Game.UI;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Settings;
using Graphite.Game.UI.Theming;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.Game.Scenes;

public sealed class MainMenuUI : UIScreen
{
    private readonly MenuAssets _assets = new();
    private readonly UIScope _dialogs = new();
    private MenuButton _settingsButton = null!;
    private MenuButton _creditsButton = null!;
    private MenuButton _quitButton = null!;
    private CreditsDialog? _credits;
    internal SettingsDialog? Settings
    {
        get; private set;
    }

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
        var brand = new VerticalStackPanel { Spacing = spacing.Sm };
        brand.Widgets.Add(new MenuTitle());
        var buttons = new VerticalStackPanel { Spacing = spacing.Sm };
        AddRow(buttons, "CONTINUE", "play", enabled: false, primary: true, arrow: false);
        AddRow(buttons, "NEW OPERATION", "play", enabled: false);
        AddRow(buttons, "LOAD OPERATION", "folder-open", enabled: false);
        _settingsButton = AddRow(buttons, "SETTINGS", "settings");
        _creditsButton = AddRow(buttons, "CREDITS", "users", enabled: GameSettings.Instance.Menu.Credits.Count > 0);
        _quitButton = AddRow(buttons, "QUIT", "log-out");
        var content = new VerticalStackPanel
        {
            Spacing = spacing.Xl,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Widgets.Add(brand);
        content.Widgets.Add(buttons);
        var menu = new UIMaterialHost(new ScrollViewer
        {
            Content = content,
            Width = theme.Layout.MenuWidth + spacing.Md,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch
        }, interactions: MenuPresentation.FadeStyle);
        var root = new Panel(styleName: "root")
        {
            Padding = new Thickness(spacing.Xl * 2, spacing.Xl)
        };
        root.Widgets.Add(menu);
        return new UIMaterialHost(root, interactions: MenuPresentation.FadeStyle);
    }

    private MenuButton AddRow(VerticalStackPanel buttons, string text, string icon,
        bool enabled = true, bool primary = false, bool arrow = true)
    {
        var button = new MenuButton(_assets, text, icon, primary, arrow, size: MenuButtonSize.Menu) { Enabled = enabled };
        buttons.Widgets.Add(button);
        return button;
    }

    protected override void Awake()
    {
        _settingsButton.Click += ShowSettings;
        _creditsButton.Click += ShowCredits;
        _quitButton.Click += Quit;
    }

    protected override void OnDestroy()
    {
        _settingsButton.Click -= ShowSettings;
        _creditsButton.Click -= ShowCredits;
        _quitButton.Click -= Quit;
        _dialogs.CloseAll();
        _assets.Dispose();
    }

    private void ShowSettings(object sender, MyraEventArgs args)
    {
        if (!IsClosing && Settings?.IsOpen != true)
        {
            Settings = _dialogs.Open<SettingsDialog>();
        }
    }

    private void ShowCredits(object sender, MyraEventArgs args)
    {
        if (!IsClosing && _credits?.IsOpen != true)
        {
            _credits = _dialogs.Open<CreditsDialog>();
        }
    }

    public void BackToMenu()
    {
        if (IsClosing)
        {
            return;
        }
        if (Settings?.IsOpen == true)
        {
            Settings.Back();
        }
        else if (_credits?.IsOpen == true)
        {
            Ui.Close(_credits);
        }
    }

    private static void Quit(object sender, MyraEventArgs args) => Application.Quit();
}
