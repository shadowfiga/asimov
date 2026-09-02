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
