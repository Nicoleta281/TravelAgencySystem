using System.Windows;

namespace TravelAgency.WPF.Views
{
    public partial class MessageDialogWindow : Window
    {
        public string TitleText { get; }
        public string MessageText { get; }

        public MessageDialogWindow(string title, string message)
        {
            InitializeComponent();
            TitleText = title;
            MessageText = message;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

