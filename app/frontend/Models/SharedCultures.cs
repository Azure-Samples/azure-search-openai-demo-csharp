namespace ClientApp.Models;

public record class SharedCultures
{
    [JsonPropertyName("translation")]
    public required IDictionary<string, AzureCulture> AvailableCultures { get; set; }
}
