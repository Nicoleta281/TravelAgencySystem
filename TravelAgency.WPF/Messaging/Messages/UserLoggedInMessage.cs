using System.Windows;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.WPF.Messaging.Messages
{
    public sealed record UserLoggedInMessage(User User, Window SourceWindow);
}

