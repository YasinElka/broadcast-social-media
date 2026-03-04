namespace BroadcastSocialMedia.ViewModels
{
    public class UserRecommendationViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public int FollowerCount { get; set; }
    }
}