using Microsoft.Extensions.Options;
using System.Text.Json;
using TruckServices.Models;

namespace TruckServices.Services
{
    public class GeoapifyService
    {
        private readonly string _apiKey;
        private readonly HttpClient _http;
        private readonly string _username;

        public GeoapifyService(IOptions<GeoapifySettings> options)
        {
            _apiKey = options.Value.ApiKey;
            _username = options.Value.Username;
            _http = new HttpClient();
        }

        private async Task<(double? lat, double? lon)> GeocodeAddressAsync(string address)
        {
            string url =
                $"https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(address)}&apiKey={_apiKey}";

            var response = await _http.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var features = doc.RootElement.GetProperty("features");

            if (features.GetArrayLength() == 0)
                return (null, null);

            var coords = features[0].GetProperty("geometry").GetProperty("coordinates");
            double lon = coords[0].GetDouble();
            double lat = coords[1].GetDouble();
            return (lat, lon);
        }

        public async Task<(string city, string state, string country)?> GetNearestDifferentCityAsync(string location, int radiusKm = 50)
        {
            var (lat, lon) = await GeocodeAddressAsync(location);
            if (lat == null || lon == null) return null;

            string url =
                $"http://api.geonames.org/findNearbyPlaceNameJSON?" +
                $"lat={lat.Value}&lng={lon.Value}&radius={radiusKm}&cities=cities15000&username={_username}";

            var resp = await _http.GetAsync(url);
            var json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("geonames", out var arr)) return null;

            foreach (var place in arr.EnumerateArray())
            {
                string city = place.GetProperty("name").GetString() ?? "";
                string country = place.GetProperty("countryName").GetString() ?? "";
                string state = place.TryGetProperty("adminName1", out var s) ? s.GetString() ?? "" : "";

                // Skip same city as user entered
                if (!location.Contains(city, StringComparison.OrdinalIgnoreCase))
                {
                    return (city, state, country);
                }
            }

            return null;
        }

    }
}
