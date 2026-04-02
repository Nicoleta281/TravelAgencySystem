using System;
using System.IO;

namespace TravelAgency.Core.Patterns.Bridge.Services
{
    public class ReportPathProvider
    {
        public string GetOutputPath(string reportName, string extension)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TravelAgencySystem",
                "AgentReports");

            Directory.CreateDirectory(folder);

            string safeName = reportName.Replace(" ", "_");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return Path.Combine(folder, $"{safeName}_{timestamp}{extension}");
        }
    }
}