using System.Reflection;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Scenes;

public static class SceneManager
{
    private static Scene? _activeScene;
    private static readonly Queue<Action> Pending = new();

    internal static void Shutdown()
    {
        Pending.Clear();
        var scene = _activeScene;
        _activeScene = null;
        scene?.UnloadInternal();
    }

    public static void Load<T>()
        where T : Scene, new()
    {
        Pending.Enqueue(() => LoadNow(new T()));
    }

    public static void Load(string gameRelativeClass)
    {
        var sceneType = ResolveGameSceneType(gameRelativeClass);
        Pending.Enqueue(() => LoadNow(CreateScene(sceneType)));
    }

    private static void LoadNow(Scene scene)
    {
        var previous = _activeScene;
        _activeScene = null;
        previous?.UnloadInternal();
        _activeScene = scene;
        scene.OnLoad();
    }

    private static Type ResolveGameSceneType(string gameRelativeClass)
    {
        var classPath = gameRelativeClass.Trim();
        if (classPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            classPath = classPath[..^3];
        }

        var className = classPath
            .Replace('\\', '.')
            .Replace('/', '.')
            .Trim('.');

        var assembly = Assembly.GetEntryAssembly() ?? typeof(SceneManager).Assembly;
        var assemblyName = assembly.GetName().Name
            ?? throw new InvalidOperationException("The game assembly has no name.");

        var fullName = className.StartsWith($"{assemblyName}.", StringComparison.Ordinal)
            ? className
            : $"{assemblyName}.Game.{className}";
        var sceneType = assembly.GetType(fullName, throwOnError: false, ignoreCase: false)
            ?? throw new InvalidDataException(
                $"Startup scene '{gameRelativeClass}' was not found. Expected class '{fullName}'.");

        if (!typeof(Scene).IsAssignableFrom(sceneType) || sceneType.IsAbstract)
        {
            throw new InvalidDataException(
                $"Startup scene '{fullName}' must be a concrete {nameof(Scene)} class.");
        }

        if (sceneType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidDataException(
                $"Startup scene '{fullName}' must have a public parameterless constructor.");
        }

        return sceneType;
    }

    private static Scene CreateScene(Type sceneType)
        => (Scene)(Activator.CreateInstance(sceneType)
            ?? throw new InvalidOperationException($"Could not create scene '{sceneType.FullName}'."));

    internal static void Update(float dt) => _activeScene?.Update(dt);
    internal static void Draw(GameTime gameTime) => _activeScene?.Draw(gameTime);

    internal static void CommitPendingChanges()
    {
        while (Pending.TryDequeue(out var action))
        {
            action();
        }
    }
}
