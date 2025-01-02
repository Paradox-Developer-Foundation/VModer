namespace VModer.Core.Infrastructure;

public sealed class ResetLazy<T>(Func<T> valueFactory)
{
    public T Value => _lazy.Value;

    private Lazy<T> _lazy = new(valueFactory);

    public void Reset()
    {
        _lazy = new Lazy<T>(valueFactory);
    }
}
