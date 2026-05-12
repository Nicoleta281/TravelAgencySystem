using System;
using System.Collections.Generic;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Reporting.Exporters;
using TravelAgency.Core.Reporting.Reports;
using TravelAgency.Core.Reporting.Services;

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
                "Toate rezervările" or "All Bookings" => new AllBookingsReport(exporter),
                "În așteptare" or "Pending Bookings" => new PendingBookingsReport(exporter),
                "Confirmate" or "Confirmed Bookings" => new ConfirmedBookingsReport(exporter),
                "Respinse" or "Rejected Bookings" => new RejectedBookingsReport(exporter),
                _ => throw new ArgumentException($"Unsupported report type: {reportType}")
            };
        }
    }
}