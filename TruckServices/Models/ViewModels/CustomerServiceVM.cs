using TruckServices.Data;

namespace TruckServices.Models.ViewModels
{
    public class CustomerServiceVM
    {
        public CustomersData Customer { get; set; }

        // All services for display
        public List<ServiceCheckboxVM> Services { get; set; } = new();
    }
}
