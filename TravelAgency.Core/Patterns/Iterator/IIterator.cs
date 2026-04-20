namespace TravelAgency.Core.Patterns.Iterator
{
    public interface IIterator<T>
    {
        bool HasNext();
        T Next();
    }
}