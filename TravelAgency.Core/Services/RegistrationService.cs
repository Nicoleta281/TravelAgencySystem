using System;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Services
{
    public class RegistrationService
    {
        private readonly IUserRepository _userRepository;

        public RegistrationService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public bool UsernameExists(string username)
        {
            return _userRepository.GetByUsername(username) != null;
        }

        public User RegisterClient(RegisterRequest request)
        {
            if (UsernameExists(request.Username))
                throw new InvalidOperationException("Username already exists.");

            var client = new Client
            {
                Username = request.Username.Trim(),
                PasswordHash = PasswordHasher.Hash(request.Password),
                Role = new Role { Name = "Client" },
                Session = new UserSession { IsActive = false }
            };

            return _userRepository.Add(client);
        }
    }
}