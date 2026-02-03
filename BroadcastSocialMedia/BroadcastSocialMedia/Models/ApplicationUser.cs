// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BroadcastSocialMedia.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string ProfileImageUrl { get; set; } = "/images/default-avatar.png";
        public string Bio { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Broadcast> Broadcasts { get; set; } = new List<Broadcast>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

        // Följande-system
        public virtual ICollection<UserFollowing> Following { get; set; } = new List<UserFollowing>();
        public virtual ICollection<UserFollowing> Followers { get; set; } = new List<UserFollowing>();

        [NotMapped]
        public int FollowerCount => Followers?.Count ?? 0;

        [NotMapped]
        public int FollowingCount => Following?.Count ?? 0;
    }
}