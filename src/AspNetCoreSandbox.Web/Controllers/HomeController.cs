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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
