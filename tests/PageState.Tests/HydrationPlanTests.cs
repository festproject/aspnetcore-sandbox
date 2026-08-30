using PageState.Internal;

namespace PageState.Tests;

/// <summary>Phase 1 checkpoint: method selection by parameter type, base-class ordering.</summary>
public class HydrationPlanTests
{
    [Fact]
    public void For_SelectsOnlyMethods_WhoseParameterTypeMatchesTheModel()
    {
        var plan = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelY));

        Assert.Single(plan.Methods);
        Assert.Equal("HydrateY", plan.Methods[0].Name);
    }

    [Fact]
    public void For_OrdersBaseClassMethodsBeforeDerivedClassMethods()
    {
        var plan = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelX));

        Assert.Equal(["HydrateBase", "HydrateDerived"], plan.Methods.Select(m => m.Name));
    }

    [Fact]
    public void For_CollectsHydratedProperties()
    {
        var plan = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelX));

        Assert.Equal(["Options"], plan.HydratedProps.Select(p => p.Name));
    }

    [Fact]
    public void For_CollectsUnclassifiedReferenceProperties_ButNotValueTypesOrAttributedOnes()
    {
        var plan = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelX));

        Assert.Equal(["Unclassified"], plan.UnclassifiedRef.Select(p => p.Name));
    }

    [Fact]
    public void For_ExcludesPageStateProperties_FromUnclassified()
    {
        var plan = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelWithPageStateOnly));

        Assert.Empty(plan.UnclassifiedRef);
    }

    [Fact]
    public void IsNoOp_TrueWhenModelHasNoHydratedPropertiesAndNoMatchingMethod()
    {
        var plan = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelNoHydration));

        Assert.True(plan.IsNoOp);
        Assert.Empty(plan.Methods);
        Assert.Empty(plan.HydratedProps);
    }

    [Fact]
    public void For_ReturnsTheSameCachedInstance_OnRepeatedCalls()
    {
        var first = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelX));
        var second = HydrationPlan.For(typeof(PlanHostDerived), typeof(ModelX));

        Assert.Same(first, second);
    }
}
