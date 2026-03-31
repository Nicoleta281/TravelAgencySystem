using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Models.Users.Access
{
    public class LoggedInState : ISessionState
    {
        public void Login(UserSession session, User user)
        {
        }

        public void Logout(UserSession session)
        {
            session.CurrentUser = null;
            session.IsActive = false;
            session.State = new LoggedOutState();
        }
    }
}