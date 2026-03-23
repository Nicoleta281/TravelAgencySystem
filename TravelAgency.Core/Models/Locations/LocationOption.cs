namespace TravelAgency.Core.Models.Locations
{
    public class LocationOption
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{City}, {Country}";
        }
    }
}