using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Patterns.ChainOfResponsibility;
using TravelAgency.Core.Services;
using Xunit;

namespace TravelAgency.Core.Tests;

public class BookingApprovalChainTests
{
    [Fact]
    public void Chain_Rejects_WhenClientMissing()
    {
        var chain = new BookingApprovalChainFactory().Create();
        var booking = new Booking { TripPackage = new TripPackage { AvailableSeats = 5 }, TotalPrice = 100 };
        var result = chain.Handle(new BookingApprovalContext(booking));

        Assert.False(result.IsApproved);
        Assert.Contains("client", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Chain_Approves_WhenAllChecksPass()
    {
        var chain = new BookingApprovalChainFactory().Create();
        var booking = new Booking
        {
            Client = new Client { Username = "u1" },
            TripPackage = new TripPackage { Name = "X", AvailableSeats = 3 },
            TotalPrice = 250,
        };

        var result = chain.Handle(new BookingApprovalContext(booking));

        Assert.True(result.IsApproved);
        Assert.Contains("passed", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
