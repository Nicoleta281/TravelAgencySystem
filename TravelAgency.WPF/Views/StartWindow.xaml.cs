using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TravelAgency.WPF.Messaging;

namespace TravelAgency.WPF.Views
{
    public partial class StartWindow : Window, INotifyPropertyChanged
    {
        private readonly IMediator _mediator;
        private readonly DispatcherTimer _timer;
        private readonly bool _autoRedirectToLogin = false;
        private int _secondsRemaining = 2;
        private bool _stayHere;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsAutoRedirectEnabled => _autoRedirectToLogin;

        public string CountdownText =>
            _autoRedirectToLogin
                ? (_stayHere ? "Auto-redirect paused." : $"Redirecting to login in {_secondsRemaining}s…")
                : "Click Login to continue.";

        public StartWindow() : this(App.Mediator)
        {
        }

        public StartWindow(IMediator mediator)
        {
            _mediator = mediator;
            InitializeComponent();
            DataContext = this;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Resources["EnterStoryboard"] is Storyboard sb)
            {
                sb.Begin(this);
            }

            if (Resources["HeroMotionStoryboard"] is Storyboard heroSb)
            {
                heroSb.Begin(this, true);
            }

            _secondsRemaining = 2;
            _stayHere = false;
            RaisePropertyChanged(nameof(CountdownText));
            RaisePropertyChanged(nameof(IsAutoRedirectEnabled));

            if (_autoRedirectToLogin)
            {
                _timer.Start();
            }
        }

        private void LeftHero_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not FrameworkElement fe)
            {
                return;
            }

            // WPF ClipToBounds clips rectangular; this forces rounded clipping.
            fe.Clip = new RectangleGeometry(
                new Rect(0, 0, fe.ActualWidth, fe.ActualHeight),
                radiusX: 28,
                radiusY: 28);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_autoRedirectToLogin)
            {
                _timer.Stop();
                return;
            }

            if (_stayHere)
            {
                _timer.Stop();
                return;
            }

            _secondsRemaining--;
            RaisePropertyChanged(nameof(CountdownText));

            if (_secondsRemaining <= 0)
            {
                _timer.Stop();
                LoginToApp();
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            LoginToApp();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            var registerWindow = new RegisterWindow();
            Application.Current.MainWindow = registerWindow;
            registerWindow.Show();
            Close();
        }

        private void StayHere_Click(object sender, RoutedEventArgs e)
        {
            _stayHere = true;
            RaisePropertyChanged(nameof(CountdownText));
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoginToApp()
        {
            _timer.Stop();
            var loginWindow = new LoginWindow(_mediator);
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _timer.Stop();
            base.OnClosing(e);
        }

        private void RaisePropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

