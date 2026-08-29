namespace PageState;

public interface IPageStateProtector
{
    string Protect<T>(T state, string? owner);
    PageStateReadResult<T> Unprotect<T>(string? token, string? owner);
}
