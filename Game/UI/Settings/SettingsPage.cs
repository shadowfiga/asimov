using Graphite.Game.UI.Theming;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal abstract class SettingsPage : VerticalStackPanel, IDisposable
{
    protected SettingsPage(string title)
    {
        Spacing = GameThemes.DeepDrive.Spacing.Sm;
        Padding = new Myra.Graphics2D.Thickness(0, 0, GameThemes.DeepDrive.Spacing.Md, 0);
        Widgets.Add(DialogLayout.Title(title, 24));
        Widgets.Add(new HorizontalSeparator { Thickness = 2 });
    }

    internal virtual void Synchronize()
    {
    }
    internal virtual void Attach()
    {
    }
    public virtual void Dispose()
    {
    }
}
