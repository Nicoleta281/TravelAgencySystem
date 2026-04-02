using System.IO;
using System.Text;
using TravelAgency.Core.Patterns.Bridge.Models;

namespace TravelAgency.Core.Patterns.Bridge.Exporters
{
    public class CsvReportExporter : IReportExporter
    {
        public string FileExtension => ".csv";

        public string Export(ReportDocument document, string outputPath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("ClientName,PackageName,Destination,Status,BookingDate,TravelPeriod,Price");

            foreach (var row in document.Rows)
            {
                sb.AppendLine(string.Join(",",
                    Escape(row.ClientName),
                    Escape(row.PackageName),
                    Escape(row.Destination),
                    Escape(row.Status),
                    Escape(row.BookingDate),
                    Escape(row.TravelPeriod),
                    Escape(row.Price)));
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            return outputPath;
        }

        private static string Escape(string value)
        {
            value ??= "";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}