using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCoreSandbox.Web.Models;

public class FailureDefinitionsInput
{
    [Required]
    public string? RequiredOnly { get; set; }

    [BindRequired]
    public string? BindRequiredOnly { get; set; }

    public string? Optional { get; set; }
}
