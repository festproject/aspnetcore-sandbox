using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

// Must be public: Razor's tag-helper discovery (@addTagHelper *, PageState) only finds publicly
// visible TagHelper-derived types in the target assembly. An internal class here compiles fine
// and silently never renders anything.
[HtmlTargetElement("page-state", TagStructure = TagStructure.WithoutEndTag)]
public sealed class PageStateTagHelper : TagHelper
{
    private readonly IPageStateAccessor _accessor;
    private readonly IPageStateProtector _protector;
    private readonly IPageStateOwnerProvider _ownerProvider;
    private readonly PageStateOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PageStateTagHelper> _logger;

    public PageStateTagHelper(
        IPageStateAccessor accessor,
        IPageStateProtector protector,
        IPageStateOwnerProvider ownerProvider,
        IOptions<PageStateOptions> options,
        IHostEnvironment environment,
        ILogger<PageStateTagHelper> logger)
    {
        _accessor = accessor;
        _protector = protector;
        _ownerProvider = ownerProvider;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var state = _accessor.Current;
        var stateType = _accessor.CurrentType;

        if (state is null || stateType is null)
        {
            if (_environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "No PageState was set for this render; call IPageStateAccessor.Set(state) in " +
                    "your GET handler, or use @Html.PageState(state) for additional forms on the same page.");
            }

            _logger.LogError("No PageState was set for this render; <page-state /> emitted nothing.");
            output.SuppressOutput();
            return;
        }

        var owner = _ownerProvider.GetOwner(ViewContext.HttpContext);
        var token = PageStateRendering.ProtectDynamic(_protector, stateType, state, owner);

        PageStateRendering.RenderHiddenInput(output, _options.FormFieldName, token);
    }
}
