namespace Sparky.Models
{
    public sealed class RoleLimit
    {
        public string Id { get; set; }

        public int MessageCount { get; set; }

        public int KarmaCount { get; set; }

        public static RoleLimit New(ulong id, int messageCount, int karmaCount)
        {
            return new RoleLimit()
            {
                Id = id.ToString(),
                MessageCount = messageCount,
                KarmaCount = karmaCount
            };
        }
    }
}