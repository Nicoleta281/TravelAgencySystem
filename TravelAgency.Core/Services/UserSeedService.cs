using System.Linq;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Services
{
    public class UserSeedService
    {
        private readonly IUserRepository _userRepository;

        public UserSeedService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public void SeedDefaultUsers()
        {
            var users = _userRepository.GetAll();

            if (!users.Any(x => x.Username == "agent1"))
            {
                var agent = new Agent
                {
                    Username = "agent1",
                    PasswordHash = PasswordHasher.Hash("Agent123!"),
                    Role = new Role { Name = "Agent" },
                    Session = new UserSession { IsActive = false }
                };

                _userRepository.Add(agent);
            }

            if (!users.Any(x => x.Username == "client1"))
            {
                var client = new Client
                {
                    Username = "client1",
                    PasswordHash = PasswordHasher.Hash("Client123!"),
                    Role = new Role { Name = "Client" },
                    Session = new UserSession { IsActive = false }
                };

                _userRepository.Add(client);
            }
        }
    }
}