using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sparky.Database
{
    public partial class KarmaEvent
    {
        public string Id { get; set; }

        public long RecipientId { get; set; }

        public long GiverId { get; set; }

        public int Amount { get; set; }

        public DateTime GivenAt { get; set; }

        public virtual SparkyUser Giver { get; set; }

        public virtual SparkyUser Recipient { get; set; }

        public static string GetId(ulong giverId, ulong messageId) => $"{giverId}:{messageId}";

        public static KarmaEvent New(ulong giverId, IMessage message, int amount)
            => New(giverId, message.Id, message.Author.Id, amount);

        public static KarmaEvent New(ulong giverId, ulong messageId, ulong recipientId, int amount)
        {
            return new KarmaEvent
            {
                Id = GetId(giverId, messageId),
                GiverId = Convert.ToInt64(giverId),
                RecipientId = Convert.ToInt64(recipientId),
                Amount = amount,
                GivenAt = DateTime.UtcNow
            };
        }

        public static List<(ulong, int)> GetForAllUsers(List<KarmaEvent> events, List<SparkyUser> users)
        {
            var karmaRankings = new List<(ulong, int)>();
            foreach (var user in users)
                karmaRankings.Add(((ulong) user.Id, events.Where(e => e.RecipientId == user.Id).Sum(e => e.Amount)));
            return karmaRankings.OrderByDescending(r => r.Item2).ToList();
        }
    }
}
