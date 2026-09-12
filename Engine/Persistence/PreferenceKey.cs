namespace Graphite.Engine.Persistence;

public sealed class PreferenceKey<T>
{
    public string Name { get; }
    public T DefaultValue { get; }
    internal string TypeTag { get; }
    private readonly Func<T, bool> _validate;

    public PreferenceKey(string name, T defaultValue, Func<T, bool>? validate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Preference keys cannot be empty.", nameof(name));
        }

        TypeTag = typeof(T) == typeof(bool) ? "bool" : typeof(T) == typeof(int) ? "int"
            : typeof(T) == typeof(long) ? "long" : typeof(T) == typeof(float) ? "float"
            : typeof(T) == typeof(double) ? "double" : typeof(T) == typeof(string) ? "string"
            : throw new NotSupportedException("Preferences support bool, int, long, float, double and string values.");
        Name = name;
        DefaultValue = defaultValue;
        _validate = validate ?? (_ => true);
        if (!IsValid(defaultValue))
        {
            throw new ArgumentException("The preference default is invalid.", nameof(defaultValue));
        }
    }

    internal bool IsValid(T value) => value is not null
        && (value is not float number || float.IsFinite(number))
        && (value is not double number64 || double.IsFinite(number64)) && _validate(value);
}
