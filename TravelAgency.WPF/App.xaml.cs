using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using System.Net.Http;
using System.Windows;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Patterns.Adapters.GeoDb;
using TravelAgency.Core.Patterns.Adapters.SerpApi;
using TravelAgency.Core.Patterns.Builders;
using TravelAgency.Core.Patterns.Facades;
using TravelAgency.Core.Patterns.Flyweight;
using TravelAgency.Core.Patterns.Observer;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Messaging;
using TravelAgency.WPF.Messaging.Messages;
using TravelAgency.WPF.Services;
using TravelAgency.WPF.Services.Navigation;
using TravelAgency.WPF.Views;
using TravelAgency.WPF.Views.Agent;

namespace TravelAgency.WPF
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public static IMediator Mediator { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Services = ConfigureServices();
            Mediator = Services.GetRequiredService<IMediator>();

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

            var startWindow = ActivatorUtilities.CreateInstance<StartWindow>(Services, Mediator);
            MainWindow = startWindow;
            startWindow.Show();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Messaging / MVVM infrastructure
            services.AddSingleton<IMediator, AppMediator>();
            services.AddSingleton<INavigationService>(sp => new NavigationService(sp));

            // Observer - keep a single in-process instance (no static global required)
            services.AddSingleton<BookingNotificationService>();

            // Repositories & domain services (basic registrations; viewmodels can evolve to consume these via DI)
            services.AddTransient<IUserRepository, EfUserRepository>();
            services.AddTransient<ITripPackageRepository, EfTripPackageRepository>();
            services.AddTransient<IBookingRepository, EfBookingRepository>();
            services.AddTransient<IAdminAnalyticsSnapshotRepository>(_ =>
                new EfAdminAnalyticsSnapshotRepository(TravelAgencyDbContextFactory.Create()));
            services.AddTransient<AuthenticationService>();
            services.AddTransient<RegistrationService>();
            services.AddTransient<BookingAccessService>();
            services.AddTransient<BookingService>();
            services.AddTransient<BookingApprovalChainFactory>();
            services.AddSingleton<PackageSharedInfoFactory>();
            services.AddTransient<TripComponentFactorySelector>();
            services.AddTransient<TripPackageBuilder>();
            services.AddTransient<TripDirector>();
            services.AddTransient<TripCreationService>();

            // Password reset flow used by WPF (calls API). Kept as factory to preserve current behavior.
            services.AddTransient<IPasswordResetFlow>(_ => PasswordResetFlowFactory.Create());

            // HTTP clients + Adapters (Adapter pattern, production-style composition)
            services.AddHttpClient("GeoDb");
            services.AddHttpClient("SerpApi");

            services.AddTransient<GeoDbOptions>(_ => BuildGeoDbOptions());
            services.AddTransient<SerpApiOptions>(_ => BuildSerpApiOptions());

            services.AddTransient<ILocationSearchProvider>(sp =>
                new GeoDbLocationAdapter(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("GeoDb"),
                    sp.GetRequiredService<GeoDbOptions>()));

            services.AddTransient<IHotelSearchProvider>(sp =>
                new SerpApiHotelAdapter(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SerpApi"),
                    sp.GetRequiredService<SerpApiOptions>()));

            services.AddTransient<TravelPackageFacade>();

            return services.BuildServiceProvider();
        }

        private static GeoDbOptions BuildGeoDbOptions()
        {
            string key = (Environment.GetEnvironmentVariable("RAPIDAPI_KEY")
                          ?? Environment.GetEnvironmentVariable("RAPIDAPI_KEY", EnvironmentVariableTarget.User)
                          ?? Environment.GetEnvironmentVariable("RAPIDAPI_KEY", EnvironmentVariableTarget.Machine)
                          ?? "").Trim();

            return new GeoDbOptions
            {
                ApiKey = key
            };
        }

        private static SerpApiOptions BuildSerpApiOptions()
        {
            string key = (Environment.GetEnvironmentVariable("SERPAPI_API_KEY")
                          ?? Environment.GetEnvironmentVariable("SERPAPI_API_KEY", EnvironmentVariableTarget.User)
                          ?? Environment.GetEnvironmentVariable("SERPAPI_API_KEY", EnvironmentVariableTarget.Machine)
                          ?? "").Trim();

            return new SerpApiOptions
            {
                ApiKey = key
            };
        }

        private static void OnUserLoggedIn(UserLoggedInMessage msg)
        {
            if (msg.User is Admin)
            {
                var adminWindow = ActivatorUtilities.CreateInstance<AdminWindow>(Services);
                adminWindow.Show();
                msg.SourceWindow.Close();
                return;
            }

            if (msg.User is Agent)
            {
                var agentWindow = ActivatorUtilities.CreateInstance<AgentWindow>(Services);
                agentWindow.Show();
                msg.SourceWindow.Close();
                return;
            }

            if (msg.User is Client)
            {
                var clientWindow = ActivatorUtilities.CreateInstance<ClientWindow>(Services);
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
            var startWindow = ActivatorUtilities.CreateInstance<StartWindow>(Services, Mediator);
            Current.MainWindow = startWindow;
            startWindow.Show();
            msg.SourceWindow.Close();
        }
    }
}