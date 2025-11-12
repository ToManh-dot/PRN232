using MarathonManager.Web.DTOs;
using System.Collections.Generic;

namespace MarathonManager.Web.ViewModels
{
    public class AdminUserDetailsViewModel
    {
        public UserDto User { get; set; }
        public List<RoleDto> AllRoles { get; set; }

        public AdminUserDetailsViewModel()
        {
            User = new UserDto();
            AllRoles = new List<RoleDto>();
        }
    }
}
