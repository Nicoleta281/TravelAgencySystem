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
        public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
        public DbSet<PasswordResetLinkTokenEntity> PasswordResetLinkTokens => Set<PasswordResetLinkTokenEntity>();
        public DbSet<UserMessageEntity> UserMessages => Set<UserMessageEntity>();
        public DbSet<ClientPackageFavoriteEntity> ClientPackageFavorites => Set<ClientPackageFavoriteEntity>();

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
                e.Property(x => x.Email).HasMaxLength(254);
                e.Property(x => x.PhoneNumber).HasMaxLength(20);
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.RoleName).IsRequired();

                e.HasIndex(x => x.Username).IsUnique();
                e.HasIndex(x => x.PhoneNumber);
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

            modelBuilder.Entity<PasswordResetTokenEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.UserId).IsRequired();
                e.Property(x => x.CodeHash).IsRequired();
                e.Property(x => x.CreatedAtUtc).IsRequired();
                e.Property(x => x.ExpiresAtUtc).IsRequired();

                e.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            });

            modelBuilder.Entity<PasswordResetLinkTokenEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.UserId).IsRequired();
                e.Property(x => x.TokenHash).IsRequired();
                e.Property(x => x.CreatedAtUtc).IsRequired();
                e.Property(x => x.ExpiresAtUtc).IsRequired();

                e.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
                e.HasIndex(x => x.TokenHash);
            });

            modelBuilder.Entity<UserMessageEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.FromUsername).IsRequired();
                e.Property(x => x.ToUsername).IsRequired();
                e.Property(x => x.Body).IsRequired();
                e.Property(x => x.SentAtUtc).IsRequired();
                e.HasIndex(x => new { x.ToUsername, x.IsRead });
                e.HasIndex(x => new { x.FromUsername, x.ToUsername, x.SentAtUtc });
            });

            modelBuilder.Entity<ClientPackageFavoriteEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.ClientUsername).IsRequired().HasMaxLength(128);
                e.Property(x => x.SavedAtUtc).IsRequired();
                e.HasIndex(x => new { x.ClientUsername, x.TripPackageId }).IsUnique();
            });
        }
    }
}