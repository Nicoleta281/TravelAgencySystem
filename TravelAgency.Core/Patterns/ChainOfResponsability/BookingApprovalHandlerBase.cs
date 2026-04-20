namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public abstract class BookingApprovalHandlerBase : IBookingApprovalHandler
    {
        private IBookingApprovalHandler? _next;

        public IBookingApprovalHandler SetNext(IBookingApprovalHandler next)
        {
            _next = next;
            return next;
        }

        public virtual BookingApprovalResult Handle(BookingApprovalContext context)
        {
            if (_next != null)
                return _next.Handle(context);

            return new BookingApprovalResult
            {
                IsApproved = true,
                Message = "Booking passed all approval checks."
            };
        }
    }
}