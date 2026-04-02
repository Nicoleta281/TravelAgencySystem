using System.Collections.Generic;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Patterns.Bridge.Exporters;
using TravelAgency.Core.Patterns.Bridge.Models;

namespace TravelAgency.Core.Patterns.Bridge.Reports
{
    public abstract class AgentReport
    {
        protected readonly IReportExporter Exporter;

        protected AgentReport(IReportExporter exporter)
        {
            Exporter = exporter;
        }

        protected abstract ReportDocument BuildDocument(IEnumerable<Booking> bookings, string generatedBy);

        public string Generate(IEnumerable<Booking> bookings, string generatedBy, string outputPath)
        {
            var document = BuildDocument(bookings, generatedBy);
            return Exporter.Export(document, outputPath);
        }
    }
}