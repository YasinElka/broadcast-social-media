// Models/UserFollowing.cs - SKAPA NY FIL
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BroadcastSocialMedia.Models
{
    public class UserFollowing
    {
        public int Id { get; set; }
        public string FollowerId { get; set; } = string.Empty;
        public string FollowingId { get; set; } = string.Empty;
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("FollowerId")]
        public virtual ApplicationUser? Follower { get; set; }

        [ForeignKey("FollowingId")]
        public virtual ApplicationUser? Following { get; set; }
    }
}