using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Reporting.Exporters;
using TravelAgency.Core.Reporting.Models;

namespace TravelAgency.Core.Reporting.Reports
{
    public abstract class AgentReport
    {
        protected readonly IReportExporter Exporter;

        protected AgentReport(IReportExporter exporter)
        {
            Exporter = exporter;
        }

        // Template Method:
        // Generate() defines the algorithm skeleton; subclasses override steps.
        protected virtual void ValidateInputs(IEnumerable<Booking> bookings, string generatedBy, string outputPath)
        {
            if (bookings == null)
                throw new System.ArgumentNullException(nameof(bookings));

            if (string.IsNullOrWhiteSpace(generatedBy))
                throw new System.ArgumentException("generatedBy cannot be empty.", nameof(generatedBy));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new System.ArgumentException("outputPath cannot be empty.", nameof(outputPath));
        }

        protected virtual IReadOnlyList<Booking> PrepareBookings(IEnumerable<Booking> bookings)
        {
            return bookings.ToList();
        }

        protected abstract ReportDocument BuildDocument(IReadOnlyList<Booking> bookings, string generatedBy);

        public string Generate(IEnumerable<Booking> bookings, string generatedBy, string outputPath)
        {
            ValidateInputs(bookings, generatedBy, outputPath);
            var prepared = PrepareBookings(bookings);
            var document = BuildDocument(prepared, generatedBy);
            return Exporter.Export(document, outputPath);
        }
    }
}

