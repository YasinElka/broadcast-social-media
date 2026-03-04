using BroadcastSocialMedia.Data;
using BroadcastSocialMedia.Models;
using BroadcastSocialMedia.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace BroadcastSocialMedia.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;
        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IWebHostEnvironment environment)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
            _environment = environment;
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
        [HttpGet]
        [Authorize]
        public IActionResult CreateBroadcast()
        {
            return View(new CreateBroadcastViewModel());
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBroadcast(CreateBroadcastViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var broadcast = new Broadcast
            {
                Content = model.Content,
                UserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Enkel bilduppladdning
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);// Generera unikt filnamn
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName); // Spara filen i wwwroot/uploads

                using (var stream = new FileStream(filePath, FileMode.Create)) // Spara filen på servern
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                broadcast.ImageUrl = "/uploads/" + fileName; // Spara bildens URL i databasen
            }

            _dbContext.Broadcasts.Add(broadcast);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Inlägg publicerat!"; // Lägg till en framgångsmeddelande

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Profile(string id)
        {
            // Om inget ID anges, använd inloggad användares ID
            if (string.IsNullOrEmpty(id))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                id = currentUser.Id;
            }

            try
            {
                var user = await _dbContext.Users
                    .Include(u => u.Broadcasts)
                    .ThenInclude(b => b.Likes)
                    .Include(u => u.Followers)
                    .Include(u => u.Following)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound($"Användare med id {id} hittades inte");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                return Content($"Fel: {ex.Message}");
            }
        }


        [HttpGet]
        public async Task<IActionResult> Popular()
        {
            var currentUserId = User.Identity?.IsAuthenticated == true
                ? (await _userManager.GetUserAsync(User))?.Id
                : null;

            // Hämta de mest populära broadcasts (med flest likes)
            var popularBroadcasts = await _dbContext.Broadcasts
                .Include(b => b.User)  // VIKTIGT: Inkludera användaren
                .Include(b => b.Likes)
                .Where(b => b.UserId != null && b.User != null)  // Bara broadcasts med giltig användare
                .OrderByDescending(b => b.Likes.Count)
                .ThenByDescending(b => b.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Mappa till viewmodell - inkludera profilbild!
            var broadcastViewModels = popularBroadcasts.Select(b => new HomeBroadcastViewModel
            {
                Id = b.Id,
                Content = b.Content ?? "",
                UserName = b.User?.UserName ?? "Okänd",
                UserProfileImage = b.User?.ProfileImageUrl ?? "/images/default-avatar.png",  // VIKTIG!
                CreatedAt = b.CreatedAt,
                ImageUrl = b.ImageUrl,
                LikeCount = b.Likes?.Count ?? 0,
                IsLikedByCurrentUser = currentUserId != null &&
                                      b.Likes != null &&
                                      b.Likes.Any(l => l.UserId == currentUserId)
            }).ToList();

            var viewModel = new HomeIndexViewModel
            {
                Broadcasts = broadcastViewModels
            };

            ViewData["Title"] = "Populära inlägg";
            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Recommendations()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Hämtar IDn på användare som currentUser redan följer
            var followingIds = await _dbContext.UserFollowings
                .Where(uf => uf.FollowerId == currentUser.Id)
                .Select(uf => uf.FollowingId)
                .ToListAsync();

            // Hämtar användare som currentUser INTE följer (max 10 st)
            var recommendations = await _dbContext.Users
                .Where(u => u.Id != currentUser.Id && !followingIds.Contains(u.Id))
                .Select(u => new UserRecommendationViewModel

                {
                    UserId = u.Id,
                    UserName = u.UserName ?? "Okänd",
                    ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                    Bio = u.Bio ?? "",
                    FollowerCount = u.Followers.Count
                })
                .Take(10)
                .ToListAsync();

            return View(recommendations);
        }


        [HttpGet]
        public async Task<IActionResult> Following()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var following = await _dbContext.UserFollowings
                .Include(uf => uf.Following)
                .Where(uf => uf.FollowerId == currentUser.Id)
                .Select(uf => new FollowingViewModel
                {
                    UserId = uf.FollowingId,
                    UserName = uf.Following.UserName,
                    ProfileImageUrl = uf.Following.ProfileImageUrl ?? "/images/default-avatar.png",
                    Bio = uf.Following.Bio,
                    FollowedAt = uf.FollowedAt
                })
                .ToListAsync();

            return View(following);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.Id == userId)
            {
                TempData["Error"] = "Du kan inte följa dig själv.";
                return RedirectToAction("Search");
            }

            // Kontrollera om redan följer
            var existingFollow = await _dbContext.UserFollowings
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUser.Id && uf.FollowingId == userId);

            if (existingFollow == null)
            {
                var follow = new UserFollowing
                {
                    FollowerId = currentUser.Id,
                    FollowingId = userId,
                    FollowedAt = DateTime.UtcNow
                };

                _dbContext.UserFollowings.Add(follow);
                await _dbContext.SaveChangesAsync();
                TempData["Success"] = $"Du följer nu användaren!";
            }
            else
            {
                TempData["Info"] = "Du följer redan den här användaren.";
            }

            return RedirectToAction("Search", new { query = ViewBag.SearchQuery?.ToString() ?? "" });
        }




        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var viewModel = new UsersIndexViewModel
            {
                Search = query ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                return View(viewModel);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            // Hämta alla användare som matchar sökningen
            var users = await _dbContext.Users
                .Where(u => u.UserName != null &&
                           (u.UserName.Contains(query) ||
                           (u.Email != null && u.Email.Contains(query))))
                .Select(u => new UserSearchResult
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                    Bio = u.Bio ?? string.Empty,
                    FollowerCount = u.Followers.Count,
                    IsFollowing = currentUserId != null &&
                                 _dbContext.UserFollowings.Any(uf =>
                                     uf.FollowerId == currentUserId &&
                                     uf.FollowingId == u.Id)
                })
                .ToListAsync();

            viewModel.Result = users;
            ViewBag.SearchQuery = query;
            ViewBag.CurrentUserId = currentUserId;

            return View(viewModel);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Broadcast(HomeBroadcastViewModel viewModel, IFormFile? imageFile)
        {

            if (string.IsNullOrWhiteSpace(viewModel.Content))
            {
                TempData["Error"] = "Meddelande är obligatorisk fält.";
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
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                Published = DateTime.UtcNow
            };

            // Bilduppladdning (om bild finns)
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                broadcast.ImageUrl = $"/uploads/{uniqueFileName}";
            }

            _dbContext.Broadcasts.Add(broadcast);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Broadcast skapad!";
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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int broadcastId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            // Hitta gilla-markering
            var like = await _dbContext.Likes
                .FirstOrDefaultAsync(l => l.BroadcastId == broadcastId && l.UserId == userId);

            if (like == null)
            {
                // Lägg till ny gilla-markering
                _dbContext.Likes.Add(new Like
                {
                    BroadcastId = broadcastId,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Ta bort gilla-markering
                _dbContext.Likes.Remove(like);
            }

            await _dbContext.SaveChangesAsync();

            // Räkna antal gilla-markeringar
            var likeCount = await _dbContext.Likes.CountAsync(l => l.BroadcastId == broadcastId);

            // Skicka tillbaka till sidan
            return RedirectToAction("Index", "Home");
        }

    }
}
