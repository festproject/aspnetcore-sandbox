using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSandbox.Web.Models;

// The only type bound via [FromForm] on the PageState demo's POST action — editable fields only.
public sealed class OrderEditInput
{
    [Required]
    [Display(Name = "Customer Name")]
    public string? CustomerName { get; set; }

    [Required]
    [Display(Name = "Product")]
    public string? Product { get; set; }
}
