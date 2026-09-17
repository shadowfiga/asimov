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
        // Publish only after preparation succeeds; scene code can use the fail-fast non-null getter.
        SessionManager.ActiveSession = _session;
        SceneManager.Load<SandboxScene>();
    }
}
