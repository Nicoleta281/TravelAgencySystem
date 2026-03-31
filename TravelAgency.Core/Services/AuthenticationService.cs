using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Services
{
    public class AuthenticationService
    {
        private readonly IUserRepository _userRepository;

        public AuthenticationService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User? Authenticate(string username, string password)
        {
            var user = _userRepository.GetByUsername(username);

            if (user == null)
                return null;

            if (!PasswordHasher.Verify(password, user.PasswordHash))
                return null;

            return user;
        }
    }
}