using TravelAgency.WPF.ViewModels;

namespace TravelAgency.WPF.ViewModels.Admin
{
    public class AdminUserRowViewModel : ViewModelBase
    {
        private int _id;
        private string _username = "";
        private string _email = "";
        private string _role = "";
        private string _status = "";

        public int Id
        {
            get => _id;
            set => Set(ref _id, value);
        }

        public string Username
        {
            get => _username;
            set => Set(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => Set(ref _email, value);
        }

        public string Role
        {
            get => _role;
            set => Set(ref _role, value);
        }

        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }
    }
}