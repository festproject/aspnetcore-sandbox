using PageState;

namespace AspNetCoreSandbox.Web.Models;

// Non-editable identity for the PageState demo — never rendered as a form input.
// See PAGESTATE_IMPLEMENTATION_GUIDE.md §5.1.
[PageState("OrderEdit", SchemaVersion = 1)]
public sealed record OrderEditPageState(int OrderId);
