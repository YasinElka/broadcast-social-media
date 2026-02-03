using BroadcastSocialMedia.Models;

namespace BroadcastSocialMedia.ViewModels
{
    public class UsersIndexViewModel
    {
        public string Search { get; set; } = string.Empty;
        public List <ApplicationUser> Result{ get; set; } = new List<ApplicationUser>();




    }
}
