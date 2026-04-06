using System;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Patterns.Builders
{
    public class TripDirector
    {
        private ITripPackageBuilder _builder;

        public TripDirector(ITripPackageBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void ChangeBuilder(ITripPackageBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public TripPackage Build(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _builder.Reset();
            _builder.BuildBasicInfo(request);
            _builder.BuildDestinationAndDates(request);
            _builder.BuildTransportAndAccommodation(request);
            _builder.BuildPricing(request);

            return _builder.GetResult();
        }
    }
}