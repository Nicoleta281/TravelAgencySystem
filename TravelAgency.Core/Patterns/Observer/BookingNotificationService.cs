using System.Collections.Generic;
using System.Linq;

namespace TravelAgency.Core.Patterns.Observer
{
    public class BookingNotificationService : IBookingSubject
    {
        public static BookingNotificationService Instance { get; } = new();

        private readonly List<IBookingObserver> _observers = new();

        public void Attach(IBookingObserver observer)
        {
            if (observer == null)
                return;

            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        public void Detach(IBookingObserver observer)
        {
            if (observer == null)
                return;

            _observers.Remove(observer);
        }

        public void Notify(BookingStatusChangedEvent bookingEvent)
        {
            foreach (var observer in _observers.ToList())
            {
                observer.Update(bookingEvent);
            }
        }
    }
}