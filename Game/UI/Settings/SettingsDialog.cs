using Graphite.Engine.Graphics;
using Graphite.Engine.UI;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Theming;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.Game.UI.Settings;

/// <summary>Reusable settings overlay; pages own their controls and preference bindings.</summary>
public sealed class SettingsDialog : UIScreen
{
    private readonly MenuAssets _assets = new();
    private SettingsPage[] _pages = [];
    private SettingsNavigationButton[] _navigation = [];
    private Grid _frame = null!;
    private ScrollViewer _pageScroll = null!;
    private MenuButton _back = null!;
    private Dialog _overlay = null!;
    private DisplayConfirmation _confirmation = null!;
    private AccessibilitySettingsPage _accessibility = null!;
    private VideoSettingsPage _video = null!;
    private AudioSettingsPage _audio = null!;
    internal int SelectedPage
    {
        get; private set;
    }
    internal IReadOnlyList<SettingsNavigationButton> Navigation => _navigation;
    internal Widget Frame => _frame;
    internal Dialog Overlay => _overlay;
    internal MenuButton BackButton => _back;
    internal ComboView UiScaleControl => _accessibility.ScaleControl;
    internal HorizontalSlider CrtIntensityControl => _accessibility.Crt;
    internal ComboView ResolutionControl => _video.Resolution;
    internal ComboView WindowModeControl => _video.Mode;
    internal HorizontalSlider VolumeControl => _audio.Volume;
    internal HorizontalSlider MusicVolumeControl => _audio.Music;
    internal HorizontalSlider FxVolumeControl => _audio.Fx;
    internal Dialog DisplayConfirmation => _confirmation.Dialog.Overlay;
    internal MenuButton KeepDisplayButton => _confirmation.Dialog.ConfirmButton;
    internal MenuButton RevertDisplayButton => _confirmation.Dialog.CancelButton;

    protected override Widget Build()
    {
        try
        {
            return BuildDialog();
        }
        catch
        {
            foreach (var page in _pages)
            {
                page.Dispose();
            }
            _assets.Dispose();
            throw;
        }
    }

    private Widget BuildDialog()
    {
        var theme = GameThemes.DeepDrive;
        var spacing = theme.Spacing;
        _video = new VideoSettingsPage(_assets);
        _audio = new AudioSettingsPage();
        _accessibility = new AccessibilitySettingsPage(_assets);
        _pages = [_video, _audio, _accessibility];
        _frame = new Grid
        {
            Width = theme.Layout.SettingsSize.X,
            Height = theme.Layout.SettingsSize.Y,
            RowSpacing = spacing.Md,
            Padding = new Thickness(spacing.Xl),
            Background = DialogLayout.Surface()
        };
        _frame.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _frame.RowsProportions.Add(Proportion.Auto);
        _frame.RowsProportions.Add(Proportion.Auto);
        _frame.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _frame.RowsProportions.Add(Proportion.Auto);
        _frame.RowsProportions.Add(Proportion.Auto);
        _frame.Widgets.Add(DialogLayout.Title("SETTINGS"));
        AddToFrame(new HorizontalSeparator { Thickness = 2 }, 1);
        var body = new AdaptiveGrid { ColumnSpacing = spacing.Lg };
        body.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, theme.Layout.SidebarWidth));
        body.ColumnRules.Add(new UIColumnRule(0, theme.Layout.SidebarWidth,
            theme.Layout.CompactSidebarWidth, theme.Layout.SidebarBreakpoint));
        body.ColumnsProportions.Add(Proportion.Auto);
        body.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        body.RowsProportions.Add(new Proportion(ProportionType.Fill));
        var navigation = new VerticalStackPanel { Spacing = spacing.Sm };
        _navigation = [new("VIDEO"), new("AUDIO"), new("ACCESSIBILITY")];
        for (var index = 0; index < _navigation.Length; index++)
        {
            var pageIndex = index;
            _navigation[index].Click += (_, _) => SelectPage(pageIndex);
            navigation.Widgets.Add(_navigation[index]);
        }
        body.Widgets.Add(new ScrollViewer { Content = navigation, ShowHorizontalScrollBar = false });
        var divider = new VerticalSeparator { Thickness = 2 };
        Grid.SetColumn(divider, 1);
        body.Widgets.Add(divider);
        _pageScroll = new ScrollViewer { ShowHorizontalScrollBar = false };
        Grid.SetColumn(_pageScroll, 2);
        body.Widgets.Add(_pageScroll);
        AddToFrame(body, 2);
        AddToFrame(new HorizontalSeparator { Thickness = 2 }, 3);
        _back = new MenuButton(_assets, "BACK", "arrow-left", arrow: false, size: MenuButtonSize.Dialog)
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        AddToFrame(_back, 4);
        _overlay = new Dialog(new UIMaterialHost(_frame, interactions: MenuPresentation.FadeStyle));
        _confirmation = new DisplayConfirmation(_assets);
        var root = new Panel(styleName: "root") { Background = null };
        root.Widgets.Add(_overlay);
        root.Widgets.Add(_confirmation.Dialog.Overlay);
        SelectPage(0);
        return root;
    }

    private void AddToFrame(Widget widget, int row)
    {
        Grid.SetRow(widget, row);
        _frame.Widgets.Add(widget);
    }

    internal void SelectPage(int index)
    {
        if (index < 0 || index >= _pages.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if (IsClosing || (IsOpen && DisplaySettings.NeedsConfirmation))
        {
            return;
        }
        SelectedPage = index;
        _pages[index].Synchronize();
        _pageScroll.Content = _pages[index];
        _pageScroll.ScrollPosition = Microsoft.Xna.Framework.Point.Zero;
        for (var item = 0; item < _navigation.Length; item++)
        {
            _navigation[item].Select(item == index);
        }
    }

    protected override void Awake()
    {
        foreach (var page in _pages)
        {
            page.Attach();
        }
        _confirmation.Attach();
        _back.Click += BackClicked;
        _overlay.SetKeyboardFocus();
    }

    protected override void OnDestroy()
    {
        _back.Click -= BackClicked;
        foreach (var page in _pages)
        {
            page.Dispose();
        }
        _confirmation.Dispose();
        if (DisplaySettings.NeedsConfirmation)
        {
            DisplaySettings.Revert();
        }
        _assets.Dispose();
    }

    public void Back()
    {
        if (DisplaySettings.NeedsConfirmation)
        {
            DisplaySettings.Revert();
        }
        else
        {
            Ui.Close(this);
        }
    }

    private void BackClicked(object sender, MyraEventArgs args) => Back();
}
