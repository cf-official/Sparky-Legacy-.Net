namespace Sparky.Models
{
    public sealed class RoleLimit
    {
        public string Id { get; set; }

        public int MessageCount { get; set; }

        public int KarmaCount { get; set; }
    }
}