namespace PageState;

public interface IPageStateProtector
{
    string Protect<T>(T state, string? owner, PageStateSite site = default) where T : notnull;
    PageStateReadResult<T> Unprotect<T>(string? token, string? owner, PageStateSite site = default);
}
