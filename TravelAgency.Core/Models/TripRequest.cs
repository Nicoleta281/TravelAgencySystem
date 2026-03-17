using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models
{
    public class TripRequest
    {
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public string TripType { get; set; } = "";
        public string TransportType { get; set; } = "";
    }
}
