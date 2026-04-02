using System;
using System.Collections.Generic;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Patterns.Bridge.Exporters;
using TravelAgency.Core.Patterns.Bridge.Reports;
using TravelAgency.Core.Patterns.Bridge.Services;

namespace TravelAgency.Core.Services
{
    public class AgentReportService
    {
        private readonly ReportPathProvider _pathProvider = new();

        public string GenerateReport(
            string reportType,
            string exportFormat,
            IEnumerable<Booking> bookings,
            string generatedBy)
        {
            IReportExporter exporter = CreateExporter(exportFormat);
            AgentReport report = CreateReport(reportType, exporter);

            string outputPath = _pathProvider.GetOutputPath(reportType, exporter.FileExtension);

            return report.Generate(bookings, generatedBy, outputPath);
        }

        private static IReportExporter CreateExporter(string exportFormat)
        {
            return exportFormat switch
            {
                "PDF" => new PdfReportExporter(),
                "CSV" => new CsvReportExporter(),
                "TXT" => new TxtReportExporter(),
                _ => throw new ArgumentException($"Unsupported export format: {exportFormat}")
            };
        }

        private static AgentReport CreateReport(string reportType, IReportExporter exporter)
        {
            return reportType switch
            {
                "All Bookings" => new AllBookingsReport(exporter),
                "Pending Bookings" => new PendingBookingsReport(exporter),
                "Confirmed Bookings" => new ConfirmedBookingsReport(exporter),
                "Rejected Bookings" => new RejectedBookingsReport(exporter),
                _ => throw new ArgumentException($"Unsupported report type: {reportType}")
            };
        }
    }
}