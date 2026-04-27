using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TravelAgency.Core.Data
{
    public class TravelAgencyDbContextDesignFactory : IDesignTimeDbContextFactory<TravelAgencyDbContext>
    {
        public TravelAgencyDbContext CreateDbContext(string[] args)
        {
            var configRoot = FindRepoRoot(Directory.GetCurrentDirectory());
            var apiSettings = configRoot == null
                ? null
                : Path.Combine(configRoot, "TravelAgency.Api", "appsettings.json");

            var cfg = new ConfigurationBuilder()
                .SetBasePath(configRoot ?? Directory.GetCurrentDirectory())
                .AddJsonFile(apiSettings ?? "appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var baseConnectionString = cfg.GetConnectionString("TravelAgencyDb")
                ?? cfg["ConnectionStrings:TravelAgencyDb"]
                ?? throw new InvalidOperationException("Missing ConnectionStrings:TravelAgencyDb for design-time DbContext.");

            var password = Environment.GetEnvironmentVariable("TRAVEL_AGENCY_DB_PASSWORD");
            var csb = new NpgsqlConnectionStringBuilder(baseConnectionString);

            // Allow Password to be either in connection string or in env var.
            if (!string.IsNullOrWhiteSpace(password))
                csb.Password = password;

            var options = new DbContextOptionsBuilder<TravelAgencyDbContext>()
                .UseNpgsql(csb.ConnectionString)
                .Options;

            return new TravelAgencyDbContext(options);
        }

        private static string? FindRepoRoot(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                var sln = Path.Combine(dir.FullName, "TravelAgencySystem.sln");
                if (File.Exists(sln))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }
}