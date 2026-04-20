using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.State
{
    public interface IBookingState
    {
        string Name { get; }

        void Confirm(Booking booking);
        void Reject(Booking booking);
        void Cancel(Booking booking);
    }
}