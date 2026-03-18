using System;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Builders
{
    public class TripPackageBuilder : ITripPackageBuilder
    {
        private TripPackage _tripPackage;

        public TripPackageBuilder()
        {
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

            _tripPackage.Name = request.PackageName;
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

            // Daca mai tarziu vrei, aici putem genera si Days
            // momentan il lasam simplu
        }

        public void BuildTransportAndAccommodation(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Aici vom lega mai tarziu Factory Method / Abstract Factory
            // deocamdata lasam Transport si Stay necompletate
            // ca sa pastram Builder-ul functional fara sa incurcam celelalte patternuri
        }

        public void BuildPricing(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.FinalPrice > 0)
            {
                _tripPackage.Price = request.FinalPrice;
            }
            else
            {
                double afterDiscount = request.BasePrice - (request.BasePrice * request.DiscountPercent / 100.0);
                double afterVat = afterDiscount + (afterDiscount * request.VatPercent / 100.0);
                _tripPackage.Price = afterVat + request.ExtraCharges;
            }
        }

        public TripPackage GetResult()
        {
            return _tripPackage;
        }
    }
}