using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreSandbox.Web.Models;
using PageState;

namespace AspNetCoreSandbox.Web.Controllers;

public class StatefulController : Controller
{
    private readonly ILogger<StatefulController> _logger;
    private readonly IPageStateAccessor _pageStateAccessor;

    public StatefulController(
        ILogger<StatefulController> logger,
        IPageStateAccessor pageStateAccessor)
    {
        _logger = logger;
        _pageStateAccessor = pageStateAccessor;
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

    [HttpGet("Stateful/PageStateDemo")]
    public IActionResult PageStateDemo()
    {
        var orderId = 123; // stands in for "loaded from the DB" in this sandbox
        _pageStateAccessor.Set(new OrderEditPageState(orderId));

        return View(new OrderEditViewModel
        {
            OrderId = orderId,
            Input = new OrderEditInput(),
            ProductOptions = GetProductOptions()
        });
    }

    [HttpPost("Stateful/PageStateDemo")]
    [ValidateAntiForgeryToken]
    public IActionResult PageStateDemoPost(
        [FromPageState] OrderEditPageState state,
        [FromForm] OrderEditInput input)
    {
        var vm = new OrderEditViewModel
        {
            OrderId = state.OrderId,
            Input = input,
            ProductOptions = GetProductOptions()
        };

        if (!ModelState.IsValid)
        {
            return View("PageStateDemo", vm);
        }

        ViewData["Message"] = $"Order {state.OrderId} updated for {input.CustomerName}.";
        return View("PageStateDemo", vm);
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

    protected private List<SelectListItem> GetProductOptions()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "P1", Text = "Product 1" },
            new SelectListItem { Value = "P2", Text = "Product 2" },
            new SelectListItem { Value = "P3", Text = "Product 3" },
            new SelectListItem { Value = "P4", Text = "Product 4" }
        };
    }
}
