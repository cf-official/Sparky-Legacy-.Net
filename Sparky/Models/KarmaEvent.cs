using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sparky.Models
{
    public sealed class KarmaEvent
    {
        private KarmaEvent()
        {
        }

        public string Id { get; set; }

        public ulong GiverId { get; set; }

        public ulong RecipientId { get; set; }

        public int Amount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public static KarmaEvent New(ulong giverId, IMessage message, int amount)
        {
            return new KarmaEvent
            {
                Id = GetId(giverId, message.Id),
                GiverId = giverId,
                RecipientId = message.Author.Id,
                Amount = amount,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        public static string GetId(ulong giverId, ulong messageId) => $"{giverId}:{messageId}";

        public static List<(string, int)> GetForAllUsers(List<KarmaEvent> events, List<SparkyUser> users)
        {
            var karmaRankings = new List<(string, int)>();
            foreach (var user in users)
                karmaRankings.Add((user.Id, events.Where(e => e.RecipientId.ToString() == user.Id).Sum(e => e.Amount)));
            return karmaRankings.OrderByDescending(r => r.Item2).ToList();
        }
    }
}
