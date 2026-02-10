using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models.Locations
{
    public class City
    {
        public string Name { get; set; }
        public List<Destination> Destinations { get; set; } = new();

    }
}
