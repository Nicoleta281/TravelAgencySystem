using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data.Mappers;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Data.Repositories
{
    public class EfBookingRepository : IBookingRepository
    {
        public IReadOnlyList<Booking> GetAll()
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entities = db.Bookings
                .AsNoTracking()
                .OrderByDescending(x => x.BookingDate)
                .ToList();

            return entities.Select(BookingMapper.FromEntity).ToList();
        }

        public IReadOnlyList<Booking> GetByClientUsername(string username)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entities = db.Bookings
                .AsNoTracking()
                .Where(x => x.ClientUsername == username)
                .OrderByDescending(x => x.BookingDate)
                .ToList();

            return entities.Select(BookingMapper.FromEntity).ToList();
        }

        public IReadOnlyList<Booking> GetPending()
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entities = db.Bookings
                .AsNoTracking()
                .Where(x => x.StatusName == "Pending")
                .OrderByDescending(x => x.BookingDate)
                .ToList();

            return entities.Select(BookingMapper.FromEntity).ToList();
        }

        public Booking Add(Booking booking)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = BookingMapper.ToEntity(booking);

            db.Bookings.Add(entity);
            db.SaveChanges();

            booking.Id = entity.Id;
            return booking;
        }

        public void Update(Booking booking)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = db.Bookings.FirstOrDefault(x => x.Id == booking.Id);

            if (entity == null)
                return;

            var mapped = BookingMapper.ToEntity(booking);

            entity.BookingDate = mapped.BookingDate;
            entity.TripPackageId = mapped.TripPackageId;
            entity.TripPackageName = mapped.TripPackageName;
            entity.ClientUsername = mapped.ClientUsername;
            entity.StatusName = mapped.StatusName;
            entity.SelectedExtras = mapped.SelectedExtras;
            entity.BasePrice = mapped.BasePrice;
            entity.TotalPrice = mapped.TotalPrice;

            db.SaveChanges();
        }

        public void Delete(int id)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = db.Bookings.FirstOrDefault(x => x.Id == id);

            if (entity == null)
                return;

            db.Bookings.Remove(entity);
            db.SaveChanges();
        }
    }
}