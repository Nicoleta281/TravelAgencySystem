using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Reporting.Exporters;
using TravelAgency.Core.Reporting.Models;

namespace TravelAgency.Core.Reporting.Reports
{
    public class ConfirmedBookingsReport : AgentReport
    {
        public ConfirmedBookingsReport(IReportExporter exporter) : base(exporter)
        {
        }

        protected override IReadOnlyList<Booking> PrepareBookings(IEnumerable<Booking> bookings)
        {
            return bookings
                .Where(b => string.Equals(b.Status?.Name, "Confirmed", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        protected override ReportDocument BuildDocument(IReadOnlyList<Booking> bookings, string generatedBy)
        {
            return new ReportDocument
            {
                Title = "Confirmed Bookings Report",
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.Now,
                Summary = $"Confirmed bookings: {bookings.Count}",
                Rows = bookings.Select(BookingReportMapper.ToRow).ToList()
            };
        }
    }
}

