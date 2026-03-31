using FluentValidation;
using FluentValidation.Validators;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Validators
{ 
    public class TripPackageValidator : AbstractValidator<TripPackage>
{
    public TripPackageValidator()
    {
        RuleFor(x => x.Name)
             .NotEmpty().WithMessage("TripPackage trebuie sa aiba Nume");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("TripPackage trebuie sa aiba Pretul mai mare ca 0");

        RuleFor(x => x.Transport)
            .NotNull().WithMessage("TripPackage trebuie sa aiba transport");

        RuleFor(x => x.Stay)
            .NotNull().WithMessage("TripPackage trebuie sa aiba cazare");

        RuleFor(x => x.Days)
            .NotNull().WithMessage("TripPackage trebuie sa aiba lista de zile");

        RuleFor(x => x.Days.Count)
            .GreaterThan(0).WithMessage("TripPackage trebuie sa aiba cel putin o zi");
    }
}
}