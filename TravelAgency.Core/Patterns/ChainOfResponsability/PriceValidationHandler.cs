namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public class PriceValidationHandler : BookingApprovalHandlerBase
    {
        public override BookingApprovalResult Handle(BookingApprovalContext context)
        {
            if (context.Booking.TotalPrice <= 0)
            {
                return new BookingApprovalResult
                {
                    IsApproved = false,
                    Message = "Booking cannot be approved because the total price is invalid."
                };
            }

            return base.Handle(context);
        }
    }
}