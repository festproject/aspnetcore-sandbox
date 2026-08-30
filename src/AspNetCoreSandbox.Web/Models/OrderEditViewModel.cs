using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PageState;

namespace AspNetCoreSandbox.Web.Models;

// One view model, not two — every property classifies itself by its attribute alone.
public sealed class OrderEditViewModel
{
    // form input
    [Required]
    [Display(Name = "Customer Name")]
    public string? CustomerName { get; set; }

    [Required]
    [Display(Name = "Product")]
    public string? Product { get; set; }

    // survives postback
    [PageState]
    public int OrderId { get; set; }

    // rebuilt before every render
    [Hydrated]
    public List<SelectListItem> ProductOptions { get; set; } = [];
}
