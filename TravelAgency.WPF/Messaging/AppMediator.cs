using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TravelAgency.WPF.Messaging
{
    public sealed class AppMediator : IMediator
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

        public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var list = _handlers.GetOrAdd(typeof(TMessage), _ => new List<Delegate>());
            lock (list)
            {
                list.Add(handler);
            }

            return new Subscription(() =>
            {
                if (_handlers.TryGetValue(typeof(TMessage), out var handlers))
                {
                    lock (handlers)
                    {
                        handlers.Remove(handler);
                    }
                }
            });
        }

        public void Publish<TMessage>(TMessage message)
        {
            if (!_handlers.TryGetValue(typeof(TMessage), out var handlers))
                return;

            Delegate[] snapshot;
            lock (handlers)
            {
                snapshot = handlers.ToArray();
            }

            foreach (var d in snapshot)
            {
                if (d is Action<TMessage> action)
                    action(message);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _dispose;
            private bool _isDisposed;

            public Subscription(Action dispose) => _dispose = dispose;

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _dispose();
            }
        }
    }
}

