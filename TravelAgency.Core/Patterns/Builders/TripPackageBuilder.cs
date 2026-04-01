using System;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Patterns.Flyweight;

namespace TravelAgency.Core.Patterns.Builders
{
    public class TripPackageBuilder : ITripPackageBuilder
    {
        private TripPackage _tripPackage;
        private readonly PackageSharedInfoFactory _sharedInfoFactory;

        public TripPackageBuilder()
        {
            _sharedInfoFactory = PackageSharedInfoFactorySingleton.Instance;
            Reset();
        }

        public void Reset()
        {
            _tripPackage = new TripPackage();
        }

        public void BuildBasicInfo(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _tripPackage.Name = request.PackageName ?? "";
            _tripPackage.TripType = request.TripType ?? "";
            _tripPackage.Category = request.Category ?? "";
            _tripPackage.ShortDescription = request.ShortDescription ?? "";
        }

        public void BuildDestinationAndDates(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                _tripPackage.Season = new Season
                {
                    Name = $"{request.Destination} trip",
                    StartDate = request.StartDate.Value,
                    EndDate = request.EndDate.Value
                };
            }

            _tripPackage.SharedInfo = _sharedInfoFactory.GetOrCreate(
                request.Destination ?? "",
                request.Country ?? "",
                request.DepartureCity ?? "",
                request.AccommodationName ?? "",
                request.MealPlan ?? "",
                request.TransportType ?? "",
                request.AccommodationType ?? "");
        }

        public void BuildTransportAndAccommodation(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _tripPackage.TransportDisplayName = request.TransportType ?? "";
            _tripPackage.StayDisplayName = request.AccommodationType ?? "";
            _tripPackage.AvailableSeats = request.AvailableSeats;
        }

        public void BuildPricing(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _tripPackage.BasePrice = request.BasePrice;
            _tripPackage.DiscountPercent = request.DiscountPercent;
            _tripPackage.VatPercent = request.VatPercent;
            _tripPackage.ExtraCharges = request.ExtraCharges;

            if (request.FinalPrice > 0)
            {
                _tripPackage.Price = request.FinalPrice;
            }
            else
            {
                double afterDiscount = request.BasePrice - request.BasePrice * request.DiscountPercent / 100.0;
                double afterVat = afterDiscount + afterDiscount * request.VatPercent / 100.0;
                _tripPackage.Price = afterVat + request.ExtraCharges;
            }
        }

        public TripPackage GetResult()
        {
            return _tripPackage;
        }
    }
}