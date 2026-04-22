using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Reporting.Exporters;
using TravelAgency.Core.Reporting.Models;

namespace TravelAgency.Core.Reporting.Reports
{
    public class AllBookingsReport : AgentReport
    {
        public AllBookingsReport(IReportExporter exporter) : base(exporter)
        {
        }

        protected override ReportDocument BuildDocument(IReadOnlyList<Booking> bookings, string generatedBy)
        {
            return new ReportDocument
            {
                Title = "All Bookings Report",
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.Now,
                Summary = $"Total bookings: {bookings.Count}",
                Rows = bookings.Select(BookingReportMapper.ToRow).ToList()
            };
        }
    }
}

