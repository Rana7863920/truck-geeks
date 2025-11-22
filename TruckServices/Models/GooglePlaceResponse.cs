namespace TruckServices.Models
{
    using System.Text.Json.Serialization;

    public class NewPlacesResponse
    {
        [JsonPropertyName("places")]
        public List<NewPlace> Places { get; set; }
    }

    public class NewPlace
    {
        [JsonPropertyName("displayName")]
        public DisplayName DisplayName { get; set; }

        [JsonPropertyName("businessStatus")]
        public string BusinessStatus { get; set; }

        [JsonPropertyName("openingHours")]
        public OpeningHours OpeningHours { get; set; }
    }

    public class DisplayName
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class OpeningHours
    {
        [JsonPropertyName("openNow")]
        public bool? OpenNow { get; set; }
    }

}
