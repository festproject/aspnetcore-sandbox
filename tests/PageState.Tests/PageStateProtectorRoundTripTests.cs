using System.Text.Json;

namespace PageState.Tests;

public class PageStateProtectorRoundTripTests
{
    [Fact]
    public void Protect_Unprotect_RoundTrips_NestedArraysNullsAndNonAscii()
    {
        var protector = TestFactory.CreateProtector();
        var state = new RoundTripState(
            Name: "Ünïcödé — 日本語 🎉",
            Numbers: [1, 2, 3, -4, 0],
            Nested: new NestedInfo("nested-value", 42),
            Optional: null);

        var token = protector.Protect(state, owner: "owner-1");
        var result = protector.Unprotect<RoundTripState>(token, owner: "owner-1");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.State);
        var actual = result.State!;
        Assert.Equal(state.Name, actual.Name);
        Assert.Equal(state.Numbers, actual.Numbers);
        Assert.Equal(state.Nested, actual.Nested);
        Assert.Equal(state.Optional, actual.Optional);
    }

    [Fact]
    public void Protect_Unprotect_RoundTrips_WithNullOwner()
    {
        var protector = TestFactory.CreateProtector();
        var state = new WorkflowAStateV1("value");

        var token = protector.Protect(state, owner: null);
        var result = protector.Unprotect<WorkflowAStateV1>(token, owner: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(state, result.State);
    }

    [Fact]
    public void Protect_Succeeds_AtExactMaxPayloadBytes_AndThrows_OneByteOver()
    {
        var options = TestFactory.CreateOptions();
        var baselineBytes = JsonSerializer.SerializeToUtf8Bytes(new SizeTestState(""), options.SerializerOptions).Length;
        options.MaxPayloadBytes = baselineBytes;

        var protector = TestFactory.CreateProtector(options);

        var token = protector.Protect(new SizeTestState(""), owner: null);
        Assert.False(string.IsNullOrEmpty(token));

        var ex = Assert.Throws<PageStateTooLargeException>(() => protector.Protect(new SizeTestState("a"), owner: null));
        Assert.Equal(typeof(SizeTestState), ex.StateType);
        Assert.Equal(options.MaxPayloadBytes, ex.MaxPayloadBytes);
    }

    [Fact]
    public void Unprotect_ReturnsTooLarge_WithoutInvokingProtector_WhenTokenExceedsMaxTokenChars()
    {
        var options = TestFactory.CreateOptions(o => o.MaxTokenChars = 10);
        var spy = new SpyDataProtectionProvider();
        var protector = TestFactory.CreateProtector(options, dataProtectionProvider: spy);

        var oversizedToken = new string('a', 11);

        var result = protector.Unprotect<RoundTripState>(oversizedToken, owner: null);

        Assert.Equal(PageStateStatus.TooLarge, result.Status);
        Assert.Equal(0, spy.CreateProtectorCallCount);
    }
}
