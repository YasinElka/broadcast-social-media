using System;

namespace BroadcastSocialMedia.ViewModels
{
    public class HomeBroadcastViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty; 
        public string UserProfileImage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
       
        public string? ImageUrl { get; set; }
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}