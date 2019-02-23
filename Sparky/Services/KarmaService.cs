using Discord;
using Discord.WebSocket;
using Sparky.Database;
using System;
using System.Linq;
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

            using (var dctx = new SparkyContext())
            {
                var relevantEvents = dctx.KarmaEvents
                    .Where(k => k.RecipientId == Convert.ToInt64(message.Author.Id) || k.GiverId == Convert.ToInt64(reaction.UserId)).ToList();

                // Check if the giver has already given on this message
                var eventOnMessage = relevantEvents.FirstOrDefault(e => e.Id == KarmaEvent.GetId(reaction.UserId, message.Id));
                if (eventOnMessage != null)
                    return;

                // Check if the giver is allowed to give in general
                var lastGiverEvent = relevantEvents.Where(e => e.GiverId == Convert.ToInt64(reaction.UserId)).OrderByDescending(e => e.GivenAt).FirstOrDefault();
                if (DateTimeOffset.UtcNow.Subtract(lastGiverEvent?.GivenAt ?? DateTime.MinValue).TotalMinutes < Configuration.Get<int>("karma_limit_all"))
                    return;

                // Check if the giver has given to recipient witin limit
                var lastGiverToRecipient = relevantEvents.Where(e => e.GiverId == Convert.ToInt64(reaction.UserId) && e.RecipientId == Convert.ToInt64(message.Author.Id))
                    .OrderByDescending(e => e.GivenAt).FirstOrDefault();
                if (DateTimeOffset.UtcNow.Subtract(lastGiverToRecipient?.GivenAt ?? DateTime.MinValue).TotalMinutes < Configuration.Get<int>("karma_limit_mutual"))
                    return;

                // Write new event to db
                dctx.Add(KarmaEvent.New(reaction.UserId, message, 1));

                await dctx.SaveChangesAsync();
            }
        }

        private Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!VerifyIsKarmaEmote(reaction))
                return Task.CompletedTask;

            using (var dctx = new SparkyContext())
            {
                dctx.Remove(KarmaEvent.GetId(reaction.UserId, cacheable.Id));

                return dctx.SaveChangesAsync();
            }
        }

        private bool VerifyIsKarmaEmote(SocketReaction reaction) => reaction.Emote.Name.Equals(Configuration.Get<string>("karma_emote_name"));

        private bool VerifyIsKarmaChannel(SocketReaction reaction) => Configuration.Get<ulong[]>("karma_channels").Any(id => id == reaction.Channel.Id);

        public static (int rank, int amount) GetKarmaRank(ulong userId)
        {
            using (var dctx = new SparkyContext())
            {
                var karmaRanks = KarmaEvent.GetForAllUsers(dctx.KarmaEvents.ToList(), dctx.Users.ToList());
                var userRank = karmaRanks.First(r => r.Item1 == userId);

                return (karmaRanks.IndexOf(userRank) + 1, userRank.Item2);
            }
        }

        public static int GetKarma(ulong userId)
        {
            using (var dctx = new SparkyContext())
            {
                var userKarma = dctx.KarmaEvents.Where(e => e.RecipientId == Convert.ToInt64(userId));

                return userKarma.Sum(e => e.Amount);
            }
        }
    }
}
