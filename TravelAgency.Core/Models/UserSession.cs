using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models
{
    public class UserSession
    {
        public DateTime DateLogin { get; set; }
        public bool IsActive { get; set; }

        public void StartSession() { }
        public void EndSession() { }
    }
}
