using FluentValidation;
using TravelAgency.Core.Models;

namespace TravelAgency.Core.Validators
{
    public class TripRequestValidator : AbstractValidator<TripRequest>
    {
        public TripRequestValidator()
        {
            RuleFor(x => x.PackageName)
                .NotEmpty().WithMessage("Package Name is required.");

            RuleFor(x => x.TripType)
                .NotEmpty().WithMessage("Trip Type is required.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(x => x.Destination)
                .NotEmpty().WithMessage("Destination is required.");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required.");

            RuleFor(x => x.StartDate)
                .NotNull().WithMessage("Start Date is required.");

            RuleFor(x => x.EndDate)
                .NotNull().WithMessage("End Date is required.")
                .GreaterThan(x => x.StartDate)
                .WithMessage("End Date must be after Start Date.");

            RuleFor(x => x.AvailableSeats)
                .GreaterThan(0).WithMessage("Available Seats must be greater than 0.");

            RuleFor(x => x.BasePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Base Price must be greater than or equal to 0.");

            RuleFor(x => x.FinalPrice)
                .GreaterThan(0).WithMessage("Final Price must be greater than 0.");
        }
    }
}

