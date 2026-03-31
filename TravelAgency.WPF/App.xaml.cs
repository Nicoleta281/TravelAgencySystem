using System.Windows;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Views;

namespace TravelAgency.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var db = TravelAgencyDbContextFactory.Create())
            {
                db.Database.Migrate();
            }

            var userRepository = new EfUserRepository();
            var seedService = new UserSeedService(userRepository);
            seedService.SeedDefaultUsers();

            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}
