using System;
using System.Collections.Generic;
using System.Linq;

namespace TravelAgency.Core.Patterns.Observer
{
    public class BookingNotificationService : IBookingSubject
    {
        // Backwards-compatible singleton for existing callers.
        // Prefer injecting/owning an instance where possible.
        public static BookingNotificationService Instance { get; } = new();

        private readonly object _gate = new();
        private readonly List<WeakReference<IBookingObserver>> _observers = new();

        public void Attach(IBookingObserver observer)
        {
            if (observer == null) return;
            Subscribe(observer);
        }

        public void Detach(IBookingObserver observer)
        {
            if (observer == null) return;
            Unsubscribe(observer);
        }

        public IDisposable Subscribe(IBookingObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            lock (_gate)
            {
                CleanupDeadObservers_NoLock();

                // Prevent duplicates (including duplicates via different weak refs).
                if (!ContainsObserver_NoLock(observer))
                    _observers.Add(new WeakReference<IBookingObserver>(observer));
            }

            return new Subscription(this, observer);
        }

        public void Unsubscribe(IBookingObserver observer)
        {
            if (observer == null) return;

            lock (_gate)
            {
                for (int i = _observers.Count - 1; i >= 0; i--)
                {
                    if (!_observers[i].TryGetTarget(out var target) || ReferenceEquals(target, observer))
                        _observers.RemoveAt(i);
                }
            }
        }

        public void Notify(BookingStatusChangedEvent bookingEvent)
        {
            if (bookingEvent == null) throw new ArgumentNullException(nameof(bookingEvent));

            IBookingObserver[] snapshot;
            lock (_gate)
            {
                CleanupDeadObservers_NoLock();
                snapshot = _observers
                    .Select(wr => wr.TryGetTarget(out var t) ? t : null)
                    .Where(t => t != null)
                    .Cast<IBookingObserver>()
                    .ToArray();
            }

            foreach (var observer in snapshot)
            {
                observer.Update(bookingEvent);
            }
        }

        private bool ContainsObserver_NoLock(IBookingObserver observer)
        {
            for (int i = 0; i < _observers.Count; i++)
            {
                if (_observers[i].TryGetTarget(out var target) && ReferenceEquals(target, observer))
                    return true;
            }

            return false;
        }

        private void CleanupDeadObservers_NoLock()
        {
            for (int i = _observers.Count - 1; i >= 0; i--)
            {
                if (!_observers[i].TryGetTarget(out _))
                    _observers.RemoveAt(i);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private BookingNotificationService? _service;
            private IBookingObserver? _observer;

            public Subscription(BookingNotificationService service, IBookingObserver observer)
            {
                _service = service;
                _observer = observer;
            }

            public void Dispose()
            {
                var service = _service;
                var observer = _observer;
                _service = null;
                _observer = null;

                if (service != null && observer != null)
                    service.Unsubscribe(observer);
            }
        }
    }
}