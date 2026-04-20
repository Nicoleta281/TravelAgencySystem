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
                    Message = "Booking cannot be approved because there are no available seats."
                };
            }

            return base.Handle(context);
        }
    }
}
