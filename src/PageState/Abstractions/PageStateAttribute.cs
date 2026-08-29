namespace PageState;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class PageStateAttribute : Attribute
{
    public PageStateAttribute(string workflow) => Workflow = workflow;

    public string Workflow { get; }
    public int SchemaVersion { get; init; } = 1;
    public int? LifetimeMinutes { get; init; }
}
