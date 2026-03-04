using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BroadcastSocialMedia.ViewModels
{
    public class CreateBroadcastViewModel
    {
        [Required(ErrorMessage = "Du måste skriva något!")]
        [StringLength(500, ErrorMessage = "Max 500 tecken")]
        [Display(Name = "Vad tänker du på?")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Ladda upp bild (valfritt)")]
        public IFormFile? ImageFile { get; set; }
    }
}