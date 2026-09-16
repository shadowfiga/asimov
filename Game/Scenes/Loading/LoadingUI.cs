using Graphite.Engine.UI;
using Graphite.Game.UI;
using Graphite.Game.UI.Theming;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.Scenes;

public sealed class LoadingUI : UIScreen
{
    private HorizontalProgressBar _progress = null!;

    protected override Widget Build()
    {
        var theme = GameThemes.DeepDrive;
        var title = DialogLayout.Title("LOADING", theme.MenuButton.Dialog.DefaultFontSize);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _progress = new HorizontalProgressBar
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0,
            Height = theme.Layout.LoadingBarHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var content = new VerticalStackPanel
        {
            Width = theme.Layout.LoadingWidth,
            Spacing = theme.Spacing.Md,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Widgets.Add(title);
        content.Widgets.Add(_progress);
        var root = new Panel(styleName: "root") { Padding = new Thickness(theme.Spacing.Xl) };
        root.Widgets.Add(content);
        return root;
    }

    internal void SetProgress(float value) => _progress.Value = Math.Clamp(value, 0, 1);
}
