using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.UI.Materials;

public sealed class UIParameter<T>(string name, T defaultValue) where T : struct
{
    public string Name { get; } = name;
    public T DefaultValue { get; } = defaultValue;
}

public sealed class UIParameters
{
    private readonly Dictionary<object, object> _values = [];
    public T Get<T>(UIParameter<T> parameter) where T : struct
        => _values.TryGetValue(parameter, out var value) ? (T)value : parameter.DefaultValue;
    public void Set<T>(UIParameter<T> parameter, T value) where T : struct => _values[parameter] = value;
    internal void CopyFrom(UIParameters other)
    {
        _values.Clear();
        foreach (var pair in other._values)
        {
            _values.Add(pair.Key, pair.Value);
        }
    }
}

/// <summary>Reusable material definition. Mutable resources belong to its instance.</summary>
public abstract class UIMaterial
{
    public abstract UIMaterialInstance CreateInstance(GraphicsDevice graphicsDevice);
}

public abstract class UIMaterialInstance : IDisposable
{
    public virtual bool IsActive(UIParameters parameters) => true;
    public abstract void Render(UIMaterialContext context);
    public virtual void Dispose()
    {
    }
}

/// <summary>A single pass; Source and Destination are always distinct.</summary>
public sealed class UIMaterialContext(
    GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D source,
    RenderTarget2D destination, UIParameters parameters, float elapsedTime)
{
    public GraphicsDevice GraphicsDevice { get; } = graphicsDevice;
    public SpriteBatch SpriteBatch { get; } = spriteBatch;
    public Texture2D Source { get; } = source;
    public RenderTarget2D Destination { get; } = destination;
    public UIParameters Parameters { get; } = parameters;
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

/// <summary>Loads compiled DesktopGL shader bytecode; subclasses bind typed parameters.</summary>
public abstract class UIShaderMaterial(string assetPath) : UIMaterial
{
    public override UIMaterialInstance CreateInstance(GraphicsDevice graphicsDevice)
        => new ShaderInstance(this, new Effect(graphicsDevice,
            File.ReadAllBytes(Path.GetFullPath(assetPath, AppContext.BaseDirectory))));

    protected abstract void Configure(Effect effect, UIMaterialContext context);
    protected virtual bool IsActive(UIParameters parameters) => true;

    private sealed class ShaderInstance(UIShaderMaterial definition, Effect effect) : UIMaterialInstance
    {
        public override bool IsActive(UIParameters parameters) => definition.IsActive(parameters);
        public override void Render(UIMaterialContext context)
        {
            definition.Configure(effect, context);
            context.Draw(effect);
        }
        public override void Dispose() => effect.Dispose();
    }
}
