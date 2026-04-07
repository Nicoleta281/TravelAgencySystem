using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Observer
{
    public interface IBookingSubject
    {
        void Attach(IBookingObserver observer);
        void Detach(IBookingObserver observer);
        void Notify(BookingStatusChangedEvent bookingEvent);
    }
}
