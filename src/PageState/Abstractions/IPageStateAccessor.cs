namespace PageState;

public interface IPageStateAccessor
{
    void Set<T>(T state);
    object? Current { get; }
    Type? CurrentType { get; }
}
