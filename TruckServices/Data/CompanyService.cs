namespace TruckServices.Data
{
    public class CompanyService
    {
        public int CompanyId { get; set; }
        public CustomersData Company { get; set; }

        public int ServiceId { get; set; }
        public Service Service { get; set; }
    }
}
