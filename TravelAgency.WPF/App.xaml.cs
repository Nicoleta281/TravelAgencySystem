using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Windows;
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

            QuestPDF.Settings.License = LicenseType.Community;

            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}