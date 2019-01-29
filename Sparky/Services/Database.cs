using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Sparky.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sparky.Services
{
    public static class Database
    {
        private static Lazy<IDocumentStore> store = new Lazy<IDocumentStore>(CreateStore);

        public static IDocumentStore Store => store.Value;

        private static IDocumentStore CreateStore()
            => new DocumentStore()
            {
                Urls = new[] { "http://localhost:8080" },
                Database = "Sparky"
            }.Initialize();

        public static async Task<SparkyUser> EnsureCreatedAsync(IAsyncDocumentSession session, ulong id)
        {
            var user = await session.LoadAsync<SparkyUser>(id.ToString());
            if (user == null)
            {
                user = new SparkyUser()
                {
                    Id = id.ToString(),
                    MessageCount = 0,
                    LastMessageAt = null,
                    RoleIds = new ulong[0],
                    Karma = 0,
                    KarmaGivers = new Dictionary<ulong, DateTimeOffset>()
                };
                await session.StoreAsync(user);
            }

            return user;
        }
    }
}
