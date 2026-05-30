using System.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AspNetCoreSandbox.Web.Models;

namespace AspNetCoreSandbox.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAntiforgery _antiforgery;
    private readonly AntiforgeryOptions _antiforgeryOptions;

    public HomeController(ILogger<HomeController> logger, IAntiforgery antiforgery, IOptions<AntiforgeryOptions> antiforgeryOptions)
    {
        _logger = logger;
        _antiforgery = antiforgery;
        _antiforgeryOptions = antiforgeryOptions.Value;
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

    [HttpGet("Home/EmptyVsMissing/{age?}")]
    public IActionResult EmptyVsMissing(int? age)
    {
        PopulateEmptyVsMissingViewData(age);
        return View();
    }

    [HttpPost("Home/EmptyVsMissing/{age?}")]
    [ValidateAntiForgeryToken]
    public IActionResult EmptyVsMissingPost(int? age)
    {
        PopulateEmptyVsMissingViewData(age);
        ViewData["Message"] = "Posted.";
        return View("EmptyVsMissing");
    }

    [HttpGet("Home/BodyVsRouteQuery/{routeAge?}")]
    public IActionResult BodyVsRouteQuery(int? routeAge)
    {
        ViewData["RouteAge"] = routeAge?.ToString() ?? "(null)";
        ViewData["QueryAge"] = HttpContext.Request.Query["age"].ToString();
        return View();
    }

    [HttpPost("Home/BodyVsRouteQuery/{routeAge?}")]
    public IActionResult BodyVsRouteQueryPost([FromBody] BodyBindingLabInput? input, int? routeAge)
    {
        return Json(new
        {
            boundAge = input?.Age?.ToString() ?? "(null)",
            routeAge = routeAge?.ToString() ?? "(null)",
            queryAge = HttpContext.Request.Query["age"].ToString(),
            modelStateErrors = ModelState.Select(static kvp => new
            {
                key = kvp.Key,
                errors = kvp.Value!.Errors.Select(static error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.Exception?.Message : error.ErrorMessage)
                    .Where(static message => !string.IsNullOrWhiteSpace(message))
                    .ToArray()
            }).Where(static entry => entry.errors.Length > 0).ToArray()
        });
    }

    [HttpGet("Home/AntiForgeryTokenSource")]
    public IActionResult AntiForgeryTokenSource()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        ViewData["FormFieldName"] = tokens.FormFieldName;
        ViewData["HeaderName"] = tokens.HeaderName ?? _antiforgeryOptions.HeaderName ?? "RequestVerificationToken";
        ViewData["RequestToken"] = tokens.RequestToken ?? string.Empty;
        ViewData["CookieTokenPresent"] = string.IsNullOrWhiteSpace(tokens.CookieToken) ? "false" : "true";
        return View();
    }

    [HttpPost("Home/AntiForgeryTokenSource")]
    [ValidateAntiForgeryToken]
    public IActionResult AntiForgeryTokenSourcePost([FromForm] string? scenario)
    {
        var headerName = _antiforgeryOptions.HeaderName ?? "RequestVerificationToken";
        var formFieldName = _antiforgeryOptions.FormFieldName;
        var hasHeaderToken = Request.Headers.ContainsKey(headerName);
        var hasFormToken = Request.HasFormContentType && Request.Form.ContainsKey(formFieldName);

        return Json(new
        {
            status = 200,
            scenario = scenario ?? string.Empty,
            headerName,
            formFieldName,
            hasHeaderToken,
            hasFormToken,
            formTokenPreview = hasFormToken ? Request.Form[formFieldName].ToString() : string.Empty
        });
    }

    [HttpGet("Home/AntiForgeryFailureModes")]
    public IActionResult AntiForgeryFailureModes()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        ViewData["FormFieldName"] = tokens.FormFieldName;
        ViewData["HeaderName"] = tokens.HeaderName ?? _antiforgeryOptions.HeaderName ?? "RequestVerificationToken";
        ViewData["CookieName"] = _antiforgeryOptions.Cookie.Name ?? string.Empty;
        ViewData["RequestToken"] = tokens.RequestToken ?? string.Empty;
        return View();
    }

    [HttpPost("Home/AntiForgeryFailureModes")]
    public async Task<IActionResult> AntiForgeryFailureModesPost([FromForm] string? scenario)
    {
        var headerName = _antiforgeryOptions.HeaderName ?? "RequestVerificationToken";
        var formFieldName = _antiforgeryOptions.FormFieldName;
        var cookieName = _antiforgeryOptions.Cookie.Name ?? string.Empty;

        var hasHeaderToken = Request.Headers.ContainsKey(headerName);
        var hasFormToken = Request.HasFormContentType && Request.Form.ContainsKey(formFieldName);
        var hasCookieToken = Request.Cookies.ContainsKey(cookieName);

        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);

            return Json(new
            {
                status = 200,
                scenario = scenario ?? string.Empty,
                valid = true,
                message = "Validation succeeded.",
                headerName,
                formFieldName,
                cookieName,
                hasHeaderToken,
                hasFormToken,
                hasCookieToken
            });
        }
        catch (AntiforgeryValidationException exception)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;

            return Json(new
            {
                status = 400,
                scenario = scenario ?? string.Empty,
                valid = false,
                message = exception.Message,
                headerName,
                formFieldName,
                cookieName,
                hasHeaderToken,
                hasFormToken,
                hasCookieToken
            });
        }
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

    [HttpGet("Home/DuplicateInForm")]
    public IActionResult DuplicateInForm()
    {
        PopulateDuplicateInFormViewData(age: null);
        return View();
    }

    [HttpPost("Home/DuplicateInForm")]
    [ValidateAntiForgeryToken]
    public IActionResult DuplicateInFormPost(int? age)
    {
        PopulateDuplicateInFormViewData(age);
        ViewData["Message"] = "Posted.";
        return View("DuplicateInForm");
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

    private void PopulateEmptyVsMissingViewData(int? age)
    {
        ViewData["BoundAge"] = age?.ToString() ?? "(null)";
        ViewData["RouteAge"] = RouteData.Values["age"]?.ToString() ?? string.Empty;
        ViewData["QueryAge"] = HttpContext.Request.Query["age"].ToString();

        if (HttpContext.Request.HasFormContentType)
        {
            ViewData["HasFormAgeKey"] = HttpContext.Request.Form.ContainsKey("age") ? "true" : "false";
            ViewData["FormAgeValues"] = string.Join(" | ", HttpContext.Request.Form["age"].AsEnumerable());
        }
        else
        {
            ViewData["HasFormAgeKey"] = "false";
            ViewData["FormAgeValues"] = string.Empty;
        }

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

    private void PopulateDuplicateInFormViewData(int? age)
    {
        ViewData["BoundAge"] = age?.ToString() ?? "(null)";
        ViewData["FormAgeValues"] = HttpContext.Request.HasFormContentType
            ? string.Join(" | ", HttpContext.Request.Form["age"].AsEnumerable())
            : string.Empty;
        ViewData["AgeErrors"] = string.Join(" | ", ModelState.TryGetValue("age", out var entry)
            ? entry.Errors.Select(static e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).Where(static m => !string.IsNullOrWhiteSpace(m))
            : Array.Empty<string>());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
