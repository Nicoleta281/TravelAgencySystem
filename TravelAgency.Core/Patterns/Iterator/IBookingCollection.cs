using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.Iterator
{
    public interface IBookingCollection
    {
        IIterator<Booking> CreateAllIterator();
        IIterator<Booking> CreatePendingIterator();
        IIterator<Booking> CreateConfirmedIterator();

        IIterator<Booking> CreateRejectedIterator();
    }
}