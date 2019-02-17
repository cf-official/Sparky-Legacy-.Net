using Discord;
using Discord.WebSocket;
using Raven.Client.Documents;
using Sparky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparky.Services
{
    public sealed class KarmaService
    {
        private readonly DiscordSocketClient _client;

        public KarmaService(DiscordSocketClient client)
        {
            _client = client;
            _client.ReactionAdded += HandleReactionAdded;
            _client.ReactionRemoved += HandleReactionRemoved;
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!VerifyIsKarmaEmote(reaction) || !VerifyIsKarmaChannel(reaction))
                return;
            var message = await cacheable.GetOrDownloadAsync();
            if (message.Author.IsBot || message.Author.Id == reaction.UserId)
                return;

            using (var session = Database.Store.OpenAsyncSession())
            {
                var relevantEvents = await session.Query<KarmaEvent>()
                    .Where(k => k.RecipientId == message.Author.Id || k.GiverId == reaction.UserId).ToListAsync();

                // Check if the giver has already given on this message
                var eventOnMessage = relevantEvents.FirstOrDefault(e => e.Id == KarmaEvent.GetId(reaction.UserId, message.Id));
                if (eventOnMessage != null)
                    return;

                // Check if the giver is allowed to give in general
                var lastGiverEvent = relevantEvents.Where(e => e.GiverId == reaction.UserId).OrderByDescending(e => e.CreatedAt).FirstOrDefault();
                if (DateTimeOffset.UtcNow.Subtract(lastGiverEvent?.CreatedAt ?? DateTimeOffset.MinValue).TotalMinutes < Configuration.Get<int>("karma_limit_all"))
                    return;

                // Check if the giver has given to recipient witin limit
                var lastGiverToRecipient = relevantEvents.Where(e => e.GiverId == reaction.UserId && e.RecipientId == message.Author.Id).OrderByDescending(e => e.CreatedAt).FirstOrDefault();
                if (DateTimeOffset.UtcNow.Subtract(lastGiverToRecipient?.CreatedAt ?? DateTimeOffset.MinValue).TotalMinutes < Configuration.Get<int>("karma_limit_mutual"))
                    return;

                // Write new event to db
                await session.StoreAsync(KarmaEvent.New(reaction.UserId, message, 1));

                await session.SaveChangesAsync();
            }
        }

        private Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!VerifyIsKarmaEmote(reaction))
                return Task.CompletedTask;

            using (var session = Database.Store.OpenAsyncSession())
            {
                session.Delete(KarmaEvent.GetId(reaction.UserId, cacheable.Id));

                return session.SaveChangesAsync();
            }
        }

        private bool VerifyIsKarmaEmote(SocketReaction reaction) => reaction.Emote.Name.Equals(Configuration.Get<string>("karma_emote_name"));

        private bool VerifyIsKarmaChannel(SocketReaction reaction) => Configuration.Get<ulong[]>("karma_channels").Any(id => id == reaction.Channel.Id);

        public static async Task<(int rank, int amount)> GetKarmaRankAsync(ulong userId)
        {
            using (var userSession = Database.Store.OpenAsyncSession())
            using (var karmaSession = Database.Store.OpenAsyncSession())
            {
                var users = await userSession.Query<SparkyUser>().ToListAsync();
                var events = await userSession.Query<KarmaEvent>().ToListAsync();

                var karmaRanks = KarmaEvent.GetForAllUsers(events, users);
                var userRank = karmaRanks.First(r => r.Item1 == userId.ToString());

                return (karmaRanks.IndexOf(userRank) + 1, userRank.Item2);
            }
        }

        public static async Task<int> GetKarmaAsync(ulong userId)
        {
            using (var karmaSession = Database.Store.OpenAsyncSession())
            {
                var userKarma = await karmaSession.Query<KarmaEvent>().Where(e => e.RecipientId == userId).ToListAsync();

                return userKarma.Sum(e => e.Amount);
            }
        }
    }
}
