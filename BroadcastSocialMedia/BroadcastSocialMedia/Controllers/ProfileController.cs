using BroadcastSocialMedia.Models;
using BroadcastSocialMedia.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BroadcastSocialMedia.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager; // UserManager for ApplicationUser

        public ProfileController(UserManager<ApplicationUser>userManager)
        {
           _userManager = userManager; // Dependency injection of UserManager
        }
        public async Task <IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User); // Get the current user's ID
           
            var viewModel = new ProfileIndexViewModel()
            {
                Name = user.Name ?? "" // Set the Name property in the ViewModel
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Update(ProfileIndexViewModel viewModel)
        {
            var user = await _userManager.GetUserAsync(User);
            user.Name = viewModel.Name; // Update the Name property
            await _userManager.UpdateAsync(user); // Save changes to the user

            return RedirectToAction("/"); // Redirect to the Index action
        }
    }
}
