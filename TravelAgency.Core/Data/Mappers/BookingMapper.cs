using System;
using System.Linq;
using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Data.Mappers
{
    public static class BookingMapper
    {
        public static BookingEntity ToEntity(Booking booking)
        {
            return new BookingEntity
            {
                Id = booking.Id,
                BookingDate = booking.BookingDate.Kind == DateTimeKind.Utc
    ? booking.BookingDate
    : booking.BookingDate.ToUniversalTime(),
                TripPackageId = booking.TripPackage?.Id ?? 0,
                TripPackageName = booking.TripPackage?.Name ?? "",
                ClientUsername = booking.Client?.Username ?? "",
                StatusName = booking.Status?.Name ?? "",
                SelectedExtras = string.Join(",", booking.SelectedExtras),
                BasePrice = booking.BasePrice,
                TotalPrice = booking.TotalPrice
            };
        }

        public static Booking FromEntity(BookingEntity entity)
        {
            var booking = new Booking
            {
                Id = entity.Id,
                BookingDate = entity.BookingDate.Kind == DateTimeKind.Utc
    ? entity.BookingDate
    : DateTime.SpecifyKind(entity.BookingDate, DateTimeKind.Utc),
                TripPackage = new TripPackage
                {
                    Id = entity.TripPackageId,
                    Name = entity.TripPackageName
                },
                Client = new Client
                {
                    Username = entity.ClientUsername
                },
                SelectedExtras = string.IsNullOrWhiteSpace(entity.SelectedExtras)
                    ? new System.Collections.Generic.List<string>()
                    : entity.SelectedExtras
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList(),
                BasePrice = entity.BasePrice,
                TotalPrice = entity.TotalPrice
            };

            booking.ChangeStatus(new BookingStatus
            {
                Name = entity.StatusName
            });

            return booking;
        }
    }
}