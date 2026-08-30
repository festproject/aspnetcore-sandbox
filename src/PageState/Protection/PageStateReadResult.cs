namespace PageState;

public sealed record PageStateReadResult<T>(PageStateStatus Status, T? State)
{
    public bool IsSuccess => Status == PageStateStatus.Success;
}
