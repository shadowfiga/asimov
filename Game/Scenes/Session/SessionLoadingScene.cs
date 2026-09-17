using Graphite.Engine.Audio;
using Graphite.Engine.Scenes;
using Graphite.Game.Audio;
using Graphite.Game.Sessions;

namespace Graphite.Game.Scenes;

public sealed class SessionLoadingScene : LoadingScene
{
    private readonly Session _session = new();

    protected override IEnumerable<Action> Prepare()
    {
        yield return () => GameAudio.SessionCue(AudioManager.Current);
    }

    protected override void Complete()
    {
        // Publish a ready session only after preparation succeeds, before gameplay can access it.
        _session.StartRun();
        SessionManager.ActiveSession = _session;
        SceneManager.Load<SandboxScene>();
    }
}
