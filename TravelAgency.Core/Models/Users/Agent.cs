using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models.Users
{
    public class Agent : User
    {
        public override void Login() { }
        public override void Logout() { }

        public void CreateTripPackage() { }

    }
}
