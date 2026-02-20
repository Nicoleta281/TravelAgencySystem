using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Builders
{
    public class TripDirector
    {
        public TripPackage BuildCityBreak(
            ITripPackageBuilder builder,
            ITransport transport,
            IStay stay,
            Season season)
        {
            builder.Reset();

            builder.SetName("City Break");
            builder.SetPrice(200);
            builder.SetSeason(season);

            builder.SetTransport(transport);
            builder.SetStay(stay);

            builder.AddDay(new TripDay());
            builder.AddDay(new TripDay());

            return builder.Build();
        }
    }
}

