using System;

namespace TravelAgency.Core.Patterns.Flyweight
{
    public sealed class PackageSharedInfoFactorySingleton
    {
        private static readonly Lazy<PackageSharedInfoFactory> _instance =
            new(() => new PackageSharedInfoFactory());

        public static PackageSharedInfoFactory Instance => _instance.Value;

        private PackageSharedInfoFactorySingleton()
        {
        }
    }
}