using System;

namespace TravelAgency.Core.Patterns.Composite
{
    public class ExtraServiceLeaf : IExtraServiceComponent
    {
        public string Name { get; }
        public decimal Price { get; }

        public ExtraServiceLeaf(string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Service name cannot be empty.", nameof(name));

            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

            Name = name;
            Price = price;
        }

        public decimal GetPrice()
        {
            return Price;
        }

        public string GetDetails()
        {
            return $"{Name} - {Price:0.##} EUR";
        }

        public int GetServiceCount()
        {
            return 1;
        }

        public bool ContainsService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return false;

            return Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase);
        }
    }
}