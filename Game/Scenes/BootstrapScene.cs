using Graphite.Engine.Scenes;
using Graphite.Game.UI.Theming;
using Graphite.Game.UI.Materials;
using Graphite.Engine.Persistence;
using Graphite.Game.Persistence;
using Graphite.Game.Preferences;
using Graphite.Game.Sessions;

namespace Graphite.Game.Scenes;

public sealed class BootstrapScene : Scene
{
    private readonly CancellationTokenSource _lifetime = new();
    private Task<SaveResult>? _initialization;

    protected internal override void OnLoad()
    {
        MyraTheme.Apply(GameThemes.Aftergreen);
        MenuPresentation.Initialize();
        _initialization = InitializePersistenceAsync(_lifetime.Token);
    }

    private static async Task<SaveResult> InitializePersistenceAsync(CancellationToken cancellationToken)
    {
        var result = await GamePersistence.Preferences.LoadAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SessionManager.Instance.RefreshSlotsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not list save slots: {exception.Message}");
        }

        return result;
    }

    protected internal override void Update(float dt)
    {
        if (_initialization is not { IsCompleted: true })
        {
            return;
        }

        var result = _initialization.GetAwaiter().GetResult();
        _initialization = null;
        if (result.Status is not (SaveStatus.Success or SaveStatus.NotFound))
        {
            Console.Error.WriteLine($"Could not load preferences: {result.Error}");
        }

        PlayerPreferences.ApplyAudio();
        SceneManager.Load<MainMenuScene>();
    }

    protected internal override void OnUnload()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
