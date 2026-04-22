namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public class SeatsAvailableHandler : BookingApprovalHandlerBase
    {
        public override BookingApprovalResult Handle(BookingApprovalContext context)
        {
            var trip = context.Booking.TripPackage;

            if (trip == null || trip.AvailableSeats <= 0)
            {
                return new BookingApprovalResult
                {
                    IsApproved = false,
                    Message = "Aprobarea nu este posibilă: pachetul nu mai are locuri disponibile (capacitate epuizată)."
                };
            }

            return base.Handle(context);
        }
    }
}
