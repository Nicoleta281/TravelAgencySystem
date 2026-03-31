using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Data.Mappers
{
    public static class UserMapper
    {
        public static UserEntity ToEntity(User user)
        {
            return new UserEntity
            {
                Id = user.Id,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                RoleName = user.Role?.Name ?? "",
                IsActive = user.Session?.IsActive ?? false
            };
        }

        public static User FromEntity(UserEntity entity)
        {
            User user = entity.RoleName == "Agent"
                ? new Agent()
                : new Client();

            user.Id = entity.Id;
            user.Username = entity.Username;
            user.PasswordHash = entity.PasswordHash;
            user.Role = new Role { Name = entity.RoleName };
            user.Session = new UserSession
            {
                IsActive = entity.IsActive
            };

            return user;
        }
    }
}
