using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Models.Messaging;

namespace TravelAgency.Core.Data.Repositories
{
    public class EfUserMessageRepository : IUserMessageRepository
    {
        public void Send(string fromUsername, string toUsername, string body)
        {
            var from = (fromUsername ?? "").Trim();
            var to = (toUsername ?? "").Trim();
            var text = (body ?? "").Trim();

            if (from.Length == 0 || to.Length == 0 || string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Destinatar sau expeditor invalid.");

            if (text.Length == 0)
                throw new ArgumentException("Mesajul nu poate fi gol.");

            if (text.Length > 2000)
                text = text[..2000];

            using var db = TravelAgencyDbContextFactory.Create();
            db.UserMessages.Add(new UserMessageEntity
            {
                FromUsername = from,
                ToUsername = to,
                Body = text,
                SentAtUtc = DateTime.UtcNow,
                IsRead = false
            });
            db.SaveChanges();
        }

        public IReadOnlyList<UserMessage> GetConversation(string userA, string userB, int maxMessages = 400)
        {
            var a = (userA ?? "").Trim();
            var b = (userB ?? "").Trim();
            if (a.Length == 0 || b.Length == 0)
                return Array.Empty<UserMessage>();

            using var db = TravelAgencyDbContextFactory.Create();
            var q = db.UserMessages
                .AsNoTracking()
                .Where(m =>
                    (m.FromUsername == a && m.ToUsername == b) ||
                    (m.FromUsername == b && m.ToUsername == a))
                .OrderByDescending(m => m.SentAtUtc)
                .Take(maxMessages);

            var list = q.AsEnumerable().Reverse().Select(Map).ToList();
            return list;
        }

        public int GetUnreadCount(string forUsername)
        {
            var u = (forUsername ?? "").Trim();
            if (u.Length == 0)
                return 0;

            using var db = TravelAgencyDbContextFactory.Create();
            return db.UserMessages.Count(m => m.ToUsername == u && !m.IsRead);
        }

        public void MarkThreadReadForRecipient(string recipient, string sender)
        {
            var r = (recipient ?? "").Trim();
            var s = (sender ?? "").Trim();
            if (r.Length == 0 || s.Length == 0)
                return;

            using var db = TravelAgencyDbContextFactory.Create();
            var rows = db.UserMessages
                .Where(m => m.ToUsername == r && m.FromUsername == s && !m.IsRead)
                .ToList();

            foreach (var m in rows)
                m.IsRead = true;

            if (rows.Count > 0)
                db.SaveChanges();
        }

        private static UserMessage Map(UserMessageEntity e) =>
            new()
            {
                Id = e.Id,
                FromUsername = e.FromUsername,
                ToUsername = e.ToUsername,
                Body = e.Body,
                SentAtUtc = e.SentAtUtc,
                IsRead = e.IsRead
            };
    }
}
