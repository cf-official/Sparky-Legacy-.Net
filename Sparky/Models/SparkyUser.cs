using System;
using System.Collections.Generic;

namespace Sparky.Models
{
    public sealed class SparkyUser
    {
        public SparkyUser()
        {
        }

        public string Id { get; set; }

        public int MessageCount { get; set; }

        public DateTimeOffset? LastMessageAt { get; set; }

        public int Karma { get; set; }

        public Dictionary<ulong, DateTimeOffset> KarmaGivers { get; set; }

        public ulong[] RoleIds { get; set; }

        public static SparkyUser New(ulong id)
        {
            return new SparkyUser()
            {
                Id = id.ToString(),
                MessageCount = 0,
                LastMessageAt = null,
                RoleIds = new ulong[0],
                Karma = 0,
                KarmaGivers = new Dictionary<ulong, DateTimeOffset>()
            };
        }
    }
}