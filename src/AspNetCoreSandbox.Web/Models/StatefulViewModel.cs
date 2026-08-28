using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspNetCoreSandbox.Web.Models;

public class StatefulViewModel
{
    [Required]
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Range(1, 120)]
    [Display(Name = "Age")]
    public int? Age { get; set; }

    [Required]
    [Display(Name = "Country")]
    public string? Country { get; set; }

    [BindNever]
    public List<SelectListItem> CountryOptions { get; set; } = [];

    [StringLength(200)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
