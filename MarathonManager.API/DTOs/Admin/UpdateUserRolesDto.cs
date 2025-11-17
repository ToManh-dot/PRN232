using System.Collections.Generic;

namespace MarathonManager.API.DTOs.Admin
{
    public class UpdateUserRolesDto
    {
        public List<string> RoleNames { get; set; }
    }
}