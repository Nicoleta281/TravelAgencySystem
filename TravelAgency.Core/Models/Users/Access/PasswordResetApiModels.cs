namespace TravelAgency.Core.Models.Users.Access
{
    public sealed class PasswordResetRequestApiModel
    {
        public string EmailOrUsername { get; set; } = "";
    }

    public sealed class PasswordResetRequestApiResponse
    {
        public bool CodeSent { get; set; }
    }

    public sealed class PasswordResetConfirmApiModel
    {
        public string EmailOrUsername { get; set; } = "";
        public string OtpCode { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    public sealed class PasswordResetErrorApiResponse
    {
        public string Error { get; set; } = "";
    }
}
