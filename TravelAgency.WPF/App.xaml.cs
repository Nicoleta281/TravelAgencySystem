using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
        private System.Diagnostics.Process? _apiProcess;
        private bool _apiStartedByThisApp;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Catch unhandled exceptions so the UI doesn't silently close.
            // This also makes debugging much easier for runtime XAML/binding/storyboard issues.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Reduce noisy binding warnings in Output window (they are usually harmless and extremely spammy).
            // We still show real exceptions via the handlers above.
            try
            {
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
            }
            catch
            {
                // ignore
            }

#if DEBUG
            // In development we can prevent "port already in use" by spawning/stopping the API together with the WPF app.
            // If the API is already running, we won't start another instance.
            TryStartApiForDev();
#endif

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

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Debug.WriteLine("DispatcherUnhandledException: " + e.Exception);
                MessageBox.Show(
                    e.Exception.ToString(),
                    "Unhandled UI exception",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // ignored
            }
            finally
            {
                // Prevent immediate shutdown so user can continue.
                e.Handled = true;
            }
        }

        private static void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Debug.WriteLine("DomainUnhandledException: " + (e.ExceptionObject?.ToString() ?? "<null>"));
            }
            catch
            {
                // ignored
            }
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Debug.WriteLine("UnobservedTaskException: " + e.Exception);
            }
            catch
            {
                // ignored
            }
            finally
            {
                e.SetObserved();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
#if DEBUG
            if (_apiStartedByThisApp && _apiProcess != null && !_apiProcess.HasExited)
            {
                try
                {
                    _apiProcess.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best-effort shutdown. In the worst case the next run can still detect the port and skip starting.
                }
            }
#endif
            base.OnExit(e);
        }

#if DEBUG
        private void TryStartApiForDev()
        {
            // Keep aligned with WPF image proxy / media calls (http://localhost:5280)
            const int apiPort = 5280;
            if (IsPortListening(apiPort))
                return;

            try
            {
                var apiCsproj = GetRepoRoot() + Path.DirectorySeparatorChar + "TravelAgency.Api" + Path.DirectorySeparatorChar + "TravelAgency.Api.csproj";

                // Uses launchSettings.json profile named "http" => http://localhost:5280
                var startInfo = new System.Diagnostics.ProcessStartInfo(
                    "dotnet",
                    $"run --project \"{apiCsproj}\" --launch-profile http --no-build")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                _apiProcess = System.Diagnostics.Process.Start(startInfo);
                if (_apiProcess == null)
                    return;

                _apiStartedByThisApp = true;

                // Wait until the port is reachable (avoid race condition when user immediately clicks "Forgot password").
                WaitForPortListening(apiPort, TimeSpan.FromSeconds(30));
            }
            catch
            {
                _apiProcess = null;
                _apiStartedByThisApp = false;
            }
        }

        private static bool IsPortListening(int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync("localhost", port);
                return connectTask.Wait(TimeSpan.FromMilliseconds(500)) && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static void WaitForPortListening(int port, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                if (IsPortListening(port))
                    return;

                System.Threading.Thread.Sleep(250);
            }
        }

        private static string GetRepoRoot()
        {
            // base directory: .../TravelAgency.WPF/bin/Debug/net8.0-windows/
            // go up 4 levels to .../TravelAgencySystem/
            var baseDir = AppContext.BaseDirectory;
            var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            return repoRoot;
        }
#endif

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
            services.AddTransient<IUserMessageRepository, EfUserMessageRepository>();
            services.AddTransient<IAdminAnalyticsSnapshotRepository>(_ =>
                new EfAdminAnalyticsSnapshotRepository(TravelAgencyDbContextFactory.Create()));
            services.AddTransient<AuthenticationService>();
            services.AddTransient<RegistrationService>();
            services.AddTransient<BookingAccessService>();
            services.AddTransient<BookingService>();
            services.AddTransient<BookingApprovalChainFactory>();
            services.AddSingleton<PackageSharedInfoFactory>();
            services.AddTransient<TripComponentFactorySelector>();
            // TripDirector depinde de interfața ITripPackageBuilder.
            // Dacă nu o înregistrăm, DI nu poate rezolva constructorul TripDirector și crash-uie la navigare.
            services.AddTransient<ITripPackageBuilder, TripPackageBuilder>();
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