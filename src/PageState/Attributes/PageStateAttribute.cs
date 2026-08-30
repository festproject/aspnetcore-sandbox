using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PageState;

/// <summary>
/// Declares that this property is created on GET and restored on POST from a
/// protected hidden field, rather than bound from the form.
/// </summary>
/// <remarks>
/// The binding source is greedy: MVC must not recurse into the state type's
/// properties. Without that, form fields such as "State.RowVersion" would be
/// bound directly from the request, bypassing Data Protection entirely.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PageStateAttribute : Attribute, IBindingSourceMetadata
{
    public static readonly BindingSource Source =
        new("PageState", "PageState", isGreedy: true, isFromRequest: true);

    public BindingSource BindingSource => Source;
}
