using System;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Models.Users.Access
{
    public class UserSession
    {
        public DateTime DateLogin { get; set; }
        public bool IsActive { get; set; }
        public User? CurrentUser { get; set; }
        public ISessionState State { get; set; }

        public UserSession()
        {
            State = new LoggedOutState();
        }

        public void StartSession(User user)
        {
            State.Login(this, user);
        }

        public void EndSession()
        {
            State.Logout(this);
        }
    }
}