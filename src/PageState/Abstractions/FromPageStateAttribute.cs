using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PageState;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class FromPageStateAttribute : Attribute, IBindingSourceMetadata
{
    public static readonly BindingSource Source =
        new("PageState", "PageState", isGreedy: true, isFromRequest: true);

    public BindingSource BindingSource => Source;
}
