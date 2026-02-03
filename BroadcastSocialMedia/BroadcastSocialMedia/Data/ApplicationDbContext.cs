using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BroadcastSocialMedia.Models;

namespace BroadcastSocialMedia.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Broadcast> Broadcasts { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<UserFollowing> UserFollowings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Konfigurera Broadcast
            builder.Entity<Broadcast>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.HasOne(b => b.User)
                    .WithMany(u => u.Broadcasts)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(b => b.Content)
                    .IsRequired() // Gör Content required
                    .HasDefaultValue(""); // Defaultvärde tom sträng

                entity.Property(b => b.CreatedAt)
                    .HasDefaultValueSql("GETDATE()"); // Auto-datum

                entity.Property(b => b.Published)
                    .HasDefaultValueSql("GETDATE()"); // Auto-datum
            });

            // Konfigurera Like
            builder.Entity<Like>(entity =>
            {
                entity.HasKey(l => l.Id);

                // Konfigurera relation till User
                entity.HasOne(l => l.User)
                    .WithMany(u => u.Likes)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Konfigurera relation till Broadcast
                entity.HasOne(l => l.Broadcast)
                    .WithMany(b => b.Likes)
                    .HasForeignKey(l => l.BroadcastId)
                    .OnDelete(DeleteBehavior.Cascade); // Ta bort likes när broadcast tas bort

                // Gör kombinationen av UserId och BroadcastId unik
                // (En användare kan bara likea en broadcast en gång)
                entity.HasIndex(l => new { l.UserId, l.BroadcastId })
                    .IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                entity.Property(l => l.LikedAt)
                    .HasDefaultValueSql("GETDATE()"); // Auto-datum
            });

            // Konfigurera UserFollowing
            builder.Entity<UserFollowing>(entity =>
            {
                entity.HasKey(uf => uf.Id);



                entity.HasOne(uf => uf.Follower)
                    .WithMany(u => u.Following)
                    .HasForeignKey(uf => uf.FollowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(uf => uf.Following)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(uf => uf.FollowingId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Gör kombinationen unik (en användare kan inte följa samma person två gånger)
                entity.HasIndex(uf => new { uf.FollowerId, uf.FollowingId })
                    .IsUnique();

                entity.Property(uf => uf.FollowedAt)
                    .HasDefaultValueSql("GETDATE()"); // Auto-datum
            });
        }
    }
}