using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.Graphics;

/// <summary>Applies one screen filter to the completed scene and UI frame.</summary>
public static class PostProcessing
{
    private static GraphicsDevice? _device;
    private static SpriteBatch? _batch;
    private static ScreenFilter? _definition;
    private static ScreenFilterInstance? _filter;
    private static RenderTarget2D? _source;
    private static RenderTarget2D? _destination;

    public static ScreenFilterParameters Parameters { get; private set; } = new();
    public static bool IsActive => _filter?.IsActive(Parameters) == true;
    internal static int AllocatedTargets => (_source is null ? 0 : 1) + (_destination is null ? 0 : 1);
    internal static Point TargetSize => _source is null ? Point.Zero : new Point(_source.Width, _source.Height);
    internal static DepthFormat SourceDepthFormat => _source?.DepthStencilFormat ?? DepthFormat.None;
    internal static int SourceMultiSampleCount => _source?.MultiSampleCount ?? 0;

    internal static void Initialize(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        Shutdown();
        _device = graphicsDevice;
        _batch = new SpriteBatch(graphicsDevice);
    }

    public static void Set(ScreenFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var device = Device;
        if (ReferenceEquals(_definition, filter))
        {
            return;
        }

        // Construct first so a failed shader load leaves the current filter usable.
        var instance = filter.CreateInstance(device);
        _filter?.Dispose();
        _definition = filter;
        _filter = instance;
        Parameters = new ScreenFilterParameters();
    }

    public static void Clear()
    {
        _filter?.Dispose();
        _filter = null;
        _definition = null;
        Parameters = new ScreenFilterParameters();
        ReleaseTargets();
    }

    internal static void Render(GameTime gameTime, Color clearColor, Action<GameTime> drawFrame)
    {
        ArgumentNullException.ThrowIfNull(gameTime);
        ArgumentNullException.ThrowIfNull(drawFrame);
        var device = Device;
        var targets = device.GetRenderTargets();
        var viewport = device.Viewport;
        var scissor = device.ScissorRectangle;
        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var rasterizer = device.RasterizerState;
        var sampler = device.SamplerStates[0];
        var texture = device.Textures[0];

        try
        {
            var filter = _filter;
            if (filter is null || !filter.IsActive(Parameters))
            {
                device.SetRenderTarget(null);
                device.Clear(clearColor);
                drawFrame(gameTime);
                return;
            }

            EnsureTargets();
            var source = _source!;
            var destination = _destination!;
            device.SetRenderTarget(source);
            device.Clear(clearColor);
            drawFrame(gameTime);

            filter.Render(new ScreenFilterContext(
                device, Batch, source, destination, Parameters, (float)gameTime.TotalGameTime.TotalSeconds));

            device.SetRenderTarget(null);
            device.Clear(clearColor);
            Batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            try
            {
                Batch.Draw(destination, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);
            }
            finally
            {
                Batch.End();
            }
        }
        finally
        {
            device.SetRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler;
            device.Textures[0] = texture?.IsDisposed == true ? null : texture;
        }
    }

    private static GraphicsDevice Device
        => _device ?? throw new InvalidOperationException("Post-processing is not initialized.");
    private static SpriteBatch Batch
        => _batch ?? throw new InvalidOperationException("Post-processing is not initialized.");

    private static void EnsureTargets()
    {
        var presentation = Device.PresentationParameters;
        var width = presentation.BackBufferWidth;
        var height = presentation.BackBufferHeight;
        if (_source is { IsDisposed: false } && _destination is { IsDisposed: false }
            && _source.Width == width && _source.Height == height
            && _source.Format == presentation.BackBufferFormat
            && _source.DepthStencilFormat == presentation.DepthStencilFormat
            && _source.MultiSampleCount == presentation.MultiSampleCount
            && _destination.Width == width && _destination.Height == height)
        {
            return;
        }

        ReleaseTargets();
        _source = new RenderTarget2D(
            Device, width, height, false, presentation.BackBufferFormat,
            presentation.DepthStencilFormat, presentation.MultiSampleCount, RenderTargetUsage.PreserveContents);
        try
        {
            _destination = new RenderTarget2D(
                Device, width, height, false, presentation.BackBufferFormat,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
        catch
        {
            _source.Dispose();
            _source = null;
            throw;
        }
    }

    private static void ReleaseTargets()
    {
        _source?.Dispose();
        _destination?.Dispose();
        _source = null;
        _destination = null;
    }

    internal static void Shutdown()
    {
        ReleaseTargets();
        _filter?.Dispose();
        _batch?.Dispose();
        _filter = null;
        _definition = null;
        _batch = null;
        _device = null;
        Parameters = new ScreenFilterParameters();
    }
}
