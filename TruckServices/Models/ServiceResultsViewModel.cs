namespace TruckServices.Models
{
    public class ServiceResultsViewModel
    {
        public List<ServicesProviders> Providers { get; set; }
        public int CurrentPage { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }

        public string Location { get; set; }
        public string Service { get; set; }
        public string ErrorMessage { get; set; } 

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }


}
