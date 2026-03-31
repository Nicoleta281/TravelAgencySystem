using System.Collections.Generic;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Data.Repositories
{
    public interface IUserRepository
    {
        IReadOnlyList<User> GetAll();
        User? GetByUsername(string username);
        User Add(User user);
        void Update(User user);
    }
}