using TravelAgency.Core.Patterns.Bridge.Models;

namespace TravelAgency.Core.Patterns.Bridge.Exporters
{
    public interface IReportExporter
    {
        string FileExtension { get; }
        string Export(ReportDocument document, string outputPath);
    }
}