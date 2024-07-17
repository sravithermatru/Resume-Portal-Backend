using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Resume_Selector_Page.Models;

namespace Resume_Selector_Page.Data
{
    public class AppDbContext : IdentityDbContext<Recruiter>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Resume> ResumesData { get; set; }
        public DbSet<DownloadedResume> DownloadedResumes { get; set; }
    }
}
