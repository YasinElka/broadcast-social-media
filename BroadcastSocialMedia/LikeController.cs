[HttpPost]
public async Task<IActionResult> ToggleLike(int broadcastId)
{
    var broadcast = await _context.Broadcasts
        .Include(b => b.Likes)
        .FirstOrDefaultAsync(b => b.Id == broadcastId);

    var user = await _userManager.GetUserAsync(User);

    if (broadcast != null && user != null)
    {
        var existingLike = broadcast.Likes.FirstOrDefault(l => l.UserId == int.Parse(user.Id));
        if (existingLike != null)
        {
            // Remove like
            broadcast.Likes.Remove(existingLike);
            _context.Remove(existingLike);
        }
        else
        {
            // Add like
            var like = new BroadcastSocialMedia.Models.Like
            {
                UserId = int.Parse(user.Id)
            };
            broadcast.Likes.Add(like);
        }

        await _context.SaveChangesAsync();
    }

    return RedirectToAction("Index", "Home");
}