using MarathonManager.Web.DTOs;

namespace MarathonManager.Web.ViewModels
{
    public class HomePageViewModel
    {
        public List<RaceSummaryDto> FeaturedRaces { get; set; }
        public List<BlogSummaryDto> RecentBlogPosts { get; set; }

        public HomePageViewModel()
        {
            FeaturedRaces = new List<RaceSummaryDto>();
            RecentBlogPosts = new List<BlogSummaryDto>();
        }
    }
}