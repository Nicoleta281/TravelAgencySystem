using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models.Locations
{
    public class Country
    {
        public string Name { get; set; }
        public List<City> Cities { get; set; } = new();

    }
}
