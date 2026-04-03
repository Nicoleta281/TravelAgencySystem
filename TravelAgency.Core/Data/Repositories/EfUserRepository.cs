using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data.Mappers;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Data.Repositories
{
    public class EfUserRepository : IUserRepository
    {
        public User? GetById(int id)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = db.Users
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id);

            return entity == null ? null : UserMapper.FromEntity(entity);
        }
        public IReadOnlyList<User> GetAll()
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entities = db.Users.AsNoTracking().ToList();
            return entities.Select(UserMapper.FromEntity).ToList();
        }

        public User? GetByUsername(string username)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = db.Users
                .AsNoTracking()
                .FirstOrDefault(x => x.Username == username);

            return entity == null ? null : UserMapper.FromEntity(entity);
        }

        public User Add(User user)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = UserMapper.ToEntity(user);

            db.Users.Add(entity);
            db.SaveChanges();

            user.Id = entity.Id;
            return user;
        }

        public void Update(User user)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = db.Users.FirstOrDefault(x => x.Id == user.Id);

            if (entity == null)
                return;

            var mapped = UserMapper.ToEntity(user);

            entity.Username = mapped.Username;
            entity.PasswordHash = mapped.PasswordHash;
            entity.RoleName = mapped.RoleName;
            entity.IsActive = mapped.IsActive;
            entity.IsBlocked = mapped.IsBlocked;

            db.SaveChanges();
        }
    }
}