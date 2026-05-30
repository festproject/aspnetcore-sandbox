using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSandbox.Web.Models;

public class ModelBindingLabInput
{
    [Required]
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Range(1, 120)]
    [Display(Name = "Age")]
    public int? Age { get; set; }

    [StringLength(200)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
