using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data.Entities;

namespace TravelAgency.Core.Data
{
    public class TravelAgencyDbContext : DbContext
    {
        public DbSet<TripPackageEntity> TripPackages => Set<TripPackageEntity>();
        public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

        public TravelAgencyDbContext(DbContextOptions<TravelAgencyDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TripPackageEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.Name).IsRequired();
                e.Property(x => x.SeasonName).IsRequired();
                e.Property(x => x.TransportType).IsRequired();
                e.Property(x => x.StayType).IsRequired();
            });

            modelBuilder.Entity<BookingEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.TripPackageName).IsRequired();
                e.Property(x => x.ClientUsername).IsRequired();
                e.Property(x => x.StatusName).IsRequired();
                e.Property(x => x.SelectedExtras).IsRequired();
            });
        }
    }
}