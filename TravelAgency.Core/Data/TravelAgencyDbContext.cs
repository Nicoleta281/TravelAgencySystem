using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data.Entities;

namespace TravelAgency.Core.Data
{
    public class TravelAgencyDbContext : DbContext
    {
        public DbSet<TripPackageEntity> TripPackages => Set<TripPackageEntity>();
        public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
        public DbSet<AdminAnalyticsSnapshotEntity> AdminAnalyticsSnapshots { get; set; }
        public DbSet<UserEntity> Users => Set<UserEntity>();

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

            modelBuilder.Entity<UserEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.Username).IsRequired();
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.RoleName).IsRequired();

                e.HasIndex(x => x.Username).IsUnique();
            });
            modelBuilder.Entity<AdminAnalyticsSnapshotEntity>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.TopDestination)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(x => x.SavedAt)
                      .IsRequired();
            });
        }
    }
}