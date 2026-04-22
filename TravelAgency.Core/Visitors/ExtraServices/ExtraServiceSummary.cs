using System.Collections.Generic;

namespace TravelAgency.Core.Visitors.ExtraServices
{
    public sealed class ExtraServiceSummary
    {
        public decimal TotalPrice { get; init; }
        public int LeafCount { get; init; }
        public int MaxDepth { get; init; }
        public IReadOnlyList<string> LeafNames { get; init; } = new List<string>();
    }
}

