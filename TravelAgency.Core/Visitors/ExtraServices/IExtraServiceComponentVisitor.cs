using TravelAgency.Core.Patterns.Composite;

namespace TravelAgency.Core.Visitors.ExtraServices
{
    public interface IExtraServiceComponentVisitor<T>
    {
        T VisitLeaf(ExtraServiceLeaf leaf);
        T VisitGroup(ExtraServiceGroup group);
    }
}

