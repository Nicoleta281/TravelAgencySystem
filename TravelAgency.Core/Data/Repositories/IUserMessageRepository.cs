using System.Collections.Generic;
using TravelAgency.Core.Models.Messaging;

namespace TravelAgency.Core.Data.Repositories
{
    public interface IUserMessageRepository
    {
        void Send(string fromUsername, string toUsername, string body);

        IReadOnlyList<UserMessage> GetConversation(string userA, string userB, int maxMessages = 400);

        int GetUnreadCount(string forUsername);

        /// <summary>Marchează citite mesajele primite de <paramref name="recipient"/> de la <paramref name="sender"/>.</summary>
        void MarkThreadReadForRecipient(string recipient, string sender);
    }
}
