using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Reporting.Models;

namespace TravelAgency.Core.Reporting.Reports
{
    internal static class BookingReportMapper
    {
        public static ReportRow ToRow(Booking booking)
        {
            string clientName = booking.Client?.Username ?? "-";
            string packageName = booking.TripPackage?.Name ?? "-";
            string destination = booking.TripPackage?.Destination ?? "-";
            string status = booking.Status?.Name ?? "-";
            string bookingDate = booking.BookingDate.ToString("dd.MM.yyyy");

            string travelPeriod = "-";
            if (booking.TripPackage?.Season != null)
            {
                travelPeriod =
                    $"{booking.TripPackage.Season.StartDate:dd.MM.yyyy} - {booking.TripPackage.Season.EndDate:dd.MM.yyyy}";
            }

            string price = $"{booking.TotalPrice:0.00} EUR";

            return new ReportRow
            {
                ClientName = clientName,
                PackageName = packageName,
                Destination = destination,
                Status = status,
                BookingDate = bookingDate,
                TravelPeriod = travelPeriod,
                Price = price
            };
        }
    }
}

