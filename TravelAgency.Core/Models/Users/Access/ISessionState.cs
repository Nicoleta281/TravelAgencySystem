using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Models.Users.Access
{
    public interface ISessionState
    {
        void Login(UserSession session, User user);
        void Logout(UserSession session);
    }
}