using BroadcastSocialMedia.Models;

namespace BroadcastSocialMedia.ViewModels
{
    public class UsersIndexViewModel
    {
        public string Search { get; set; } = string.Empty;
        public List<UserSearchResult> Result { get; set; } = new();
    }

    public class UserSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public int FollowerCount { get; set; }
        public bool IsFollowing { get; set; }
    }
}