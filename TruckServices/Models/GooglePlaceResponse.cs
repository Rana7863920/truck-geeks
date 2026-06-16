namespace TruckServices.Models
{
    using System.Text.Json.Serialization;

    public class PlacesNearbyResponse
    {
        [JsonPropertyName("places")]
        public List<Place> Places { get; set; } = new();
    }

    public class Place
    {
        [JsonPropertyName("location")]
        public PlaceLocation Location { get; set; } = new();

        [JsonPropertyName("displayName")]
        public DisplayName DisplayName { get; set; } = new();
    }


    public class DisplayName
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = "";
    }


    public class PlaceLocation
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }


    public class PlacesNearbyRequest
    {
        public LocationRestriction LocationRestriction { get; set; } = new();
        public List<string> IncludedTypes { get; set; } = new();
        public int MaxResultCount { get; set; } = 5;
    }

    public class LocationRestriction
    {
        public Circle Circle { get; set; } = new();
    }

    public class Circle
    {
        public Center Center { get; set; } = new();
        public double Radius { get; set; }
    }

    public class Center
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}
