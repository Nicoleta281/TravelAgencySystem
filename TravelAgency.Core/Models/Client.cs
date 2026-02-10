using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models
{
    public class Client : User
    { public override void Login() { }
      public override void Logout() { }

      public void ViewTrips() { }
    }
}
