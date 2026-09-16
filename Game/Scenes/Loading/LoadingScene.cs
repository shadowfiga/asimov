using Graphite.Engine.Scenes;
using Microsoft.Xna.Framework;

namespace Graphite.Game.Scenes;

/// <summary>Shared frame-presented loading flow; screens only declare their resource steps.</summary>
public abstract class LoadingScene : Scene
{
    private readonly Queue<Action> _preload = [];
    private LoadingUI _screen = null!;
    private int _total;
    private bool _rendered;
    private bool _completed;

    protected abstract IEnumerable<Action> Prepare();
    protected abstract void Complete();

    protected internal sealed override void OnLoad()
    {
        _screen = UI.Open<LoadingUI>();
        foreach (var step in Prepare())
        {
            _preload.Enqueue(step);
        }
        _total = _preload.Count;
        if (_total == 0)
        {
            _screen.SetProgress(1);
        }
    }

    protected internal sealed override void Draw(GameTime gameTime) => _rendered = true;

    protected internal sealed override void Update(float dt)
    {
        if (!_rendered || _completed)
        {
            return;
        }
        _rendered = false;
        if (_preload.TryDequeue(out var prepare))
        {
            // Native audio uploads, graphics and UI stay on the game thread.
            prepare();
            _screen.SetProgress(1f - _preload.Count / (float)_total);
            return;
        }
        // Present a completed bar before replacing the loading view.
        _completed = true;
        Complete();
    }

    protected internal sealed override void OnUnload() => _preload.Clear();
}
