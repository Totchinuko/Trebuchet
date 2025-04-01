using System.Text.Json.Serialization;

namespace TrebuchetLib.Services;

public class AppSettings
{
    [JsonPropertyName("apikey")]
    public string ApiKey { get; set; } = string.Empty;
}