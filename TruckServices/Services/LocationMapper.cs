namespace TruckServices.Services
{
    public static class LocationMapper
    {
        private static readonly Dictionary<string, string> StateMap = new()
    {
        // US States
        { "AL", "Alabama" }, { "AK", "Alaska" }, { "AZ", "Arizona" },
        { "AR", "Arkansas" }, { "CA", "California" }, { "CO", "Colorado" },
        { "CT", "Connecticut" }, { "DE", "Delaware" }, { "FL", "Florida" },
        { "GA", "Georgia" }, { "HI", "Hawaii" }, { "ID", "Idaho" },
        { "IL", "Illinois" }, { "IN", "Indiana" }, { "IA", "Iowa" },
        { "KS", "Kansas" }, { "KY", "Kentucky" }, { "LA", "Louisiana" },
        { "ME", "Maine" }, { "MD", "Maryland" }, { "MA", "Massachusetts" },
        { "MI", "Michigan" }, { "MN", "Minnesota" }, { "MS", "Mississippi" },
        { "MO", "Missouri" }, { "MT", "Montana" }, { "NE", "Nebraska" },
        { "NV", "Nevada" }, { "NH", "New Hampshire" }, { "NJ", "New Jersey" },
        { "NM", "New Mexico" }, { "NY", "New York" }, { "NC", "North Carolina" },
        { "ND", "North Dakota" }, { "OH", "Ohio" }, { "OK", "Oklahoma" },
        { "OR", "Oregon" }, { "PA", "Pennsylvania" }, { "RI", "Rhode Island" },
        { "SC", "South Carolina" }, { "SD", "South Dakota" }, { "TN", "Tennessee" },
        { "TX", "Texas" }, { "UT", "Utah" }, { "VT", "Vermont" },
        { "VA", "Virginia" }, { "WA", "Washington" }, { "WV", "West Virginia" },
        { "WI", "Wisconsin" }, { "WY", "Wyoming" },

        // Canada
        { "AB", "Alberta" }, { "BC", "British Columbia" }, { "MB", "Manitoba" },
        { "NB", "New Brunswick" }, { "NL", "Newfoundland and Labrador" },
        { "NS", "Nova Scotia" }, { "ON", "Ontario" }, { "PE", "Prince Edward Island" },
        { "QC", "Quebec" }, { "SK", "Saskatchewan" }, { "NT", "Northwest Territories" },
        { "NU", "Nunavut" }, { "YT", "Yukon" }
    };

        private static readonly Dictionary<string, HashSet<string>> StateCanonicalToVariants =
    StateMap
        .SelectMany(kvp => new[]
        {
            new { Canonical = kvp.Value, Variant = kvp.Key },
            new { Canonical = kvp.Value, Variant = kvp.Value }
        })
        .GroupBy(x => x.Canonical, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            g => g.Key,
            g => g.Select(x => x.Variant)
                  .ToHashSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase
        );


        public static string MapStateToCanonical(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return StateMap.TryGetValue(input.Trim(), out var full)
                ? full
                : input.Trim();
        }

        public static IReadOnlyCollection<string> GetStateVariants(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<string>();

            var canonical = MapStateToCanonical(input);

            return StateCanonicalToVariants.TryGetValue(canonical, out var variants)
                ? variants
                : new[] { input.Trim() };
        }


        private static readonly Dictionary<string, string> CountryToCanonical =
     new(StringComparer.OrdinalIgnoreCase)
 {
    { "US", "United States" },
    { "USA", "United States" },
    { "UNITED STATES", "United States" },

    { "CA", "Canada" },
    { "CANADA", "Canada" }
 };

        private static readonly Dictionary<string, HashSet<string>> CanonicalToAllVariants =
    CountryToCanonical
        .GroupBy(kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            g => g.Key,
            g => g.Select(x => x.Key)
                  .Append(g.Key) // include canonical itself
                  .ToHashSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase
        );



        public static string MapState(string stateAbbrev) =>
            StateMap.TryGetValue(stateAbbrev.ToUpper(), out var full) ? full : stateAbbrev;

        public static string MapCountryToCanonical(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return CountryToCanonical.TryGetValue(input.Trim(), out var canonical)
                ? canonical
                : input.Trim();
        }

        public static IReadOnlyCollection<string> GetCountryVariants(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<string>();

            var canonical = MapCountryToCanonical(input);

            return CanonicalToAllVariants.TryGetValue(canonical, out var variants)
                ? variants
                : new[] { input.Trim() };
        }

    }

}
