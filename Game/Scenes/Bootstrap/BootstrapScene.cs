using Graphite.Engine.Scenes;
using Graphite.Game.UI;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Scenes;

public sealed class BootstrapScene : Scene
{
    private readonly Queue<Action> _preload = [];
    private BootstrapUI _screen = null!;
    private int _total;
    private bool _rendered;
    private bool _completed;

    protected internal override void OnLoad()
    {
        _screen = UI.Open<BootstrapUI>();
        var theme = GameThemes.DeepDrive;
        var sizes = Enum.GetValues<MenuButtonSize>().Select(theme.MenuButton.Size)
            .SelectMany(style => new[] { style.CompactFontSize, style.DefaultFontSize, style.ProminentFontSize })
            .Append(theme.Layout.MenuTitleFontSize).Distinct();
        foreach (var size in sizes)
        {
            _preload.Enqueue(() => ThemeAssets.Font(size));
        }
        var icons = Path.Combine(AppContext.BaseDirectory, "Content", "Icons", "Lucide");
        foreach (var path in Directory.EnumerateFiles(icons, "*.png").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            _preload.Enqueue(() => MenuAssets.PreloadIcon(name));
        }
        _total = _preload.Count;
    }

    protected internal override void Draw(GameTime gameTime) => _rendered = true;

    protected internal override void Update(float dt)
    {
        if (!_rendered || _completed)
        {
            return;
        }
        _rendered = false;
        if (_preload.TryDequeue(out var prepare))
        {
            // Prepare one resource per rendered frame; graphics and UI stay on the game thread.
            prepare();
            _screen.SetProgress(1f - _preload.Count / (float)_total);
            return;
        }
        // The completed progress bar gets its own frame before the menu replaces the loading view.
        _completed = true;
        SceneManager.Load<MainMenuScene>();
    }

    protected internal override void OnUnload() => _preload.Clear();
}
