using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PageState.Internal;

/// <summary>
/// Shared markup-building logic behind both entry points: the &lt;page-state /&gt; tag helper
/// (which only has a model instance and must find its [PageState] property by reflection) and
/// @Html.PageState&lt;T&gt; (which has a compile-time T and can call IPageStateProtector.Protect&lt;T&gt;
/// directly).
/// </summary>
internal static class PageStateRendering
{
    private static readonly MethodInfo ProtectMethodDefinition =
        typeof(IPageStateProtector).GetMethod(nameof(IPageStateProtector.Protect))!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> ProtectMethodCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PageStatePropertiesCache = new();

    /// <summary>
    /// Finds every [PageState] property on a model type, cached per type — this is the reflection
    /// call site the guide flags in §4.4: the tag helper only has a model instance, not a
    /// compile-time T, so it needs a cached generic dispatcher rather than MakeGenericMethod per
    /// request. A model may declare more than one [PageState] property; each gets its own field.
    /// </summary>
    internal static PropertyInfo[] FindPageStateProperties(Type modelType)
        => PageStatePropertiesCache.GetOrAdd(modelType, static type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(PageStateAttribute), inherit: true))
            .ToArray());

    internal static string FieldNameFor(PageStateOptions options, string propertyName)
        => $"{options.FormFieldName}.{propertyName}";

    internal static string ProtectDynamic(IPageStateProtector protector, Type stateType, object state, string? owner, PageStateSite site)
    {
        var method = ProtectMethodCache.GetOrAdd(stateType, static (type, definition) => definition.MakeGenericMethod(type), ProtectMethodDefinition);
        return (string)method.Invoke(protector, [state, owner, site])!;
    }

    internal static void RenderHiddenInput(TagHelperOutput output, string fieldName, string token)
    {
        output.TagName = "input";
        output.TagMode = TagMode.SelfClosing;
        output.Attributes.SetAttribute("type", "hidden");
        output.Attributes.SetAttribute("name", fieldName);
        output.Attributes.SetAttribute("value", token);
        // Required, not cosmetic: Firefox restores form control values on reload/session-restore,
        // which can pair a hidden field from one render with HTML from another render — the classic
        // __VIEWSTATE-mismatch bug. It looks removable. It is not.
        output.Attributes.SetAttribute("autocomplete", "off");
    }

    internal static IHtmlContent BuildHiddenInputHtml(string fieldName, string token)
    {
        var tag = new TagBuilder("input")
        {
            TagRenderMode = TagRenderMode.SelfClosing
        };
        tag.Attributes["type"] = "hidden";
        tag.Attributes["name"] = fieldName;
        tag.Attributes["value"] = token;
        tag.Attributes["autocomplete"] = "off";
        return tag;
    }
}
