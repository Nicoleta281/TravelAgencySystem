using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Models.Users
{
    public abstract class User : ILogin
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public Role Role { get; set; } = new();
        public UserSession Session { get; set; } = new();
        public bool IsBlocked { get; set; }
        public abstract void Login();
        public abstract void Logout();
    }
}