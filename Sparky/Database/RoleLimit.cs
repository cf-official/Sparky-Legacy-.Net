using System;

namespace Sparky.Database
{
    public partial class RoleLimit
    {
        public long Id { get; set; }

        public int PointRequirement { get; set; }

        public int KarmaRequirement { get; set; }

        public static RoleLimit New(ulong id, int pointRequirement, int karmaRequirement)
        {
            return new RoleLimit()
            {
                Id = Convert.ToInt64(id),
                PointRequirement = pointRequirement,
                KarmaRequirement = karmaRequirement
            };
        }
    }
}
