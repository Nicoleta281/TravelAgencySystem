using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.WPF.Commands.Admin
{
    public interface IAdminCommand
    {
        bool CanExecute();
        void Execute();
    }
}
