namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public class BookingApprovalResult
    {
        public bool IsApproved { get; set; }
        public string Message { get; set; } = "";
    }
}
