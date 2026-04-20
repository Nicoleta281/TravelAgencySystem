namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public class TripExistsHandler : BookingApprovalHandlerBase
    {
        public override BookingApprovalResult Handle(BookingApprovalContext context)
        {
            if (context.Booking.TripPackage == null)
            {
                return new BookingApprovalResult
                {
                    IsApproved = false,
                    Message = "Booking cannot be approved because the trip package is missing."
                };
            }

            return base.Handle(context);
        }
    }
}