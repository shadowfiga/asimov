using Graphite.Engine.Core;
using Graphite.Game.Session;

namespace Graphite.Game.Scripts;

public class GameManager: Singleton<GameManager>
{
    private List<SaveSlot> _slots = new();
    private Session _activeSession = null!;

    public void Initialize()
    {

    }
}
