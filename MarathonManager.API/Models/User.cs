    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity; 

    namespace MarathonManager.API.Models;

    public partial class User : IdentityUser<int>
    {
        
        public string FullName { get; set; } = null!;
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }


        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordTokenExpiry { get; set; }


    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
        public virtual ICollection<Race> Races { get; set; } = new List<Race>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }