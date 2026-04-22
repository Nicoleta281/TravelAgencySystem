using TravelAgency.Core.Reporting.Models;

namespace TravelAgency.Core.Reporting.Exporters
{
    public interface IReportExporter
    {
        string FileExtension { get; }
        string Export(ReportDocument document, string outputPath);
    }
}

