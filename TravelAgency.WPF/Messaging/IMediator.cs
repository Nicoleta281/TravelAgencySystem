using System;

namespace TravelAgency.WPF.Messaging
{
    public interface IMediator
    {
        IDisposable Subscribe<TMessage>(Action<TMessage> handler);
        void Publish<TMessage>(TMessage message);
    }
}

