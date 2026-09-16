using Graphite.Engine.UI;
using Graphite.Engine.Audio;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Scenes;

public abstract class Scene
{
    public UIScope UI { get; } = new();
    public AudioScope Audio { get; } = new();

    protected internal virtual void OnLoad()
    {
    }
    protected internal virtual void Update(float dt)
    {
    }
    protected internal virtual void Draw(GameTime gameTime)
    {
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
                Audio.Dispose();
            }
            finally
            {
                UI.CloseAll();
            }
        }
    }
}
