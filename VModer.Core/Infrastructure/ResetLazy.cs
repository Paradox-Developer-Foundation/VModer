using System.Diagnostics.CodeAnalysis;

namespace VModer.Core.Infrastructure;

public sealed class ResetLazy<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T
>(Func<T> valueFactory)
{
    public T Value => _lazy.Value;

    private Lazy<T> _lazy = new(valueFactory);

    public void Reset()
    {
        if (_lazy.IsValueCreated)
        {
            _lazy = new Lazy<T>(valueFactory);
        }
    }
}
