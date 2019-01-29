using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sparky.Models
{
    public class Modlog
    {
        public string Id { get; set; }

        public ActionType ActionType { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public static Modlog FromEntry(IAuditLogEntry entry)
        {
            var modlog = new Modlog()
            {
                Id = entry.Id.ToString(),
                ActionType = entry.Action,
                CreatedAt = entry.CreatedAt
            };
        }
    }
}
