using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Patterns.Bridge.Exporters;
using TravelAgency.Core.Patterns.Bridge.Models;

namespace TravelAgency.Core.Patterns.Bridge.Reports
{
    public class AllBookingsReport : AgentReport
    {
        public AllBookingsReport(IReportExporter exporter) : base(exporter)
        {
        }

        protected override ReportDocument BuildDocument(IEnumerable<Booking> bookings, string generatedBy)
        {
            var list = bookings.ToList();

            return new ReportDocument
            {
                Title = "All Bookings Report",
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.Now,
                Summary = $"Total bookings: {list.Count}",
                Rows = list.Select(BookingReportMapper.ToRow).ToList()
            };
        }
    }
}