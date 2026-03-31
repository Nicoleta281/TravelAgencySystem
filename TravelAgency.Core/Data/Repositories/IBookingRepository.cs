using System.Collections.Generic;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Data.Repositories
{
    public interface IBookingRepository
    {
        IReadOnlyList<Booking> GetAll();
        IReadOnlyList<Booking> GetByClientUsername(string username);
        IReadOnlyList<Booking> GetPending();
        Booking Add(Booking booking);
        void Update(Booking booking);
        void Delete(int id);
    }
}