using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using TruckServices.Models;

namespace TruckServices.Services
{
    public class GoogleMapsService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public GoogleMapsService(
            IOptions<GoogleMapsSettings> options,
            HttpClient http)
        {
            _apiKey = options.Value.ApiKey;
            _http = http;
        }

        public async Task<List<(string City, string State, string Country)>> AutocompleteCitiesAsync(string term)
       {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return new List<(string, string, string)>();

            // Build URL
            var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json" +
                      $"?input={Uri.EscapeDataString(term)}" +
                      $"&types=(cities)" +              // cities only
                      $"&components=country:us|country:ca" +
                      $"&key={_apiKey}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<(string, string, string)>();

            var jsonString = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonString).RootElement;

            if (!doc.TryGetProperty("predictions", out JsonElement predictions))
                return new List<(string, string, string)>();

            var results = new List<(string City, string State, string Country)>();

            foreach (var p in predictions.EnumerateArray())
            {
                string city = "";
                string state = "";
                string country = "";

                // Extract structured formatting
                if (p.TryGetProperty("structured_formatting", out var structFmt))
                {
                    city = structFmt.GetProperty("main_text").GetString() ?? "";
                }

                // Optional: Parse secondary text for state/country
                if (p.TryGetProperty("terms", out var terms))
                {
                    var termList = terms.EnumerateArray().Select(t => t.GetProperty("value").GetString()).Where(t => t != null).ToList();

                    // Usually: [City, State, Country]
                    if (termList.Count >= 2)
                    {
                        state = termList[1] ?? "";
                        country = termList.Count >= 3 ? termList[2] ?? "" : "";
                    }
                }

                string mappedCountry = country switch
                {
                    "USA" => "United States",
                    "CA" => "Canada",
                    _ => country
                };

                if (mappedCountry == "United States" || mappedCountry == "Canada")
                {
                    results.Add((city, state, mappedCountry));
                }
            }

            // Remove duplicates and order
            return results
                .Distinct()
                .OrderBy(r => r.City)
                .ToList();
        }

        private async Task<(double lat, double lng)?> GeocodeAsync(string address)
        {
            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json" +
                $"?address={Uri.EscapeDataString(address)}&key={_apiKey}";

            var doc = await _http.GetFromJsonAsync<JsonElement>(url);

            if (!doc.TryGetProperty("results", out var results) ||
                results.GetArrayLength() == 0)
                return null;

            var loc = results[0]
                .GetProperty("geometry")
                .GetProperty("location");

            return (loc.GetProperty("lat").GetDouble(),
                    loc.GetProperty("lng").GetDouble());
        }

        public async Task<List<(string city, string state, string country)>>
    GetNearestCitiesAsync(string location, int radiusKm = 50)
        {
            var coords = await GeocodeAsync(location);
            if (coords == null) return new();

            var requestBody = new PlacesNearbyRequest
            {
                IncludedTypes = new List<string> { "locality" },
                MaxResultCount = 20,
                LocationRestriction = new LocationRestriction
                {
                    Circle = new Circle
                    {
                        Center = new Center
                        {
                            Latitude = coords.Value.lat,
                            Longitude = coords.Value.lng
                        },
                        Radius = radiusKm * 1000
                    }
                }
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://places.googleapis.com/v1/places:searchNearby");

            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add(
                "X-Goog-FieldMask",
                "places.displayName,places.location");

            request.Content = JsonContent.Create(requestBody);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var places = JsonSerializer.Deserialize<PlacesNearbyResponse>(json);

            if (places?.Places == null || places.Places.Count == 0)
                return new();

            var results = new List<(string city, string state, string country)>();

            foreach (var place in places.Places)
            {
                var cityName = place.DisplayName.Text;

                // Skip same city user searched
                if (location.Contains(cityName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // If you STILL want state/country
                var details = await ReverseGeocodeAsync(
                    cityName,
                    place.Location.Latitude,
                    place.Location.Longitude);

                if (details != null)
                    results.Add(details.Value);
            }

            return results;
        }

        public async Task<(string city, string state, string country)?> ReverseGeocode(double lat, double lng)
        {
            string url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_apiKey}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            string city = null, state = null, country = null;

            foreach (var comp in results[0].GetProperty("address_components").EnumerateArray())
            {
                var types = comp.GetProperty("types").EnumerateArray().Select(t => t.GetString());
                if (types.Contains("locality")) city = comp.GetProperty("long_name").GetString();
                if (types.Contains("administrative_area_level_1")) state = comp.GetProperty("long_name").GetString();
                if (types.Contains("country")) country = comp.GetProperty("long_name").GetString();
            }

            if (city != null && state != null && country != null)
                return (city, state, country);

            return null;
        }



        // 3️⃣ Reverse geocode → state & country
        private async Task<(string city, string state, string country)?>
            ReverseGeocodeAsync(string city, double lat, double lng)
        {
            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json" +
                $"?latlng={lat},{lng}&key={_apiKey}";

            var doc = await _http.GetFromJsonAsync<JsonElement>(url);
            if (!doc.TryGetProperty("results", out var results))
                return null;

            string state = "", country = "";

            foreach (var comp in results[0].GetProperty("address_components").EnumerateArray())
            {
                var types = comp.GetProperty("types").EnumerateArray()
                    .Select(x => x.GetString()).ToList();

                if (types.Contains("administrative_area_level_1"))
                    state = comp.GetProperty("long_name").GetString() ?? "";

                if (types.Contains("country"))
                    country = comp.GetProperty("long_name").GetString() ?? "";
            }

            return (city, state, country);
        }

        private async Task<string?> GetPlaceIdAsync(string name, string city, string state, string country)
        {

            string query = $"{name}, {city}, {state}, {country}";
            var url = $"https://maps.googleapis.com/maps/api/place/findplacefromtext/json" +
                      $"?input={Uri.EscapeDataString(query)}" +
                      $"&inputtype=textquery" +
                      $"&fields=place_id" +
                      $"&key={_apiKey}";

            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json).RootElement;

            if (doc.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("place_id", out var placeIdEl))
            {
                return placeIdEl.GetString();
            }

            return null;
        }

        private async Task<string> GetBusinessStatusAsync(string placeId)
        {
            if (string.IsNullOrWhiteSpace(placeId)) return "Unknown";

            var url = $"https://maps.googleapis.com/maps/api/place/details/json" +
                      $"?place_id={placeId}" +
                      $"&fields=business_status,opening_hours" +
                      $"&key={_apiKey}";

            var resp = await _http.GetAsync(url);

            if (!resp.IsSuccessStatusCode) return "Unknown";

            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json).RootElement;

            if (!doc.TryGetProperty("result", out var result)) return "Unknown";

            if (result.TryGetProperty("business_status", out var statusEl))
            {
                string status = statusEl.GetString();
                if (status == "CLOSED_PERMANENTLY") return "Closed Permanently";
                if (status == "CLOSED_TEMPORARILY") return "Closed Temporarily";
            }

            if (result.TryGetProperty("opening_hours", out var hoursEl) &&
                hoursEl.TryGetProperty("open_now", out var openNowEl))
            {
                return openNowEl.GetBoolean() ? "Open" : "Closed";
            }

            return "Unknown";
        }



        public async Task<string> GetBusinessStatusByNameAsync(string name, string city, string state, string country)
        {
            string? placeId = await GetPlaceIdAsync(name, city, state, country);
            if (placeId == null) return "Unknown";

            return await GetBusinessStatusAsync(placeId); 
        }
    }
}
