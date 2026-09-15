using System.Reflection;

namespace Graphite.Engine.Core;

public abstract class Singleton<T> where T : Singleton<T>
{
    private static readonly Lazy<T> LazyInstance = new(CreateInstance);

    public static T Instance => LazyInstance.Value;

    protected Singleton()
    {
    }

    private static T CreateInstance()
    {
        var constructor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        if (constructor is null)
        {
            throw new InvalidOperationException(
                $"Singleton type '{typeof(T).FullName}' must have a parameterless constructor.");
        }

        return (T)constructor.Invoke(null);
    }
}
