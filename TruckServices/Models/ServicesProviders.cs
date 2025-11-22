namespace TruckServices.Models
{
    public class ServicesProviders
    {
        // From DB
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string? MobileNumber { get; set; }
        public string SecondMobileNumber { get; set; }
        public string Email { get; set; }
        public string Source { get; set; }
        public string ImageBase64 { get; set; }

        // App-only
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; }
        public string Status { get; set; }
    }

}


