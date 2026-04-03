using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.WPF.ViewModels.AdminVM
{
    public class ModerationPackageViewModel : ViewModelBase
    {
        private int _id;
        private string _name = "";
        private string _destination = "";
        private string _periodText = "";
        private string _createdByAgent = "";
        private string _status = "Pending";

        public int Id
        {
            get => _id;
            set => Set(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string Destination
        {
            get => _destination;
            set => Set(ref _destination, value);
        }

        public string PeriodText
        {
            get => _periodText;
            set => Set(ref _periodText, value);
        }

        public string CreatedByAgent
        {
            get => _createdByAgent;
            set => Set(ref _createdByAgent, value);
        }

        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }
    }
}
