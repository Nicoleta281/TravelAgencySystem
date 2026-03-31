using FluentValidation;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Validators
{
    public class BookingValidator : AbstractValidator<Booking>
    {
        public BookingValidator()
        {
            RuleFor(x => x.TripPackage)
                .NotNull().WithMessage("Booking must have an associated trip package.");

            RuleFor(x => x.BasePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Base price must be greater than or equal to 0.");

            RuleFor(x => x.TotalPrice)
                .GreaterThan(0).WithMessage("Total price must be greater than 0.");

            RuleFor(x => x.Status)
                .NotNull().WithMessage("Booking status is required.");
        }
    }
}

