using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.WPF.Messaging;
using TravelAgency.WPF.Views;
using TravelAgency.WPF.Views.Agent;

namespace TravelAgency.WPF.Services.Navigation
{
    public interface INavigationService
    {
        void ShowRegisterAndClose(Window currentWindow);

        void ShowForgotPasswordDialog(Window owner);

        void ShowLoginAndClose(Window currentWindow);

        CreatePackageWindow CreateNewPackageWindow();

        QuickCreatePackageWindow CreateQuickCreatePackageWindow();

        CreatePackageWindow CreateEditPackageWindow(TripPackage trip);
    }

    public sealed class NavigationService : INavigationService
    {
        private readonly IServiceProvider _services;

        public NavigationService(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public void ShowRegisterAndClose(Window currentWindow)
        {
            if (currentWindow == null) throw new ArgumentNullException(nameof(currentWindow));

            var registerWindow = ActivatorUtilities.CreateInstance<RegisterWindow>(_services);
            registerWindow.Show();
            currentWindow.Close();
        }

        public void ShowForgotPasswordDialog(Window owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            var forgotWindow = ActivatorUtilities.CreateInstance<ForgotPasswordWindow>(_services);
            forgotWindow.Owner = owner;
            forgotWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            forgotWindow.ShowDialog();
        }

        public void ShowLoginAndClose(Window currentWindow)
        {
            if (currentWindow == null) throw new ArgumentNullException(nameof(currentWindow));

            var mediator = _services.GetRequiredService<IMediator>();
            var loginWindow = ActivatorUtilities.CreateInstance<LoginWindow>(_services, mediator);
            loginWindow.Show();
            currentWindow.Close();
        }

        public CreatePackageWindow CreateNewPackageWindow()
            => ActivatorUtilities.CreateInstance<CreatePackageWindow>(_services);

        public QuickCreatePackageWindow CreateQuickCreatePackageWindow()
            => ActivatorUtilities.CreateInstance<QuickCreatePackageWindow>(_services);

        public CreatePackageWindow CreateEditPackageWindow(TripPackage trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));
            return ActivatorUtilities.CreateInstance<CreatePackageWindow>(_services, trip);
        }
    }
}
