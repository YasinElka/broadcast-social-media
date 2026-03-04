using BroadcastSocialMedia.Models;
using BroadcastSocialMedia.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourProject.ViewModels;
using YourProject.ViewModels.LoginViewModel;

namespace BroadcastSocialMedia.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // REGISTRERING 
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kontrollera om användarnamnet redan finns
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null)
                {
                    ModelState.AddModelError("UserName", "Användarnamnet är redan upptaget.");
                    return View(model);
                }

                // Kontrollera om e-post redan finns
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "E-postadressen är redan registrerad.");
                    return View(model);
                }

                // Skapa ny användare
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    ProfileImageUrl = "/images/default-avatar.png",
                    Bio = string.Empty
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Logga in användaren direkt
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["Success"] = "Välkommen! Ditt konto har skapats.";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }


        // INLOGGNING
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // Skapa en tom modell och skicka till vyn
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Välkommen tillbaka, {model.UserName}!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Ogiltigt användarnamn eller lösenord.");
            }

            return View(model);
        }

        // UTLOGGNING 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Info"] = "Du är nu utloggad.";
            return RedirectToAction("Index", "Home");
        }

        //  VALIDERING FÖR UNIKT ANVÄNDARNAMN 
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> IsUserNameAvailable(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return Json("Användarnamn krävs.");

            var user = await _userManager.FindByNameAsync(userName);

            if (user != null)
                return Json($"Användarnamnet '{userName}' är redan upptaget.");

            if (userName.Length < 3)
                return Json("Användarnamnet måste vara minst 3 tecken.");

            if (userName.Length > 20)
                return Json("Användarnamnet får max vara 20 tecken.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(userName, @"^[a-zA-Z0-9_]+$"))
                return Json("Endast bokstäver, siffror och underscore är tillåtna.");

            return Json(true);
        }

        // VALIDERING FÖR UNIK E-POST 
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> IsEmailAvailable(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json("E-post krävs.");

            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
                return Json($"E-postadressen '{email}' är redan registrerad.");

            return Json(true);
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new EditProfileViewModel
            {
                CurrentProfileImage = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Bio = user.Bio
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(IFormFile imageFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                // Kolla filtyp
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Ogiltigt filformat. Använd .jpg, .png, .gif eller .webp";
                    return RedirectToAction("EditProfile");
                }

                // Kolla filstorlek (max 5MB)
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Bilden är för stor. Max 5MB";
                    return RedirectToAction("EditProfile");
                }

                try
                {
                    // Skapa mapp om den inte finns
                    string uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Skapa unikt filnamn
                    string fileName = Guid.NewGuid().ToString() + fileExtension;
                    string filePath = Path.Combine(uploadPath, fileName);

                    // Spara filen
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Ta bort gammal profilbild (om det inte är standardbilden)
                    if (!string.IsNullOrEmpty(user.ProfileImageUrl) &&
                        !user.ProfileImageUrl.Contains("default-avatar"))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath,
                            user.ProfileImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Uppdatera användarens profilbild
                    user.ProfileImageUrl = "/uploads/profiles/" + fileName;
                    await _userManager.UpdateAsync(user);

                    TempData["Success"] = "Profilbild uppdaterad!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Ett fel uppstod: {ex.Message}";
                }
            }

            return RedirectToAction("EditProfile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            user.Bio = model.Bio;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profil uppdaterad!";
            return RedirectToAction("EditProfile");
        }

        // ÅTKOMST NEKAD 
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}