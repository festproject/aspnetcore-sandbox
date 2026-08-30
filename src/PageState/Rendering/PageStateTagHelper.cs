using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PageState.Internal;

// Must be public: Razor's tag-helper discovery (@addTagHelper *, PageState) only finds publicly
// visible TagHelper-derived types in the target assembly. It is still an internal implementation
// detail otherwise, hence EditorBrowsable(Never) to keep it out of app-facing IntelliSense.
[EditorBrowsable(EditorBrowsableState.Never)]
[HtmlTargetElement("page-state", TagStructure = TagStructure.WithoutEndTag)]
public sealed class PageStateTagHelper : TagHelper
{
    private readonly IPageStateProtector _protector;
    private readonly IPageStateOwnerProvider _ownerProvider;
    private readonly PageStateOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PageStateTagHelper> _logger;

    public PageStateTagHelper(
        IPageStateProtector protector,
        IPageStateOwnerProvider ownerProvider,
        IOptions<PageStateOptions> options,
        IHostEnvironment environment,
        ILogger<PageStateTagHelper> logger)
    {
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
        var model = ViewContext.ViewData.Model;
        if (model is null)
        {
            // No model at all on this render — nothing to find a [PageState] property on.
            output.SuppressOutput();
            return;
        }

        var modelType = model.GetType();
        var properties = PageStateRendering.FindPageStateProperties(modelType);
        if (properties.Length == 0)
        {
            // No [PageState] property declared on this view model — nothing to render.
            output.SuppressOutput();
            return;
        }

        // A model may declare more than one [PageState] property (e.g. two independent bits of
        // state on one page); each gets its own hidden field, so <page-state /> is replaced by
        // however many <input> elements that takes rather than by a single wrapping tag.
        output.TagName = null;

        var owner = _ownerProvider.GetOwner(ViewContext.HttpContext);

        foreach (var property in properties)
        {
            var state = property.GetValue(model);
            if (state is null)
            {
                if (_environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        $"{modelType.Name}.{property.Name} is declared [PageState] but is null at " +
                        "render time. Assign it in your GET handler before returning the view.");
                }

                _logger.LogError(
                    "{ModelType}.{Property} is [PageState] but null at render time; <page-state /> skipped it.",
                    modelType.Name, property.Name);
                continue;
            }

            // Container type is the view model itself, not property.DeclaringType — must match
            // what PageStateModelBinderProvider reads from ModelMetadata.ContainerType, or a
            // token minted here would fail to decrypt when posted back.
            var site = new PageStateSite(modelType, property.Name);
            var token = PageStateRendering.ProtectDynamic(_protector, property.PropertyType, state, owner, site);
            var fieldName = PageStateRendering.FieldNameFor(_options, property.Name);

            output.Content.AppendHtml(PageStateRendering.BuildHiddenInputHtml(fieldName, token));
        }
    }
}
