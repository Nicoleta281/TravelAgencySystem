using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Reporting.Exporters;
using TravelAgency.Core.Reporting.Models;

namespace TravelAgency.Core.Reporting.Reports
{
    public class RejectedBookingsReport : AgentReport
    {
        public RejectedBookingsReport(IReportExporter exporter) : base(exporter)
        {
        }

        protected override IReadOnlyList<Booking> PrepareBookings(IEnumerable<Booking> bookings)
        {
            return bookings
                .Where(b => string.Equals(b.Status?.Name, "Rejected", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        protected override ReportDocument BuildDocument(IReadOnlyList<Booking> bookings, string generatedBy)
        {
            return new ReportDocument
            {
                Title = "Rejected Bookings Report",
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.Now,
                Summary = $"Rejected bookings: {bookings.Count}",
                Rows = bookings.Select(BookingReportMapper.ToRow).ToList()
            };
        }
    }
}

