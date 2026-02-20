using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Builders
{
    public interface ITripPackageBuilder
    {
        void Reset();

        void SetName(string name);
        void SetPrice(double price);
        void SetSeason(Season season);
        void SetTransport(ITransport transport);
        void SetStay(IStay stay);

        void AddDay(TripDay day);
        void AddExtraService(IExtraService service);

        TripPackage Build();
    }
}
