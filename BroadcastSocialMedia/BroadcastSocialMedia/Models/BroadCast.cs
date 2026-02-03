using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BroadcastSocialMedia.Models
{
    public class Broadcast
    {
        [Key]
        public int Id { get; set; }

        // Foreign key 
        public string? UserId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime Published { get; set; }
        public string? Platform { get; set; }

        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

        // Beräknad egenskap för att få antalet likes
        [NotMapped]
        public int LikeCount => Likes?.Count ?? 0;
    }

    public class Like
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int BroadcastId { get; set; }

        [ForeignKey("BroadcastId")]
        public virtual Broadcast? Broadcast { get; set; }

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }
}