using System;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Services
{
    public class PasswordResetService
    {
        private readonly IUserRepository _userRepository;

        public PasswordResetService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public void ResetPassword(ResetPasswordRequest request)
        {
            var user = _userRepository.GetByUsername(request.Username.Trim());
            if (user == null)
                throw new InvalidOperationException("User not found.");

            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            _userRepository.Update(user);
        }
    }
}

