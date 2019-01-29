using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Sparky.Models;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Sparky.Services
{
    public static class Database
    {
        private static Lazy<IDocumentStore> store = new Lazy<IDocumentStore>(CreateStore);

        public static IDocumentStore Store => store.Value;

        private static IDocumentStore CreateStore()
        {
            X509Certificate2 cert = (X509Certificate2)X509Certificate.CreateFromCertFile(Configuration.Get<string>("cert_path"));
            return new DocumentStore()
            {
                Urls = new[] { Configuration.Get<string>("database_url") },
                Certificate = cert,
                Database = "Sparky"
            }.Initialize();
        }

        public static async Task<SparkyUser> EnsureCreatedAsync(IAsyncDocumentSession session, ulong id)
        {
            var user = await session.LoadAsync<SparkyUser>(id.ToString());
            if (user == null)
            {
                user = SparkyUser.New(id);
                await session.StoreAsync(user);
            }

            return user;
        }
    }
}