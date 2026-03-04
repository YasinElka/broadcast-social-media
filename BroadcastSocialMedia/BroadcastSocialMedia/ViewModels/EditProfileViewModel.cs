using System.ComponentModel.DataAnnotations;

namespace BroadcastSocialMedia.ViewModels
{
    public class EditProfileViewModel
    {
        public string CurrentProfileImage { get; set; }
        
        [StringLength(200, ErrorMessage = "Max 200 tecken")]
        public string Bio { get; set; }
    }
}