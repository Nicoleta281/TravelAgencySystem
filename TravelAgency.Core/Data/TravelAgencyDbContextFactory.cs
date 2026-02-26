using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;

namespace TravelAgency.Core.Data
{
    public static class TravelAgencyDbContextFactory
    {
        public static TravelAgencyDbContext Create()
        {
            var conn = ConfigurationManager.ConnectionStrings["TravelAgencyDb"]?.ConnectionString;

            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Connection string 'TravelAgencyDb' not found.");

            var options = new DbContextOptionsBuilder<TravelAgencyDbContext>()
                .UseNpgsql(conn)
                .Options;

            return new TravelAgencyDbContext(options);
        }
    }
}