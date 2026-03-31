using TravelAgency.Core.Decorator;
using TravelAgency.Core.Models.TripPkg.Package;

public class BaseTrip : ITripComponent
{
    private readonly TripPackage _trip;

    public BaseTrip(TripPackage trip)
    {
        _trip = trip;
    }

    public string GetDescription()
    {
        return _trip.Name;
    }

    public double GetPrice()
    {
        return _trip.Price;
    }
}