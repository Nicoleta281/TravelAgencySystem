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
                IsActive = user.Session?.IsActive ?? false,
                IsBlocked = user.IsBlocked
            };
        }

        public static User FromEntity(UserEntity entity)
        {
            var roleName = entity.RoleName?.Trim().ToLower();

            User user = roleName switch
            {
                "admin" => new Admin(),
                "agent" => new Agent(),
                "client" => new Client(),
                _ => new Client()
            };

            user.Id = entity.Id;
            user.Username = entity.Username;
            user.PasswordHash = entity.PasswordHash;
            user.Role = new Role { Name = entity.RoleName };
            user.IsBlocked = entity.IsBlocked;
            user.Session = new UserSession
            {
                IsActive = entity.IsActive
            };

            return user;
        }
    }
}