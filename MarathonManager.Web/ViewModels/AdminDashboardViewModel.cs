
using MarathonManager.Web.DTOs;

using X.PagedList; 
using System.Collections.Generic;

namespace MarathonManager.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public IPagedList<RaceSummaryDto> AllRaces { get; set; }

        public IPagedList<UserDto> AllUsers { get; set; }

        public IPagedList<BlogAdminDto> AllBlogPosts { get; set; }

        public AdminDashboardViewModel()
        {
            AllRaces = new PagedList<RaceSummaryDto>(new List<RaceSummaryDto>(), 1, 1);
            AllUsers = new PagedList<UserDto>(new List<UserDto>(), 1, 1);
            AllBlogPosts = new PagedList<BlogAdminDto>(new List<BlogAdminDto>(), 1, 1);
        }
    }
}