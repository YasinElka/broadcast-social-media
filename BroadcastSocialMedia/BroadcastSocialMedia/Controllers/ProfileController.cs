using BroadcastSocialMedia.Models;
using BroadcastSocialMedia.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BroadcastSocialMedia.Controllers
{
    // Denna controller hanterar användarprofiler
    public class ProfileController : Controller
    {
        // UserManager för att hantera användare i Identity
        private readonly UserManager<ApplicationUser> _userManager;

        // Konstruktor: Injicera UserManager
        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /Profile/Index - Visar användarens profil
        public async Task<IActionResult> Index()
        {
            // Hämta den inloggade användaren
            var currentUser = await _userManager.GetUserAsync(User);

            // Om ingen användare hittas, redirect till login
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Skapa ViewModel med användarens data
            var model = new ProfileIndexViewModel
            {
                Name = currentUser.UserName ?? string.Empty,          // Användarnamn
                ProfileImageUrl = currentUser.ProfileImageUrl ?? "/images/default-avatar.png",  // Profilbild
                Bio = currentUser.Bio ?? string.Empty,               // Bio/om mig
                Email = currentUser.Email ?? string.Empty            // Email
            };

            // Visa profilsidan med data
            return View(model);
        }

        // POST: /Profile/Update - Uppdaterar användarens profil
        [HttpPost]
        [ValidateAntiForgeryToken]  // Skydd mot CSRF-attacker
        public async Task<IActionResult> Update(ProfileIndexViewModel viewModel)
        {
            // Validera att data är korrekt
            if (!ModelState.IsValid)
            {
                return View("Index", viewModel);
            }

            // Hämta den inloggade användaren
            var user = await _userManager.GetUserAsync(User);

            // Om ingen användare hittas, redirect till login
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Uppdatera användarens information
            user.UserName = viewModel.Name;                          // Nytt användarnamn
            user.Bio = viewModel.Bio ?? string.Empty;                // Ny bio
            user.ProfileImageUrl = viewModel.ProfileImageUrl ?? "/images/default-avatar.png";  // Ny profilbild

            // Uppdatera email om det anges
            if (!string.IsNullOrEmpty(viewModel.Email))
            {
                user.Email = viewModel.Email;                        // Ny email
            }

            // Spara ändringarna till databasen
            var result = await _userManager.UpdateAsync(user);

            // Kontrollera om uppdateringen lyckades
            if (result.Succeeded)
            {
                TempData["Success"] = "Din profil har uppdaterats!"; // Meddelande om lyckat
            }
            else
            {
                // Visa felmeddelanden om det misslyckades
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Index", viewModel);
            }

            // Redirect tillbaka till profilsidan
            return RedirectToAction("Index");
        }
    }
}