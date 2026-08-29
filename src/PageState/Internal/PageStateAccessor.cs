namespace PageState.Internal;

internal sealed class PageStateAccessor : IPageStateAccessor
{
    private object? _current;
    private Type? _currentType;

    public void Set<T>(T state)
    {
        _current = state;
        _currentType = typeof(T);
    }

    public object? Current => _current;
    public Type? CurrentType => _currentType;
}
