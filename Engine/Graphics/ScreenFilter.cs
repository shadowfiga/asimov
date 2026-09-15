using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.Graphics;

/// <summary>A typed value accepted by a screen filter.</summary>
public sealed class ScreenFilterParameter<T>(string name, T defaultValue) where T : struct
{
    public string Name { get; } = name;
    public T DefaultValue { get; } = defaultValue;
}

/// <summary>The values supplied to the active screen filter.</summary>
public sealed class ScreenFilterParameters
{
    private readonly Dictionary<object, object> _values = [];

    public T Get<T>(ScreenFilterParameter<T> parameter) where T : struct
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return _values.TryGetValue(parameter, out var value) ? (T)value : parameter.DefaultValue;
    }

    public void Set<T>(ScreenFilterParameter<T> parameter, T value) where T : struct
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _values[parameter] = value;
    }
}

/// <summary>A reusable definition that creates device-bound screen-filter instances.</summary>
public abstract class ScreenFilter
{
    public abstract ScreenFilterInstance CreateInstance(GraphicsDevice graphicsDevice);
}

/// <summary>The device-bound resources and rendering behavior for one screen filter.</summary>
public abstract class ScreenFilterInstance : IDisposable
{
    public virtual bool IsActive(ScreenFilterParameters parameters) => true;
    public abstract void Render(ScreenFilterContext context);
    public virtual void Dispose()
    {
    }
}

/// <summary>A single full-screen pass whose source and destination are always distinct.</summary>
public sealed class ScreenFilterContext(
    GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D source,
    RenderTarget2D destination, ScreenFilterParameters parameters, float elapsedTime)
{
    public GraphicsDevice GraphicsDevice { get; } = graphicsDevice;
    public SpriteBatch SpriteBatch { get; } = spriteBatch;
    public Texture2D Source { get; } = source;
    public RenderTarget2D Destination { get; } = destination;
    public ScreenFilterParameters Parameters { get; } = parameters;
    public float ElapsedTime { get; } = elapsedTime;
    public Vector2 Dimensions => new(Destination.Width, Destination.Height);

    public void Draw(Effect? effect = null)
    {
        GraphicsDevice.SetRenderTarget(Destination);
        GraphicsDevice.Clear(Color.Transparent);
        SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, effect);
        try
        {
            SpriteBatch.Draw(Source, Destination.Bounds, Color.White);
        }
        finally
        {
            SpriteBatch.End();
        }
    }
}

/// <summary>Loads compiled DesktopGL shader bytecode and binds typed screen-filter parameters.</summary>
public abstract class ShaderScreenFilter(string assetPath) : ScreenFilter
{
    public override ScreenFilterInstance CreateInstance(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        return new ShaderInstance(this, new Effect(graphicsDevice,
            File.ReadAllBytes(Path.GetFullPath(assetPath, AppContext.BaseDirectory))));
    }

    protected abstract void Configure(Effect effect, ScreenFilterContext context);
    protected virtual bool IsActive(ScreenFilterParameters parameters) => true;

    private sealed class ShaderInstance(ShaderScreenFilter definition, Effect effect) : ScreenFilterInstance
    {
        public override bool IsActive(ScreenFilterParameters parameters) => definition.IsActive(parameters);

        public override void Render(ScreenFilterContext context)
        {
            definition.Configure(effect, context);
            context.Draw(effect);
        }

        public override void Dispose() => effect.Dispose();
    }
}
