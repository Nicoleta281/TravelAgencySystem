using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Services;
using Xunit;

namespace TravelAgency.Core.Tests;

public class BookingAccessProxyTests
{
    private sealed class FakeAccess : IBookingAccessService
    {
        public List<Booking> Bookings { get; } = new();

        public List<Booking> GetPendingBookings() => Bookings.Where(b => b.StatusName == "Pending").ToList();

        public List<Booking> GetBookingsForCurrentUser() => Bookings.ToList();

        public void SubmitBooking(Booking booking) => Bookings.Add(booking);

        public void ApproveBooking(Booking booking) { }

        public void RejectBooking(Booking booking) { }
    }

    [Fact]
    public void Client_SeesOnlyOwnBookings()
    {
        var fake = new FakeAccess();
        fake.Bookings.Add(new Booking
        {
            Client = new Client { Username = "alice" },
            TripPackage = new TripPackage { Name = "A" },
        });
        fake.Bookings.Add(new Booking
        {
            Client = new Client { Username = "bob" },
            TripPackage = new TripPackage { Name = "B" },
        });

        var client = new Client { Username = "alice" };
        var proxy = new BookingAccessProxy(fake, client);

        var list = proxy.GetBookingsForCurrentUser();

        Assert.Single(list);
        Assert.Equal("alice", list[0].Client?.Username);
    }

    [Fact]
    public void Agent_SeesAllBookings()
    {
        var fake = new FakeAccess();
        fake.Bookings.Add(new Booking { Client = new Client { Username = "a" } });
        fake.Bookings.Add(new Booking { Client = new Client { Username = "b" } });

        var agent = new Agent { Username = "agent1" };
        var proxy = new BookingAccessProxy(fake, agent);

        Assert.Equal(2, proxy.GetBookingsForCurrentUser().Count);
    }

    [Fact]
    public void NonAgent_CannotGetPending()
    {
        var fake = new FakeAccess();
        var client = new Client { Username = "c" };
        var proxy = new BookingAccessProxy(fake, client);

        Assert.Throws<UnauthorizedAccessException>(() => proxy.GetPendingBookings());
    }
}
