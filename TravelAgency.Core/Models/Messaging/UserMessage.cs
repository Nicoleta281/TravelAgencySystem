using System;

namespace TravelAgency.Core.Models.Messaging
{
    public class UserMessage
    {
        public int Id { get; set; }
        public string FromUsername { get; set; } = "";
        public string ToUsername { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime SentAtUtc { get; set; }
        public bool IsRead { get; set; }
    }
}
