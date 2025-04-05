
namespace Domain.ViewModels.UserPanel.Tutorials
{
    public class TutorialDetailsViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } // محتوای HTML
        public string CoverImagePath { get; set; }
        public string? Category { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public List<DownloadLinkViewModel> DownloadLinks { get; set; } = new List<DownloadLinkViewModel>();
    }
}
