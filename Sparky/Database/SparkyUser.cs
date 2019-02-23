using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sparky.Database
{
    public partial class SparkyUser
    {
        public SparkyUser()
        {
            KarmaEventsGiver = new HashSet<KarmaEvent>();
            KarmaEventsRecipient = new HashSet<KarmaEvent>();
        }

        public long Id { get; set; }

        public int Points { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public string RawRoles { get; set; }

        [NotMapped]
        public ulong[] Roles
        {
            get => JsonConvert.DeserializeObject<ulong[]>(RawRoles);
            set => RawRoles = JsonConvert.SerializeObject(value);
        }

        public virtual ICollection<KarmaEvent> KarmaEventsGiver { get; set; }

        public virtual ICollection<KarmaEvent> KarmaEventsRecipient { get; set; }
    }
}
