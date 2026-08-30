using System.Text.Json;
using System.Text.Json.Serialization;

namespace PageState;

public sealed class PageStateOptions
{
    public int MaxPayloadBytes { get; set; } = 4 * 1024;
    public int MaxTokenChars { get; set; } = 16 * 1024;
    public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromMinutes(30);
    public string FormFieldName { get; set; } = "__pagestate";

    /// <summary>
    /// Claim type read by ClaimsPageStateOwnerProvider to bind a token to a login instance.
    /// The application must issue this as a per-sign-in GUID claim, not the permanent user id.
    /// </summary>
    public string OwnerClaimType { get; set; } = "psid";

    public JsonSerializerOptions SerializerOptions { get; set; } = CreateDefaultJson();

    private static JsonSerializerOptions CreateDefaultJson() => new()
    {
        MaxDepth = 8,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
