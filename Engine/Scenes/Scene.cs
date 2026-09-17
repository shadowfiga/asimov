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
    /// <summary>The active camera object's projection; scenes do not own a separate camera.</summary>
    public Camera2D Camera => ActiveCamera?.Camera
        ?? throw new InvalidOperationException("The scene has no active camera component.");
    private CameraComponent? ActiveCamera => Objects.GetComponents<CameraComponent>().SingleOrDefault(camera => camera.IsActive);

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

    internal void UpdateInternal(float dt, Point? viewportSize = null)
    {
        if (!float.IsFinite(dt) || dt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dt), "Delta time must be finite and non-negative.");
        }
        RefreshCameras(viewportSize);
        Update(dt);
        Objects.Update(dt);
        LateUpdate(dt);
    }

    internal void DrawInternal(GameTime gameTime, GraphicsDevice? device)
    {
        var presentation = device?.PresentationParameters;
        RefreshCameras(presentation is null ? null : new Point(presentation.BackBufferWidth, presentation.BackBufferHeight));
        Draw(gameTime);
        var camera = ActiveCamera;
        if (!Objects.HasRenderers || camera is null)
        {
            return;
        }
        _renderer ??= new WorldRenderer2D(device ?? throw new InvalidOperationException("A graphics device is required to draw scene objects."));
        _renderer.Draw(Objects, camera.Camera);
    }

    private void RefreshCameras(Point? viewportSize)
    {
        // Exactly one enabled camera renders this scene; disabled cameras can be retained for switching.
        ActiveCamera?.Refresh(viewportSize);
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
