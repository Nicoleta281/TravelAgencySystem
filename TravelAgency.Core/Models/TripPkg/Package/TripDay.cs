using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Prototypes;
using System.Linq;

namespace TravelAgency.Core.Models.TripPkg.Package
{
    public class TripDay : IPrototype<TripDay>
    {
        public List<Activity> Activities { get; set; } = new();

        public void AddActivity(Activity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            Activities.Add(activity);
        }

        public TripDay ShallowClone()
        {
            return (TripDay)this.MemberwiseClone();
        }

        public TripDay DeepClone()
        {
            var copy = (TripDay)this.MemberwiseClone();

            copy.Activities = this.Activities?
                .Select(a => a.DeepClone())
                .ToList() ?? new List<Activity>();

            return copy;
        }

    }
}


