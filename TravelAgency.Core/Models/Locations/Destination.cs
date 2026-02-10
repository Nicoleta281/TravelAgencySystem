using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models.Locations
{
    public class Destination
    {
        public string Name {  get; set; }
        public List<Attraction> Attractions { get; set; } = new();

    }
}
