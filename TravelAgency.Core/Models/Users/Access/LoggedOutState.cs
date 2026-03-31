using System;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Models.Users.Access
{
    public class LoggedOutState : ISessionState
    {
        public void Login(UserSession session, User user)
        {
            session.CurrentUser = user;
            session.DateLogin = DateTime.UtcNow;
            session.IsActive = true;
            session.State = new LoggedInState();
        }

        public void Logout(UserSession session)
        {
        }
    }
}