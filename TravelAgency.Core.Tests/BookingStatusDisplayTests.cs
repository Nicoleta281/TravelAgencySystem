using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Services;
using Xunit;

namespace TravelAgency.Core.Tests;

public class BookingStatusDisplayTests
{
    [Theory]
    [InlineData("Pending", "în așteptare")]
    [InlineData("Confirmed", "confirmată")]
    [InlineData("Rejected", "respinsă")]
    [InlineData("Cancelled", "anulată")]
    public void ToRomanian_KnownStatuses(string en, string ro) =>
        Assert.Equal(ro, BookingStatusDisplay.ToRomanian(en));

    [Fact]
    public void AreEquivalent_IgnoresCaseAndWhitespace() =>
        Assert.True(BookingStatusDisplay.AreEquivalent(" pending ", "PENDING"));

    [Fact]
    public void TryCreate_NullEvent_Throws() =>
        Assert.Throws<ArgumentNullException>(() => BookingUpdateNotificationFactory.TryCreate(null!));
}
