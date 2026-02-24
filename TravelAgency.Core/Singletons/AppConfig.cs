using System;

namespace TravelAgency.Core.Singletons
{
    public sealed class AppConfig
    {
        private static readonly Lazy<AppConfig> _instance =
            new Lazy<AppConfig>(() => new AppConfig());

        public static AppConfig Instance => _instance.Value;

        private AppConfig()
        {
            Currency = "EUR";
            VatRate = 0.20m;        // 20%
            DefaultDiscount = 0.05m; // 5%
        }

        public string Currency { get; set; }
        public decimal VatRate { get; set; }          // ex: 0.20 = 20%
        public decimal DefaultDiscount { get; set; }  // ex: 0.05 = 5%

        public decimal ApplyDiscount(decimal basePrice)
        {
            if (basePrice < 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
            return basePrice * (1 - DefaultDiscount);
        }

        public decimal ApplyVat(decimal basePrice)
        {
            if (basePrice < 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
            return basePrice * (1 + VatRate);
        }

        public decimal FinalPrice(decimal basePrice)
        {
            return ApplyVat(ApplyDiscount(basePrice));
        }
    }
}