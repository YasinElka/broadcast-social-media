using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Användarnamn krävs")]
    [Display(Name = "Användarnamn")]
    public string UserName { get; set; } = string.Empty;  

    [Required(ErrorMessage = "Email krävs")]
    [EmailAddress(ErrorMessage = "Ogiltig email-adress")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;  

    [Required(ErrorMessage = "Lösenord krävs")]
    [DataType(DataType.Password)]
    [Display(Name = "Lösenord")]
    [StringLength(100, ErrorMessage = "Lösenordet måste vara minst {2} tecken långt.", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty; 

    [DataType(DataType.Password)]
    [Display(Name = "Bekräfta lösenord")]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = string.Empty;  
}