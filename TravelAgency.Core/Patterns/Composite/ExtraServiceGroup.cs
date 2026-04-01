using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TravelAgency.Core.Patterns.Composite
{
    public class ExtraServiceGroup : IExtraServiceComponent
    {
        private readonly List<IExtraServiceComponent> _services = new();

        public string Name { get; }
        public decimal DiscountPercent { get; private set; }

        public ExtraServiceGroup(string name, decimal discountPercent = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Group name cannot be empty.", nameof(name));

            if (discountPercent < 0 || discountPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount must be between 0 and 100.");

            Name = name;
            DiscountPercent = discountPercent;
        }

        public IReadOnlyCollection<IExtraServiceComponent> Children => _services.AsReadOnly();

        public void Add(IExtraServiceComponent service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (ReferenceEquals(service, this))
                throw new InvalidOperationException("A group cannot contain itself.");

            if (ContainsDirectChildByName(service.Name))
                return;

            _services.Add(service);
        }

        public void Remove(IExtraServiceComponent service)
        {
            if (service == null)
                return;

            _services.Remove(service);
        }

        public void SetDiscount(decimal discountPercent)
        {
            if (discountPercent < 0 || discountPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount must be between 0 and 100.");

            DiscountPercent = discountPercent;
        }

        public decimal GetPrice()
        {
            decimal total = _services.Sum(s => s.GetPrice());

            if (DiscountPercent > 0)
                total -= total * DiscountPercent / 100m;

            return total;
        }

        public int GetServiceCount()
        {
            return _services.Sum(s => s.GetServiceCount());
        }

        public bool ContainsService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return false;

            if (Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                return true;

            return _services.Any(s => s.ContainsService(serviceName));
        }

        public string GetDetails()
        {
            string discountText = DiscountPercent > 0
                ? $" with {DiscountPercent:0.##}% discount"
                : string.Empty;

            return $"{Name} - {GetPrice():0.##} EUR ({GetServiceCount()} services){discountText}";
        }

        public string PrintStructure(int level = 0)
        {
            var sb = new StringBuilder();
            string indent = new string(' ', level * 2);

            sb.AppendLine($"{indent}- {Name} ({GetPrice():0.##} EUR)");

            foreach (var service in _services)
            {
                if (service is ExtraServiceGroup group)
                {
                    sb.Append(group.PrintStructure(level + 1));
                }
                else
                {
                    string childIndent = new string(' ', (level + 1) * 2);
                    sb.AppendLine($"{childIndent}- {service.GetDetails()}");
                }
            }

            return sb.ToString();
        }

        private bool ContainsDirectChildByName(string name)
        {
            return _services.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}