using TravelAgency.Core.Models.Users;

namespace TravelAgency.WPF.Commands.Admin
{
    public class BlockUserCommand : IAdminCommand
    {
        private readonly User _user;

        public BlockUserCommand(User user)
        {
            _user = user;
        }

        public bool CanExecute()
        {
            return _user != null
                   && _user.Role?.Name != "Admin"
                   && _user.Session != null
                   && _user.Session.IsActive;
        }

        public void Execute()
        {
            if (!CanExecute())
                return;

            _user.Session.IsActive = false;
        }
    }
}