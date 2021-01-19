using System.Text.Json.Serialization;

namespace RadzenBlazorDemos.Models.GitHub
{
    public class Label
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }
    }
}