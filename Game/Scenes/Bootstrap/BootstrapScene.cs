using Graphite.Engine.Scenes;
using Graphite.Engine.Audio;
using Graphite.Game.Audio;
using Graphite.Game.UI;
using Graphite.Game.UI.Theming;

namespace Graphite.Game.Scenes;

public sealed class BootstrapScene : LoadingScene
{
    protected override IEnumerable<Action> Prepare()
    {
        var theme = GameThemes.DeepDrive;
        var sizes = Enum.GetValues<MenuButtonSize>().Select(theme.MenuButton.Size)
            .SelectMany(style => new[] { style.CompactFontSize, style.DefaultFontSize, style.ProminentFontSize })
            .Append(theme.Layout.MenuTitleFontSize)
            .Append(theme.ResourceHud.LabelFontSize).Append(theme.ResourceHud.ValueFontSize).Distinct();
        foreach (var size in sizes)
        {
            yield return () => ThemeAssets.Font(size);
        }
        var icons = Path.Combine(AppContext.BaseDirectory, "Content", "Icons", "Lucide");
        foreach (var path in Directory.EnumerateFiles(icons, "*.png").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            yield return () => MenuAssets.PreloadIcon(name);
        }
        foreach (var step in GameAudio.PreloadSteps(AudioManager.Current))
        {
            yield return step;
        }
    }

    protected override void Complete() => SceneManager.Load<MainMenuScene>();
}
