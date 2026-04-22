using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Patterns.Composite;

namespace TravelAgency.Core.Visitors.ExtraServices
{
    public sealed class ExtraServiceSummaryVisitor : IExtraServiceComponentVisitor<ExtraServiceSummary>
    {
        public ExtraServiceSummary VisitLeaf(ExtraServiceLeaf leaf)
        {
            if (leaf == null) throw new ArgumentNullException(nameof(leaf));

            return new ExtraServiceSummary
            {
                TotalPrice = leaf.GetPrice(),
                LeafCount = 1,
                MaxDepth = 1,
                LeafNames = new[] { leaf.Name }
            };
        }

        public ExtraServiceSummary VisitGroup(ExtraServiceGroup group)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));

            if (group.Children.Count == 0)
            {
                return new ExtraServiceSummary
                {
                    TotalPrice = group.GetPrice(),
                    LeafCount = 0,
                    MaxDepth = 1,
                    LeafNames = Array.Empty<string>()
                };
            }

            var childrenSummaries = group.Children
                .Select(child => child.Accept(this))
                .ToList();

            var leafNames = childrenSummaries
                .SelectMany(s => s.LeafNames)
                .ToList();

            return new ExtraServiceSummary
            {
                TotalPrice = group.GetPrice(),
                LeafCount = childrenSummaries.Sum(s => s.LeafCount),
                MaxDepth = 1 + childrenSummaries.Max(s => s.MaxDepth),
                LeafNames = leafNames
            };
        }
    }
}

