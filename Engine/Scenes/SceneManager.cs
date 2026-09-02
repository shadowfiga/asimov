using System.Reflection;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Scenes;

public static class SceneManager
{
    private static readonly List<Scene> LoadedScenes = [];
    private static readonly Queue<Action> Pending = new();

    public static Scene? ActiveScene { get; private set; }
    public static IReadOnlyList<Scene> Scenes => LoadedScenes;

    internal static void Initialize() { }

    public static void Load<T>(SceneLoadMode mode = SceneLoadMode.Single)
        where T : Scene, new()
    {
        Pending.Enqueue(() => LoadNow(new T(), mode));
    }

    public static void Load(string gameRelativeClass, SceneLoadMode mode = SceneLoadMode.Single)
    {
        var sceneType = ResolveGameSceneType(gameRelativeClass);
        Pending.Enqueue(() => LoadNow(CreateScene(sceneType), mode));
    }

    public static void Reload()
    {
        var type = ActiveScene?.GetType();
        if (type is null) return;
        Pending.Enqueue(() => LoadNow((Scene)Activator.CreateInstance(type)!, SceneLoadMode.Single));
    }

    public static void Unload<T>() where T : Scene
    {
        Pending.Enqueue(() =>
        {
            var scene = LoadedScenes.FirstOrDefault(s => s is T);
            if (scene is null) return;
            scene.UnloadInternal();
            LoadedScenes.Remove(scene);
            ActiveScene = LoadedScenes.LastOrDefault();
        });
    }

    private static void LoadNow(Scene scene, SceneLoadMode mode)
    {
        if (mode == SceneLoadMode.Single)
        {
            foreach (var loaded in LoadedScenes)
                loaded.UnloadInternal();
            LoadedScenes.Clear();
        }

        LoadedScenes.Add(scene);
        ActiveScene = scene;
        scene.OnLoad();
    }

    private static Type ResolveGameSceneType(string gameRelativeClass)
    {
        var classPath = gameRelativeClass.Trim();
        if (classPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            classPath = classPath[..^3];

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
            throw new InvalidDataException(
                $"Startup scene '{fullName}' must be a concrete {nameof(Scene)} class.");
        if (sceneType.GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidDataException(
                $"Startup scene '{fullName}' must have a public parameterless constructor.");

        return sceneType;
    }

    private static Scene CreateScene(Type sceneType)
        => (Scene)(Activator.CreateInstance(sceneType)
            ?? throw new InvalidOperationException($"Could not create scene '{sceneType.FullName}'."));

    internal static void Update(float dt)
    {
        foreach (var scene in LoadedScenes.ToArray())
            scene.UpdateInternal(dt);
    }

    internal static void Draw(GameTime gameTime)
    {
        foreach (var scene in LoadedScenes)
            scene.Draw(gameTime);
    }

    internal static void CommitPendingChanges()
    {
        while (Pending.TryDequeue(out var action))
            action();
    }
}
