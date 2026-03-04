using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BroadcastSocialMedia.Data;
using BroadcastSocialMedia.Models;
using BroadcastSocialMedia.ViewModels;

namespace BroadcastSocialMedia.Controllers
{
    [Authorize]
    public class BroadcastController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public BroadcastController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            var broadcasts = await _context.Broadcasts
                .Include(b => b.User)
                .Include(b => b.Likes)
                .ThenInclude(l => l.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(50)
                .ToListAsync();

            var viewModel = broadcasts.Select(b => new HomeBroadcastViewModel
            {
                Id = b.Id,
                Content = b.Content ?? string.Empty,
                UserName = b.User?.UserName ?? "Unknown",
                UserProfileImage = b.User?.ProfileImageUrl ?? "/images/default-avatar.png",
                CreatedAt = b.CreatedAt,
                ImageUrl = b.ImageUrl,
                LikeCount = b.LikeCount,
                IsLikedByCurrentUser = currentUserId != null &&
                                      b.Likes != null &&
                                      b.Likes.Any(l => l.UserId == currentUserId)
            }).ToList();

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View(new HomeBroadcastViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HomeBroadcastViewModel model, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validera att innehållet inte är tomt
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                TempData["Error"] = "Inlägget får inte vara tomt!";
                return View(model);
            }

            // Validera maxlängd
            if (model.Content.Length > 500)
            {
                TempData["Error"] = "Inlägget får vara max 500 tecken!";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var broadcast = new Broadcast
            {
                Content = model.Content,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                Published = model.CreatedAt <= DateTime.UtcNow
                    ? DateTime.UtcNow
                    : default(DateTime)
            };

            // Hantera bilduppladdning om en bild laddas upp
            if (imageFile != null && imageFile.Length > 0)
            {
                // Validera bildstorlek
                if (imageFile.Length > 5 * 1024 * 1024) // 5MB max
                {
                    TempData["Error"] = "Bilden är för stor. Max 5MB tillåtet.";
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "broadcasts");

                // Skapar mapp om den inte finns
                Directory.CreateDirectory(uploadsFolder);

                // Genererar unikt filnamn
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Sparar filen
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                broadcast.ImageUrl = $"/uploads/broadcasts/{fileName}";
            }

            _context.Broadcasts.Add(broadcast);
            await _context.SaveChangesAsync();

            if (broadcast.Published != default(DateTime))
            {
                TempData["Success"] = "Ditt inlägg har publicerats!";
            }
            else
            {
                TempData["Success"] = "Ditt inlägg har schemalagts!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Like(int id)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Likes)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            var user = await _userManager.GetUserAsync(User);

            if (broadcast == null || user == null)
            {
                return NotFound();
            }

            // Kollar om användaren redan har gillat detta inlägg
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.BroadcastId == id && l.UserId == user.Id);

            if (existingLike != null)
            {
                // Ta bort like
                _context.Likes.Remove(existingLike);
                TempData["Message"] = "Du har ogillat inlägget.";
            }
            else
            {
                // Lägg till ny like
                var like = new Like
                {
                    UserId = user.Id,
                    BroadcastId = id,
                    LikedAt = DateTime.UtcNow
                };

                _context.Likes.Add(like);
                TempData["Message"] = "Du har gillat inlägget!";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Follow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kontrollera att man inte följer sig själv
            if (currentUser.Id == userId)
            {
                TempData["Error"] = "Du kan inte följa dig själv.";
                return RedirectToAction("Recommendations");
            }

            // Kontrollera om redan följer
            var alreadyFollowing = await _context.UserFollowings
                .AnyAsync(uf => uf.FollowerId == currentUser.Id && uf.FollowingId == userId);

            if (!alreadyFollowing)
            {
                var userToFollow = await _userManager.FindByIdAsync(userId);
                if (userToFollow != null)
                {
                    var userFollowing = new UserFollowing
                    {
                        FollowerId = currentUser.Id,
                        FollowingId = userId,
                        FollowedAt = DateTime.UtcNow
                    };

                    _context.UserFollowings.Add(userFollowing);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Du följer nu {userToFollow.UserName ?? "användaren"}!";
                }
            }
            else
            {
                TempData["Info"] = "Du följer redan den här användaren.";
            }

            return RedirectToAction("Recommendations");
        }

        public async Task<IActionResult> Featured()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            // Hämta de 10 mest gillade inläggen
            var popularBroadcasts = await _context.Broadcasts
                .Include(b => b.User)
                .Include(b => b.Likes)
                .OrderByDescending(b => b.Likes.Count)
                .Take(10)
                .ToListAsync();

            var viewModel = popularBroadcasts.Select(b => new HomeBroadcastViewModel
            {
                Id = b.Id,
                Content = b.Content ?? string.Empty,
                UserName = b.User?.UserName ?? "Unknown",
                UserProfileImage = b.User?.ProfileImageUrl ?? "/images/default-avatar.png",
                CreatedAt = b.CreatedAt,
                ImageUrl = b.ImageUrl,
                LikeCount = b.LikeCount,
                IsLikedByCurrentUser = currentUserId != null &&
                                      b.Likes != null &&
                                      b.Likes.Any(l => l.UserId == currentUserId)
            }).ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> Recommendations()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var recommendedUsers = await _context.Users
                .Where(u => u.Id != currentUser.Id)
                .Take(10)
                .ToListAsync();

            var viewModel = recommendedUsers.Select(u => new UsersListenToUserViewModel
            {
                UserId = u.Id ?? string.Empty,
                UserName = u.UserName ?? string.Empty,
                ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                Bio = u.Bio ?? string.Empty
            }).ToList();

            return View(viewModel);
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
            var followingRelation = await _context.UserFollowings
                .Include(uf => uf.Following) // Lägg till Include för att ladda Following
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUser.Id && uf.FollowingId == userId);

            if (followingRelation != null)
            {
                _context.UserFollowings.Remove(followingRelation);
                await _context.SaveChangesAsync();

                if (followingRelation.Following != null)
                {
                    TempData["Success"] = $"Du följer inte längre {followingRelation.Following.UserName}";
                }
                else
                {
                    TempData["Success"] = "Du följer inte längre den här användaren.";
                }
            }
            else
            {
                TempData["Error"] = "Du följer inte den här användaren.";
            }

            return RedirectToAction("Index", "Home");
        }
    }
}