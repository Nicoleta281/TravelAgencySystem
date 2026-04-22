using System;

namespace TravelAgency.Core.Patterns.Composite
{
    public interface IExtraServiceComponent
    {
        string Name { get; }
        decimal GetPrice();
        string GetDetails();
        int GetServiceCount();
        bool ContainsService(string serviceName);

        T Accept<T>(TravelAgency.Core.Visitors.ExtraServices.IExtraServiceComponentVisitor<T> visitor);
    }
}