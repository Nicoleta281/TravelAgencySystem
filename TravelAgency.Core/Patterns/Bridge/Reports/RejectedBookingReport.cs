using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Patterns.Bridge.Exporters;
using TravelAgency.Core.Patterns.Bridge.Models;

namespace TravelAgency.Core.Patterns.Bridge.Reports
{
    public class RejectedBookingsReport : AgentReport
    {
        public RejectedBookingsReport(IReportExporter exporter) : base(exporter)
        {
        }

        protected override ReportDocument BuildDocument(IEnumerable<Booking> bookings, string generatedBy)
        {
            var list = bookings
                .Where(b => string.Equals(b.Status?.Name, "Rejected", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ReportDocument
            {
                Title = "Rejected Bookings Report",
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.Now,
                Summary = $"Rejected bookings: {list.Count}",
                Rows = list.Select(BookingReportMapper.ToRow).ToList()
            };
        }
    }
}