using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace TravelAgency.Core.Data
{
    public class TravelAgencyDbContextDesignFactory : IDesignTimeDbContextFactory<TravelAgencyDbContext>
    {
        public TravelAgencyDbContext CreateDbContext(string[] args)
        {
            var csb = new NpgsqlConnectionStringBuilder
            {
                Host = "aws-1-eu-central-1.pooler.supabase.com",
                Port = 5432,
                Database = "postgres",
                Username = "postgres.ivpwqjneuanpgqxobgxz",
                Password = "nicoleta100dolganiuc#",   
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };

            var options = new DbContextOptionsBuilder<TravelAgencyDbContext>()
                .UseNpgsql(csb.ConnectionString)
                .Options;

            return new TravelAgencyDbContext(options);
        }
    }
}