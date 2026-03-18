using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Builders
{
    public interface ITripPackageBuilder
    {
        void Reset();

        void BuildBasicInfo(TripRequest request);
        void BuildDestinationAndDates(TripRequest request);
        void BuildTransportAndAccommodation(TripRequest request);
        void BuildPricing(TripRequest request);

        TripPackage GetResult();
    }
}