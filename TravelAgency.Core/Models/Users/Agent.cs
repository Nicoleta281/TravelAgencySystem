using System;
using System.Collections.Generic;
using System;

namespace TravelAgency.Core.Models.Users
{
    public class Agent : User
    {
        public override void Login()
        {
            Session.DateLogin = DateTime.Now;
            Session.IsActive = true;
        }

        public override void Logout()
        {
            Session.IsActive = false;
        }
    }
}
