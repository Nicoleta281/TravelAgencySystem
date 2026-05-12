using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Messaging;

namespace TravelAgency.WPF.Views.Common
{
    public partial class UserConversationWindow : Window
    {
        public sealed class MessageLine
        {
            public string Body { get; init; } = "";
            public string TimeText { get; init; } = "";
            public bool IsMine { get; init; }
        }

        private readonly IUserMessageRepository _repo;
        private readonly string _me;
        private readonly string _other;
        private readonly ObservableCollection<MessageLine> _lines = new();

        public UserConversationWindow(
            IUserMessageRepository repo,
            string meUsername,
            string otherUsername,
            string title)
        {
            InitializeComponent();
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _me = (meUsername ?? "").Trim();
            _other = (otherUsername ?? "").Trim();
            Title = title;
            TitleText.Text = title;
            ChatList.ItemsSource = _lines;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _repo.MarkThreadReadForRecipient(_me, _other);
            }
            catch
            {
                // ignoră — afișăm tot istoricul
            }

            ReloadMessages();
        }

        private void ReloadMessages()
        {
            _lines.Clear();
            foreach (var m in _repo.GetConversation(_me, _other))
            {
                var local = m.SentAtUtc.ToLocalTime();
                _lines.Add(new MessageLine
                {
                    Body = m.Body,
                    TimeText = local.ToString("dd.MM.yyyy HH:mm"),
                    IsMine = string.Equals(m.FromUsername, _me, StringComparison.OrdinalIgnoreCase)
                });
            }

            EmptyHint.Visibility = _lines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_lines.Count > 0)
                ChatList.ScrollIntoView(_lines[^1]);
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            var text = (DraftBox.Text ?? "").Trim();
            if (text.Length == 0)
                return;

            try
            {
                _repo.Send(_me, _other, text);
                DraftBox.Clear();
                ReloadMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Mesaj", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
