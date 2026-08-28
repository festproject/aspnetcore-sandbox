namespace AspNetCoreSandbox.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using AspNetCoreSandbox.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
public class StatefulController : Controller
{
    private readonly ILogger<StatefulController> _logger;

    public StatefulController(
        ILogger<StatefulController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new StatefulViewModel
        {
            CountryOptions = GetCountryOptions()
        });
    }

    [HttpPost]
    public IActionResult Index(StatefulViewModel input)
    {
        if (!ModelState.IsValid)
        {
            // Re-populate the CountryOptions before returning the view
            input.CountryOptions = GetCountryOptions();
            return View(input);
        }

        ViewData["Message"] = "Model binding succeeded.";
        return View(input);
    }

    [HttpGet("Stateful/TempDataDemo")]
    public IActionResult TempDataDemo()
    {
        return View();
    }

    [HttpPost("Stateful/TempDataDemo")]
    [ValidateAntiForgeryToken]
    public IActionResult TempDataDemoPost([FromForm] string? notes)
    {
        TempData["Notes"] = notes;
        return RedirectToAction(nameof(TempDataDemo));
    }

    [HttpPost("Stateful/TempDataDemo/save")]
    [ValidateAntiForgeryToken]
    public IActionResult TempDataDemoSave([FromBody] string? notes)
    {
        TempData["Notes"] = notes;
        return Ok();
    }

    [HttpGet("Stateful/TempDataDemo/load")]
    public IActionResult TempDataDemoLoad()
    {
        var notes = TempData["Notes"] as string;
        return Json(new { notes });
    }

    protected private List<SelectListItem> GetCountryOptions()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "US", Text = "United States" },
            new SelectListItem { Value = "CA", Text = "Canada" },
            new SelectListItem { Value = "GB", Text = "United Kingdom" },
            new SelectListItem { Value = "AU", Text = "Australia" }
        };
    }
}
