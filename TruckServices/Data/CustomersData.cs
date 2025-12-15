using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TruckServices.Data
{
    public class CustomersData
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? MobileNumber { get; set; }
        public string? SecondMobileNumber { get; set; }
        public string? Email { get; set; }
        public string? Source { get; set; }
        public byte[]? ImageUrl { get; set; }   
        public bool IsPaid { get; set; }
        [ValidateNever]
        public ICollection<CompanyService> CompanyServices { get; set; }
    }

}
