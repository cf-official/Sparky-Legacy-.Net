using System;
using System.Collections.Generic;

namespace Sparky.Models
{
    public sealed class SparkyUser
    {
        private SparkyUser()
        {
        }

        public string Id { get; set; }

        public int MessageCount { get; set; }

        public DateTimeOffset? LastMessageAt { get; set; }

        public ulong[] RoleIds { get; set; }

        public static SparkyUser New(ulong id)
        {
            return new SparkyUser()
            {
                Id = id.ToString(),
                MessageCount = 0,
                LastMessageAt = null,
                RoleIds = new ulong[0]
            };
        }
    }
}