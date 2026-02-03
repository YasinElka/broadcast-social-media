using System.ComponentModel.DataAnnotations;

namespace BroadcastSocialMedia.ViewModels
{
    public class ProfileIndexViewModel
    {
        // Användarnamn - obligatoriskt fält
        [Required(ErrorMessage = "Namn krävs")]
        [Display(Name = "Användarnamn")]
        public string Name { get; set; } = string.Empty;

        // Bio - valfritt fält med maxlängd
        [Display(Name = "Bio")]
        [StringLength(500, ErrorMessage = "Bio får max vara 500 tecken")]
        public string Bio { get; set; } = string.Empty;

        // URL till profilbild - valfritt fält
        [Display(Name = "Profilbild URL")]
        [Url(ErrorMessage = "Ange en giltig URL")]
        public string ProfileImageUrl { get; set; } = string.Empty;

        
        // Email - valfritt fält med email-validering
        [EmailAddress(ErrorMessage = "Ange en giltig email-adress")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}