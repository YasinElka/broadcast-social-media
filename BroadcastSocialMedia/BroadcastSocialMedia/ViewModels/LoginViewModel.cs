using System.ComponentModel.DataAnnotations;

namespace YourProject.ViewModels.LoginViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Användarnamn är obligatoriskt")]
        [Display(Name = "Användarnamn")]
        public string UserName { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Lösenord är obligatoriskt")]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Kom ihåg mig")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}