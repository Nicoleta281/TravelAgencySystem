using System;
using System.Collections.Generic;

namespace TravelAgency.Core.Patterns.Bridge.Models
{
    public class ReportDocument
    {
        public string Title { get; set; } = "";
        public string GeneratedBy { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
        public string Summary { get; set; } = "";
        public List<ReportRow> Rows { get; set; } = new();
    }
}
