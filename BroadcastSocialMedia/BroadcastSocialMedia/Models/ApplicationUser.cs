using Microsoft.AspNetCore.Identity;

namespace BroadcastSocialMedia.Models
{
    public class ApplicationUser : IdentityUser

    {
        public string? Name { get; set; } // defines a Name property for the user


    }
}
