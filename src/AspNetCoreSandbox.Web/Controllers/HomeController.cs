using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreSandbox.Web.Models;

namespace AspNetCoreSandbox.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index([FromQuery(Name = "Name")] string? name, [FromQuery(Name = "Age")] int? age, [FromQuery(Name = "Notes")] string? notes)
    {
        return View(new ModelBindingLabInput
        {
            Name = name,
            Age = age,
            Notes = notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ModelBindingLabInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        ViewData["Message"] = "Model binding succeeded.";
        return View(input);
    }

    [HttpGet("Home/RouteVsQuery/{name?}")]
    public IActionResult RouteVsQuery(string? name)
    {
        ViewData["BoundName"] = name;
        ViewData["RouteName"] = RouteData.Values["name"]?.ToString();
        ViewData["QueryName"] = HttpContext.Request.Query["name"].ToString();
        return View();
    }

    [HttpGet("Home/FormVsRoute/{name?}")]
    public IActionResult FormVsRoute(string? name)
    {
        ViewData["BoundName"] = name;
        ViewData["RouteName"] = RouteData.Values["name"]?.ToString();
        ViewData["FormName"] = string.Empty;
        return View();
    }

    [HttpPost("Home/FormVsRoute/{name?}")]
    [ValidateAntiForgeryToken]
    public IActionResult FormVsRoutePost(string? name)
    {
        ViewData["BoundName"] = name;
        ViewData["RouteName"] = RouteData.Values["name"]?.ToString();
        ViewData["FormName"] = HttpContext.Request.Form["name"].ToString();
        ViewData["Message"] = "Posted.";
        return View("FormVsRoute");
    }

    [HttpGet("Home/NoFallback/{age?}")]
    public IActionResult NoFallback(int? age)
    {
        PopulateNoFallbackViewData(age);
        return View();
    }

    [HttpPost("Home/NoFallback/{age?}")]
    [ValidateAntiForgeryToken]
    public IActionResult NoFallbackPost(int? age)
    {
        PopulateNoFallbackViewData(age);
        ViewData["Message"] = "Posted.";
        return View("NoFallback");
    }

    [HttpGet("Home/DuplicateInQuery")]
    public IActionResult DuplicateInQuery(int? age)
    {
        ViewData["BoundAge"] = age?.ToString() ?? "(null)";
        ViewData["QueryAgeValues"] = string.Join(" | ", HttpContext.Request.Query["age"].AsEnumerable());
        ViewData["AgeErrors"] = string.Join(" | ", ModelState.TryGetValue("age", out var entry)
            ? entry.Errors.Select(static e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).Where(static m => !string.IsNullOrWhiteSpace(m))
            : Array.Empty<string>());
        return View();
    }

    [HttpGet("Home/FailureDefinitions")]
    public IActionResult FailureDefinitions([FromQuery] FailureDefinitionsInput input)
    {
        TryValidateModel(input);
        PopulateFailureDefinitionsViewData(input);
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    private void PopulateNoFallbackViewData(int? age)
    {
        ViewData["BoundAge"] = age?.ToString() ?? "(null)";
        ViewData["RouteAge"] = RouteData.Values["age"]?.ToString() ?? string.Empty;
        ViewData["QueryAge"] = HttpContext.Request.Query["age"].ToString();
        ViewData["FormAge"] = HttpContext.Request.HasFormContentType ? HttpContext.Request.Form["age"].ToString() : string.Empty;
        ViewData["AgeErrors"] = string.Join(" | ", ModelState.TryGetValue("age", out var entry)
            ? entry.Errors.Select(static e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).Where(static m => !string.IsNullOrWhiteSpace(m))
            : Array.Empty<string>());
    }

    private void PopulateFailureDefinitionsViewData(FailureDefinitionsInput input)
    {
        ViewData["RequiredOnly"] = input.RequiredOnly ?? "(null)";
        ViewData["BindRequiredOnly"] = input.BindRequiredOnly ?? "(null)";
        ViewData["Optional"] = input.Optional ?? "(null)";
        ViewData["RequiredOnlyErrors"] = string.Join(" | ", ModelState.TryGetValue(nameof(FailureDefinitionsInput.RequiredOnly), out var requiredEntry)
            ? requiredEntry.Errors.Select(static e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).Where(static m => !string.IsNullOrWhiteSpace(m))
            : Array.Empty<string>());
        ViewData["BindRequiredOnlyErrors"] = string.Join(" | ", ModelState.TryGetValue(nameof(FailureDefinitionsInput.BindRequiredOnly), out var bindRequiredEntry)
            ? bindRequiredEntry.Errors.Select(static e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).Where(static m => !string.IsNullOrWhiteSpace(m))
            : Array.Empty<string>());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
