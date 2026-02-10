using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Models
{
   public abstract class User : ILogin
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public Role Role { get; set; }
        public UserSession Session { get; set; }

        public abstract void Login();
        public abstract void Logout();
    }
}
