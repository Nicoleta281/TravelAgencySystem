using FluentValidation;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email is invalid.")
                .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must contain at least 6 characters.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");
        }
    }
}