namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public class ClientExistsHandler : BookingApprovalHandlerBase
    {
        public override BookingApprovalResult Handle(BookingApprovalContext context)
        {
            if (context.Booking.Client == null)
            {
                return new BookingApprovalResult
                {
                    IsApproved = false,
                    Message = "Booking cannot be approved because the client is missing."
                };
            }

            return base.Handle(context);
        }
    }
}
