using System;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TravelAgency.Core.Data
{
    public static class TravelAgencyDbContextFactory
    {
        public static TravelAgencyDbContext Create()
        {
            var baseConnectionString = ConfigurationManager
                .ConnectionStrings["TravelAgencyDb"]?
                .ConnectionString;

            if (string.IsNullOrWhiteSpace(baseConnectionString))
                throw new InvalidOperationException("Connection string 'TravelAgencyDb' not found.");

            var password = Environment.GetEnvironmentVariable("TRAVEL_AGENCY_DB_PASSWORD");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Variabila de mediu TRAVEL_AGENCY_DB_PASSWORD nu este setata.");

            var csb = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Password = password
            };

            var options = new DbContextOptionsBuilder<TravelAgencyDbContext>()
                .UseNpgsql(
                    csb.ConnectionString,
                    npgsql =>
                    {
                        // Reducează erorile „connection forcibly closed” / timeout-uri scurte la rețea.
                        npgsql.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(20),
                            errorCodesToAdd: null);
                    })
                .Options;

            return new TravelAgencyDbContext(options);
        }
    }
}