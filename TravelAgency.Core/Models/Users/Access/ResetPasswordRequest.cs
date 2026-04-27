namespace TravelAgency.Core.Models.Users.Access
{
    public class ResetPasswordRequest
    {
        public string Username { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}

