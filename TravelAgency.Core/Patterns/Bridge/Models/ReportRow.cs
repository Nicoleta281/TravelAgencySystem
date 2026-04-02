using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Bridge.Models
{
    public class ReportRow
    {
        public string ClientName { get; set; } = "";
        public string PackageName { get; set; } = "";
        public string Status { get; set; } = "";
        public string BookingDate { get; set; } = "";
        public string TravelPeriod { get; set; } = "";
        public string Price { get; set; } = "";
        public string Destination { get; set; } = "";
    }
}
