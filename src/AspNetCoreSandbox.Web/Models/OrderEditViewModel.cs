using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspNetCoreSandbox.Web.Models;

// View-only composition — never itself bound. The POST action binds OrderEditPageState and
// OrderEditInput directly; this type only carries them plus read-only display data to the view.
public sealed class OrderEditViewModel
{
    public int OrderId { get; set; }

    public OrderEditInput Input { get; set; } = new();

    public List<SelectListItem> ProductOptions { get; set; } = [];
}
