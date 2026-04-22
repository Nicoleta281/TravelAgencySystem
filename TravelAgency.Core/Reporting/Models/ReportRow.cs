using System;

namespace TravelAgency.Core.Reporting.Models
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

