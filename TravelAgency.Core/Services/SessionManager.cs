using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Services
{
    public sealed class SessionManager
    {
        private static readonly SessionManager _instance = new SessionManager();
        public static SessionManager Instance => _instance;

        public UserSession CurrentSession { get; }

        private SessionManager()
        {
            CurrentSession = new UserSession();
        }
    }
}