using System;

namespace TravelAgency.Core.Models.Users
{
    public class Admin : User
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

        public void ManageUsers() { }
    }
}