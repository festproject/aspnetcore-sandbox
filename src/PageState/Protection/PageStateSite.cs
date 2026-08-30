namespace PageState;

/// <summary>
/// Identifies a [PageState] property by its declaration site — container type plus property
/// name — rather than just its value type. Folded into the Data Protection purpose chain so a
/// token minted for one property can never be accepted by another, even when both happen to
/// declare the same (possibly primitive) type. `default` means "no site" — used by the
/// @Html.PageState escape hatch, which isn't tied to a single model property.
/// </summary>
public readonly record struct PageStateSite(Type ContainerType, string PropertyName);
