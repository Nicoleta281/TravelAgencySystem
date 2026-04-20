using System;

namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public class BookingStatusPendingHandler : BookingApprovalHandlerBase
    {
        public override BookingApprovalResult Handle(BookingApprovalContext context)
        {
            if (!string.Equals(context.Booking.StatusName, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return new BookingApprovalResult
                {
                    IsApproved = false,
                    Message = "Only pending bookings can be approved."
                };
            }

            return base.Handle(context);
        }
    }
}
