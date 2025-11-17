using MarathonManager.API.DTOs.Blog;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogPostsController : ControllerBase
    {
        private readonly MarathonManagerContext _context;

        public BlogPostsController(MarathonManagerContext context)
        {
            _context = context;
        }

        // GET: api/BlogPosts
        [HttpGet]
        [AllowAnonymous] // Cho phép xem công khai
        public async Task<ActionResult<IEnumerable<BlogSummaryDto>>> GetBlogPosts()
        {
            var posts = await _context.BlogPosts
                .Where(p => p.Status == "Published") 
                .OrderByDescending(p => p.CreatedAt) 
                .Take(3) 
                .Select(p => new BlogSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    CreatedAt = p.CreatedAt,
                    Summary = p.Content.Length > 100
                              ? p.Content.Substring(0, 100) + "..."
                              : p.Content
                })
                .ToListAsync();

            return Ok(posts);
        }

    }
}