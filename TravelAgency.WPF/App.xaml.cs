using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Windows;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Messaging;
using TravelAgency.WPF.Messaging.Messages;
using TravelAgency.WPF.Views;
using TravelAgency.WPF.Views.Agent;

namespace TravelAgency.WPF
{
    public partial class App : Application
    {
        public static IMediator Mediator { get; private set; } = new AppMediator();

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

            Mediator.Subscribe<UserLoggedInMessage>(OnUserLoggedIn);
            Mediator.Subscribe<LogoutRequestedMessage>(OnLogoutRequested);

            var startWindow = new StartWindow(Mediator);
            MainWindow = startWindow;
            startWindow.Show();
        }

        private static void OnUserLoggedIn(UserLoggedInMessage msg)
        {
            if (msg.User is Admin)
            {
                var adminWindow = new AdminWindow();
                adminWindow.Show();
                msg.SourceWindow.Close();
                return;
            }

            if (msg.User is Agent)
            {
                var agentWindow = new AgentWindow();
                agentWindow.Show();
                msg.SourceWindow.Close();
                return;
            }

            if (msg.User is Client)
            {
                var clientWindow = new ClientWindow();
                clientWindow.Show();
                msg.SourceWindow.Close();
                return;
            }

            MessageBox.Show("Unknown user type assigned to this user.",
                            "Login",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
        }

        private static void OnLogoutRequested(LogoutRequestedMessage msg)
        {
            var startWindow = new StartWindow(Mediator);
            Current.MainWindow = startWindow;
            startWindow.Show();
            msg.SourceWindow.Close();
        }
    }
}