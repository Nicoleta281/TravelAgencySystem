using System.Collections.ObjectModel;
using TravelAgency.WPF.ViewModels.AdminVM;

namespace TravelAgency.WPF.Commands.Admin
{
    public class ApprovePackageAdminCommand : IAdminCommand
    {
        private readonly ModerationPackageViewModel _selectedPackage;
        private readonly ObservableCollection<ModerationPackageViewModel> _packages;

        public ApprovePackageAdminCommand(
            ModerationPackageViewModel selectedPackage,
            ObservableCollection<ModerationPackageViewModel> packages)
        {
            _selectedPackage = selectedPackage;
            _packages = packages;
        }

        public bool CanExecute()
        {
            return _selectedPackage != null;
        }

        public void Execute()
        {
            if (!CanExecute())
                return;

            _selectedPackage.Status = "Approved";
            _packages.Remove(_selectedPackage);
        }
    }
}