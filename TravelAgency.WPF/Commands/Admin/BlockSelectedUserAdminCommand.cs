using TravelAgency.WPF.ViewModels.Admin;
using TravelAgency.WPF.ViewModels.AdminVM;

namespace TravelAgency.WPF.Commands.Admin
{
    public class BlockSelectedUserAdminCommand : IAdminCommand
    {
        private readonly AdminUserRowViewModel _selectedUser;

        public BlockSelectedUserAdminCommand(AdminUserRowViewModel selectedUser)
        {
            _selectedUser = selectedUser;
        }

        public bool CanExecute()
        {
            return _selectedUser != null
                   && _selectedUser.Role != "Admin"
                   && _selectedUser.Status != "Blocked";
        }

        public void Execute()
        {
            if (!CanExecute())
                return;

            _selectedUser.Status = "Blocked";
        }
    }
}