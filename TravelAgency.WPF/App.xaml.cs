using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Data;
using System.Windows;
using TravelAgency.Core.Data;

namespace TravelAgency.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = TravelAgencyDbContextFactory.Create();
            db.Database.Migrate();
        }
    }

}
