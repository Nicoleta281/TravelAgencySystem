using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models.Locations
{
    public class HotelSearchOption
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string PropertyToken { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public decimal? PricePerNight { get; set; }
        public decimal? TotalPrice { get; set; }

        public int? HotelClass { get; set; }

        public string ThumbnailUrl { get; set; } = string.Empty;

        public string PriceDisplay
        {
            get
            {
                if (PricePerNight.HasValue && PricePerNight.Value > 0)
                    return $"Price per night: {PricePerNight.Value:F0} EUR";

                if (TotalPrice.HasValue && TotalPrice.Value > 0)
                    return $"Total price: {TotalPrice.Value:F0} EUR";

                return "Price: N/A";
            }
        }
    }
}
