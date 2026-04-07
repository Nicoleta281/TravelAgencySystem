using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Observer
{
    public interface IBookingObserver
    {
        void Update(BookingStatusChangedEvent bookingEvent);
    }
}
