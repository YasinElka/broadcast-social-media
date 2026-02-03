using BroadcastSocialMedia.Data;
using BroadcastSocialMedia.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using BroadcastSocialMedia.ViewModels;

namespace BroadcastSocialMedia.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.Identity?.IsAuthenticated == true
              ? (await _userManager.GetUserAsync(User))?.Id
              : null;

            List<Broadcast> broadcasts;

            if (User.Identity?.IsAuthenticated == true)
            {
                // Om inloggad: visa broadcasts från användare man följer
                var followingIds = await _dbContext.UserFollowings
                    .Where(uf => uf.FollowerId == currentUserId)
                    .Select(uf => uf.FollowingId)
                    .ToListAsync();

                broadcasts = await _dbContext.Broadcasts
                    .Where(b => b.UserId != null && followingIds.Contains(b.UserId))
                    .Include(b => b.User)
                    .Include(b => b.Likes)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(20)
                    .ToListAsync();
            }
            else
            {
                // Om ej inloggad: visa alla broadcasts
                broadcasts = await _dbContext.Broadcasts
                    .Include(b => b.User)
                    .Include(b => b.Likes)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(20)
                    .ToListAsync();
            }

            // Mappa Broadcast till HomeBroadcastViewModel
            var broadcastViewModels = broadcasts.Select(b => new HomeBroadcastViewModel
            {
                Id = b.Id,
                Content = b.Content,
                UserName = b.User?.UserName ?? "Unknown",
                UserProfileImage = b.User?.ProfileImageUrl ?? "/images/default-avatar.png",
                CreatedAt = b.CreatedAt,
                Platform = b.Platform ?? "",
                ImageUrl = b.ImageUrl,
                LikeCount = b.Likes.Count,
                IsLikedByCurrentUser = currentUserId != null &&
                                      b.Likes.Any(l => l.UserId == currentUserId)
            }).ToList();

            // Skapa HomeIndexViewModel
            var viewModel = new HomeIndexViewModel
            {
                Broadcasts = broadcastViewModels
            };

            return View(viewModel);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        public async Task<IActionResult> Broadcast(HomeBroadcastViewModel viewModel)
        {
            if (string.IsNullOrWhiteSpace(viewModel.Content) ||
                string.IsNullOrWhiteSpace(viewModel.Platform))
            {
                TempData["Error"] = "Meddelande och plattform är obligatoriska fält.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var broadcast = new Broadcast()
            {
                Content = viewModel.Content,
                Platform = viewModel.Platform,
                UserId = user.Id,  // Använd UserId istället för User
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Broadcasts.Add(broadcast);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Unfollow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Hitta följande-relationen
            var followingRelation = await _dbContext.UserFollowings
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUser.Id && uf.FollowingId == userId);

            if (followingRelation != null)
            {
                _dbContext.UserFollowings.Remove(followingRelation);
                await _dbContext.SaveChangesAsync();
                TempData["Success"] = "Du har slutat följa användaren.";
            }
            else
            {
                TempData["Error"] = "Du följer inte den här användaren.";
            }

            return RedirectToAction("Index");
        }
    }
}
