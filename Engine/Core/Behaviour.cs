namespace Graphite.Engine.Core;

public abstract class Behaviour : Component
{
    private bool _started;

    protected internal virtual void Awake() { }
    protected internal virtual void Start() { }
    protected internal virtual void Update(float dt) { }
    protected internal virtual void LateUpdate(float dt) { }
    protected internal virtual void OnDestroy() { }

    internal void EnsureStarted()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        Start();
    }
}
