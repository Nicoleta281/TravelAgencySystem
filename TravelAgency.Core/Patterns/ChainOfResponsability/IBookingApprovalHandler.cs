namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public interface IBookingApprovalHandler
    {
        IBookingApprovalHandler SetNext(IBookingApprovalHandler next);
        BookingApprovalResult Handle(BookingApprovalContext context);
    }
}