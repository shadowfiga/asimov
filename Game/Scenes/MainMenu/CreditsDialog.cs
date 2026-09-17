using Graphite.Engine.UI;
using Graphite.Game.Configuration;
using Graphite.Game.UI;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Theming;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.Game.Scenes;

public sealed class CreditsDialog : UIScreen
{
    private readonly MenuAssets _assets = new();
    private Grid _frame = null!;
    private MenuButton _back = null!;
    internal Widget Frame => _frame;
    internal MenuButton BackButton => _back;

    protected override Widget Build()
    {
        var theme = GameThemes.DeepDrive;
        _frame = new Grid
        {
            Width = theme.Layout.CreditsSize.X,
            Height = theme.Layout.CreditsSize.Y,
            Padding = new Thickness(theme.Spacing.Xl),
            RowSpacing = theme.Spacing.Md,
            Background = DialogLayout.Surface()
        };
        _frame.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _frame.RowsProportions.Add(Proportion.Auto);
        _frame.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _frame.RowsProportions.Add(Proportion.Auto);
        _frame.Widgets.Add(DialogLayout.Title("CREDITS"));
        var entries = new VerticalStackPanel { Spacing = theme.Spacing.Md };
        foreach (var entry in GameSettings.Instance.Menu.Credits)
        {
            entries.Widgets.Add(DialogLayout.Title(entry, 20));
        }
        var scroll = new ScrollViewer { Content = entries, ShowHorizontalScrollBar = false };
        Grid.SetRow(scroll, 1);
        _frame.Widgets.Add(scroll);
        _back = new MenuButton(_assets, "BACK", "arrow-left", arrow: false, size: MenuButtonSize.Dialog);
        Grid.SetRow(_back, 2);
        _frame.Widgets.Add(_back);
        return new Graphite.Game.UI.Dialog(new UIMaterialHost(_frame, interactions: MenuPresentation.FadeStyle));
    }

    protected override void Awake()
    {
        _back.Click += Back;
    }

    protected override void OnDestroy()
    {
        _back.Click -= Back;
        _assets.Dispose();
    }

    private void Back(object sender, MyraEventArgs args) => Ui.Close(this);
}
