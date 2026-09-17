using Graphite.Engine.UI;
using Graphite.Game.Configuration;
using Graphite.Game.UI.Theming;

namespace Graphite.Game.UI;

internal sealed class MenuTitle : DistributedLabel
{
    internal MenuTitle()
    {
        var theme = GameThemes.DeepDrive;
        Text = GameSettings.DisplayName;
        Width = theme.Layout.MenuWidth;
        Font = ThemeAssets.Font(theme.Layout.MenuTitleFontSize);
        TextColor = theme.PrimaryText;
    }
}
