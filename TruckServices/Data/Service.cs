namespace TruckServices.Data
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }

        public ICollection<CompanyService> CompanyServices { get; set; }
    }
}
