using TravelAgency.Core.Patterns.ChainOfResponsibility;

namespace TravelAgency.Core.Services
{
    public sealed class BookingApprovalChainFactory
    {
        public IBookingApprovalHandler Create()
        {
            var clientHandler = new ClientExistsHandler();
            var tripHandler = new TripExistsHandler();
            var statusHandler = new BookingStatusPendingHandler();
            var seatsHandler = new SeatsAvailableHandler();
            var priceHandler = new PriceValidationHandler();

            clientHandler
                .SetNext(tripHandler)
                .SetNext(statusHandler)
                .SetNext(seatsHandler)
                .SetNext(priceHandler);

            return clientHandler;
        }
    }
}

