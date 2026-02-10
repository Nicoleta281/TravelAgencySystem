using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models.TripPackage
{
    public class TripDay
    {
        public List<Activity> Activities { get; set; } = new();
        
        public void AddActivity (Activity activity) { }


    }
}
