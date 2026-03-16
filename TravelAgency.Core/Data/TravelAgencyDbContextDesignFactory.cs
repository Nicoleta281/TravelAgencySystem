using System;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace TravelAgency.Core.Data
{
    public class TravelAgencyDbContextDesignFactory : IDesignTimeDbContextFactory<TravelAgencyDbContext>
    {
        public TravelAgencyDbContext CreateDbContext(string[] args)
        {
            var baseConnectionString = ConfigurationManager
                .ConnectionStrings["TravelAgencyDb"]
                .ConnectionString;

            var password = Environment.GetEnvironmentVariable("TRAVEL_AGENCY_DB_PASSWORD");

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Variabila de mediu TRAVEL_AGENCY_DB_PASSWORD nu este setata.");
            }

            var csb = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Password = password
            };

            var options = new DbContextOptionsBuilder<TravelAgencyDbContext>()
                .UseNpgsql(csb.ConnectionString)
                .Options;

            return new TravelAgencyDbContext(options);
        }
    }
}