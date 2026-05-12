using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.Notifications;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Patterns.Observer;
using TravelAgency.Core.Services;
using Xunit;

namespace TravelAgency.Core.Tests;

public class BookingUpdateNotificationFactoryTests
{
    [Fact]
    public void TryCreate_ReturnsNull_WhenStatusUnchanged()
    {
        var booking = new Booking { Id = 1, TripPackage = new TripPackage { Name = "Paris" } };
        var e = new BookingStatusChangedEvent(booking, "Pending", "Pending");

        Assert.Null(BookingUpdateNotificationFactory.TryCreate(e));
    }

    [Fact]
    public void TryCreate_ReturnsNotification_WithRomanianDetail()
    {
        var booking = new Booking { Id = 7, TripPackage = new TripPackage { Name = "Roma" } };
        var e = new BookingStatusChangedEvent(booking, "Pending", "Confirmed");

        var n = BookingUpdateNotificationFactory.TryCreate(e);
        Assert.NotNull(n);
        Assert.Equal(7, n!.BookingId);
        Assert.Contains("Roma", n.Detail, StringComparison.Ordinal);
        Assert.Contains("în așteptare", n.Detail, StringComparison.Ordinal);
        Assert.Contains("confirmată", n.Detail, StringComparison.Ordinal);
        Assert.IsType<BookingUpdateNotification>(n);
    }

    [Fact]
    public void TryCreate_UsesDefaultTripName_WhenPackageMissing()
    {
        var booking = new Booking { Id = 2, TripPackage = null };
        var e = new BookingStatusChangedEvent(booking, "Pending", "Rejected");

        var n = BookingUpdateNotificationFactory.TryCreate(e);
        Assert.NotNull(n);
        Assert.Contains("Pachet", n!.Detail, StringComparison.Ordinal);
    }
}
