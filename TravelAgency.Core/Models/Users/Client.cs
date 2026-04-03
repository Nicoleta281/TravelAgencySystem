using System;

namespace TravelAgency.Core.Models.Users
{
    public class Client : User
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