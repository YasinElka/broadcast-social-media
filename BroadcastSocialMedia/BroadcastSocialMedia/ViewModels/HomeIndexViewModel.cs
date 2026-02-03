using System.Collections.Generic;

namespace BroadcastSocialMedia.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<HomeBroadcastViewModel> Broadcasts { get; set; } = new List<HomeBroadcastViewModel>();

        // Lägg till dessa om du behöver:
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}
