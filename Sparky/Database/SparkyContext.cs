using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sparky.Database
{
    public partial class SparkyContext : DbContext
    {
        public SparkyContext()
        {
        }

        public SparkyContext(DbContextOptions<SparkyContext> options)
            : base(options)
        {
        }

        public virtual DbSet<KarmaEvent> KarmaEvents { get; set; }

        public virtual DbSet<RoleLimit> RoleLimits { get; set; }

        public virtual DbSet<SparkyUser> Users { get; set; }

        public SparkyUser GetOrCreateUser(ulong id)
        {
            var user = Users.FirstOrDefault(u => u.Id == Convert.ToInt64(id));
            if (user == null)
            {
                user = new SparkyUser()
                {
                    Id = Convert.ToInt64(id)
                };

                Add(user);
            }

            return user;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(Configuration.Get<string>("conn_string"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.2-servicing-10034");

            modelBuilder.Entity<KarmaEvent>(entity =>
            {
                entity.ToTable("karma_events");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasMaxLength(50)
                    .ValueGeneratedNever();

                entity.Property(e => e.Amount).HasColumnName("amount");

                entity.Property(e => e.GivenAt)
                    .HasColumnName("given_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.GiverId).HasColumnName("giver_id");

                entity.Property(e => e.RecipientId).HasColumnName("recipient_id");

                entity.HasOne(d => d.Giver)
                    .WithMany(p => p.KarmaEventsGiver)
                    .HasForeignKey(d => d.GiverId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("karma_events_giver_id_fkey");

                entity.HasOne(d => d.Recipient)
                    .WithMany(p => p.KarmaEventsRecipient)
                    .HasForeignKey(d => d.RecipientId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("karma_events_recipient_id_fkey");
            });

            modelBuilder.Entity<RoleLimit>(entity =>
            {
                entity.ToTable("role_limits");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.KarmaRequirement).HasColumnName("karma_requirement");

                entity.Property(e => e.PointRequirement).HasColumnName("point_requirement");
            });

            modelBuilder.Entity<SparkyUser>(entity =>
            {
                entity.ToTable("users");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.LastMessageAt)
                    .HasColumnName("last_message_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql(null);

                entity.Property(e => e.Points).HasColumnName("points");

                entity.Property(e => e.RawRoles)
                    .IsRequired()
                    .HasColumnName("roles")
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb");
            });
        }
    }
}
