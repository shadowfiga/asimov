using Graphite.Engine.Objects;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Graphics;

public sealed class WorldRenderer2D : IDisposable
{
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly RenderContext2D _context;
    public WorldRenderer2D(GraphicsDevice device)
    {
        _batch = new SpriteBatch(device);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _context = new RenderContext2D(_batch, _pixel);
    }
    public void Draw(GameWorld world, Camera2D camera)
    {
        _context.Camera = camera;
        foreach (var renderer in world.RenderQueue())
        {
            if (!renderer.IsActive)
            {
                continue;
            }
            _batch.Begin(transformMatrix: renderer.Transform.WorldMatrix * camera.Transform, samplerState: SamplerState.PointClamp);
            try
            {
                renderer.Draw(_context);
            }
            finally
            {
                _batch.End();
            }
        }
    }
    public void Dispose()
    {
        _pixel.Dispose();
        _batch.Dispose();
    }
}
