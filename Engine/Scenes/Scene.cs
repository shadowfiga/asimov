using Graphite.Engine.UI;
using Graphite.Engine.Audio;
using Graphite.Engine.Objects;
using Graphite.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.Scenes;

public abstract class Scene
{
    private WorldRenderer2D? _renderer;
    public UIScope UI { get; } = new();
    public AudioScope Audio { get; } = new();
    public GameWorld Objects { get; } = new();
    public Camera2D Camera { get; } = new();

    protected internal virtual void OnLoad()
    {
    }
    protected internal virtual void Update(float dt)
    {
    }
    protected internal virtual void Draw(GameTime gameTime)
    {
    }

    protected internal virtual void LateUpdate(float dt)
    {
    }

    internal void UpdateInternal(float dt)
    {
        Update(dt);
        Objects.Update(dt);
        LateUpdate(dt);
    }

    internal void DrawInternal(GameTime gameTime, GraphicsDevice? device)
    {
        Draw(gameTime);
        if (!Objects.HasRenderers)
        {
            return;
        }
        _renderer ??= new WorldRenderer2D(device ?? throw new InvalidOperationException("A graphics device is required to draw scene objects."));
        _renderer.Draw(Objects, Camera);
    }
    protected internal virtual void OnUnload()
    {
    }

    internal void UnloadInternal()
    {
        try
        {
            OnUnload();
        }
        finally
        {
            try
            {
                try
                {
                    Objects.Dispose();
                }
                finally
                {
                    _renderer?.Dispose();
                }
            }
            finally
            {
                try
                {
                    Audio.Dispose();
                }
                finally
                {
                    UI.CloseAll();
                }
            }
        }
    }
}
