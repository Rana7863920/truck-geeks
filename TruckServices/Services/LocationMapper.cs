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

        private static readonly Dictionary<string, string> CountryMap = new()
    {
        { "USA", "United States" },
        { "US", "United States" },
        { "CA", "Canada" }
    };

        public static string MapState(string stateAbbrev) =>
            StateMap.TryGetValue(stateAbbrev.ToUpper(), out var full) ? full : stateAbbrev;

        public static string MapCountry(string countryAbbrev) =>
            CountryMap.TryGetValue(countryAbbrev.ToUpper(), out var full) ? full : countryAbbrev;
    }

}
