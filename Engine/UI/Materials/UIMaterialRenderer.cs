using Graphite.Engine.UI.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI.Materials;

internal sealed class UIMaterialRenderer(GraphicsDevice device) : IDisposable
{
    private readonly SpriteBatch _batch = new(device);
    private readonly Stack<RenderTarget2D> _captures = [];
    private readonly Stack<RenderContext> _contexts = [];
    private readonly RasterizerState _scissorState = new() { CullMode = CullMode.None, ScissorTestEnable = true };
    public int AllocatedTargets { get; private set; }

    private RenderTarget2D Create(int width, int height)
    {
        AllocatedTargets++;
        return new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
    }
    private RenderTarget2D RentCapture(int width, int height)
    {
        while (_captures.TryPop(out var target))
        {
            if (target.Width == width && target.Height == height)
            {
                return target;
            }

            target.Dispose(); AllocatedTargets--;
        }
        return Create(width, height);
    }

    internal sealed class Surface : IDisposable
    {
        internal RenderTarget2D? First;
        internal RenderTarget2D? Second;
        public void Dispose() { First?.Dispose(); Second?.Dispose(); First = Second = null; }
    }

    internal void Render(UIMaterialHost host, RenderContext context, Surface surface,
        IReadOnlyList<UIMaterialInstance> materials, UIAnimationPlayer animation)
    {
        var frame = animation.Frame;
        var active = materials.Where(material => material.IsActive(animation.Parameters)).ToArray();
        if (active.Length == 0 && frame.IsIdentity) { host.Content.Render(context); return; }

        var bounds = GlobalBounds(host.Content);
        bounds.Inflate(host.OverflowPadding, host.OverflowPadding);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var viewport = device.Viewport;
        var targets = device.GetRenderTargets();
        var scissor = device.ScissorRectangle;
        var captureWidth = UI.ViewportWidth;
        var captureHeight = UI.ViewportHeight;
        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        var texture = device.Textures[0];
        var capture = RentCapture(captureWidth, captureHeight);
        context.End();
        try
        {
            // Capture in desktop coordinates so children retain their layout and hit-test transforms.
            // A shared scratch target per nesting depth avoids a full-screen target per idle widget.
            device.SetRenderTarget(capture);
            device.Clear(Color.Transparent);
            var childContext = _contexts.TryPop(out var pooledContext) ? pooledContext : new RenderContext();
            try
            {
                childContext.Begin();
                childContext.Scissor = Rectangle.Intersect(context.Scissor, capture.Bounds);
                childContext.Opacity = 1;
                try { host.Content.Render(childContext); }
                finally { childContext.End(); }
            }
            finally { _contexts.Push(childContext); }

            EnsureSurface(surface, bounds.Width, bounds.Height);
            var first = surface.First!;
            var second = surface.Second!;
            device.SetRenderTarget(first);
            device.Clear(Color.Transparent);
            _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp);
            try
            {
                var source = Rectangle.Intersect(bounds, capture.Bounds);
                if (source.Width > 0 && source.Height > 0)
                {
                    _batch.Draw(capture, new Rectangle(source.X - bounds.X, source.Y - bounds.Y, source.Width, source.Height), source, Color.White);
                }
            }
            finally { _batch.End(); }

            foreach (var material in active)
            {
                material.Render(new UIMaterialContext(device, _batch, first, second, animation.Parameters, animation.ElapsedTime));
                (first, second) = (second, first);
            }

            device.SetRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = Rectangle.Intersect(context.Scissor, viewport.Bounds);
            _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, _scissorState);
            try
            {
                var origin = new Vector2(bounds.Width, bounds.Height) / 2;
                _batch.Draw(first, new Vector2(bounds.X, bounds.Y) + origin + frame.Translation, null,
                    Color.White * (context.Opacity * frame.Opacity), MathHelper.ToRadians(frame.Rotation),
                    origin, frame.Scale, SpriteEffects.None, 0);
            }
            finally { _batch.End(); }
        }
        finally
        {
            device.SetRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            _captures.Push(capture);
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler;
            device.Textures[0] = texture?.IsDisposed == true ? null : texture;
            context.Begin();
        }
    }

    private void EnsureSurface(Surface surface, int width, int height)
    {
        if (surface.First?.Width == width && surface.First.Height == height)
        {
            return;
        }

        Release(surface);
        surface.First = Create(width, height);
        surface.Second = Create(width, height);
    }
    internal void Release(Surface surface)
    {
        if (surface.First is not null)
        {
            AllocatedTargets--;
        }

        if (surface.Second is not null)
        {
            AllocatedTargets--;
        }

        surface.Dispose();
    }
    private static Rectangle GlobalBounds(Widget widget)
    {
        var b = widget.Bounds;
        var corners = new[] { widget.ToGlobal(new Vector2(b.Left, b.Top)), widget.ToGlobal(new Vector2(b.Right, b.Top)),
            widget.ToGlobal(new Vector2(b.Left, b.Bottom)), widget.ToGlobal(new Vector2(b.Right, b.Bottom)) };
        var left = (int)MathF.Floor(corners.Min(p => p.X));
        var top = (int)MathF.Floor(corners.Min(p => p.Y));
        return new Rectangle(left, top, (int)MathF.Ceiling(corners.Max(p => p.X)) - left, (int)MathF.Ceiling(corners.Max(p => p.Y)) - top);
    }
    public void Dispose()
    {
        foreach (var target in _captures) { target.Dispose(); AllocatedTargets--; }
        _captures.Clear();
        foreach (var context in _contexts)
        {
            context.Dispose();
        }

        _contexts.Clear(); _batch.Dispose(); _scissorState.Dispose();
    }
}
