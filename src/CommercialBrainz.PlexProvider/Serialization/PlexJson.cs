using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CommercialBrainz.PlexProvider.Serialization;

/// <summary>
/// Serializes Plex responses with Newtonsoft so both <c>guid</c> and <c>Guid</c> can coexist
/// (System.Text.Json treats those names as a case-insensitive collision).
/// </summary>
public static class PlexJson
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = null
        },
        Formatting = Formatting.None
    };

    public static IResult JsonResult(object value)
        => Results.Content(JsonConvert.SerializeObject(value, Settings), "application/json");
}
