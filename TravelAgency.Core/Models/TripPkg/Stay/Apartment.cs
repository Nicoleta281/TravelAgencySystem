using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Models.TripPkg.Stay
{
    public class Apartment : IStay
    {
        public string Name { get; set; }
        public string Address { get; set; }

        public void CheckIn() { }

    }
}
