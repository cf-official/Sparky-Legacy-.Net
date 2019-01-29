using Newtonsoft.Json;

namespace Sparky.Models
{
    public sealed class RoleLimit
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("message_count")]
        public int MessageCount { get; set; }

        [JsonProperty("karma_count")]
        public int KarmaCount { get; set; }
    }
}
