using TravelAgency.WPF.ViewModels.Admin;
using TravelAgency.WPF.ViewModels.AdminVM;

namespace TravelAgency.WPF.Commands.Admin
{
    public class ChangeUserRoleAdminCommand : IAdminCommand
    {
        private readonly AdminUserRowViewModel _selectedUser;

        public ChangeUserRoleAdminCommand(AdminUserRowViewModel selectedUser)
        {
            _selectedUser = selectedUser;
        }

        public bool CanExecute()
        {
            return _selectedUser != null;
        }

        public void Execute()
        {
            if (!CanExecute())
                return;

            if (_selectedUser.Role == "Client")
                _selectedUser.Role = "Agent";
            else if (_selectedUser.Role == "Agent")
                _selectedUser.Role = "Client";
        }
    }
}