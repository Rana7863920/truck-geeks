namespace TruckServices.Models
{
    using System.Text.Json.Serialization;
    public class TruckBusinessStatus
    {
        public bool Found { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public bool? OpenNow { get; set; }
        public string? OpeningState { get; set; }
        public string? HoursText { get; set; }
        public string? Message { get; set; }
    }
}
