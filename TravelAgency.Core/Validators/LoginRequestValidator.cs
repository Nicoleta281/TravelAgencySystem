using FluentValidation;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must contain at least 3 characters.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must contain at least 6 characters.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");
        }
    }
}