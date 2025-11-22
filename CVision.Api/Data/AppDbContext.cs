using CVision.Api.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CVision.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Job> Jobs { get; set; }
    public DbSet<Candidate> Candidates { get; set;}
    public DbSet<CandidateProfile> CandidateProfiles { get; set; }
}
