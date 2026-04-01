using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Patterns.Decorator;

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