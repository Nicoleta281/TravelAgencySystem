using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Prototypes;

namespace TravelAgency.Core.Models.TripPkg.Package
{
    public class TripPackage : IPrototype<TripPackage>
    {
        public TripPackage()
        {
        }

        public string Name { get; set; }
        public double Price { get; set; }
        public Season Season { get; set; }

        public List<TripDay> Days { get; set; } = new();

        public ITransport Transport { get; set; }
        public IStay Stay { get; set; }
        public List<IExtraService> ExtraServices { get; set; } = new();

        public void AddDay(TripDay day)
        {
            if (day == null) throw new ArgumentNullException(nameof(day));
            Days.Add(day);
        }

        public void AddExtraService(IExtraService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            ExtraServices.Add(service);
        }

        // ===== Prototype =====

        public TripPackage ShallowClone()
        {
            return (TripPackage)this.MemberwiseClone();
        }

        public TripPackage DeepClone()
        {
            var copy = (TripPackage)this.MemberwiseClone();

            // Season e clasă -> copiem obiectul (altfel se partajează referința)
            if (this.Season != null)
            {
                copy.Season = new Season
                {
                    Name = this.Season.Name,
                    StartDate = this.Season.StartDate,
                    EndDate = this.Season.EndDate
                };
            }

            // Listă nouă pentru ExtraServices
            copy.ExtraServices = this.ExtraServices?.ToList() ?? new List<IExtraService>();

            // Days: pentru deep clone adevărat avem nevoie de DeepClone și în TripDay.
            // Temporar facem copie de listă (shallow asupra obiectelor TripDay).
            copy.Days = this.Days?.ToList() ?? new List<TripDay>();

            // Transport/Stay: lăsăm referință (shallow) până implementăm Prototype și la ele
            copy.Transport = this.Transport;
            copy.Stay = this.Stay;

            return copy;
        }
    }
}
