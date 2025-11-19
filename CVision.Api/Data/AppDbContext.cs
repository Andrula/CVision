using CVision.Api.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CVision.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<License> Licenses { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Candidate> Candidates { get; set; }
    public DbSet<CandidateProfile> CandidateProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add index for efficient caching lookup
        modelBuilder.Entity<Candidate>()
            .HasIndex(c => new { c.FileHash, c.JobId })
            .HasDatabaseName("IX_Candidates_FileHash_JobId");
        // Company -> License (1:1)
        modelBuilder.Entity<Company>()
            .HasOne(c => c.License)
            .WithOne(l => l.Company)
            .HasForeignKey<License>(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Company -> Users (1:Many)
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Users)
            .WithOne(u => u.Company)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Company -> Jobs (1:Many)
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Jobs)
            .WithOne(j => j.Company)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job -> Candidates (1:Many)
        modelBuilder.Entity<Job>()
            .HasMany(j => j.Candidates)
            .WithOne(c => c.Job)
            .HasForeignKey(c => c.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job -> CandidateProfiles (1:Many)
        modelBuilder.Entity<Job>()
            .HasMany(j => j.CandidateProfiles)
            .WithOne(cp => cp.Job)
            .HasForeignKey(cp => cp.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Candidate -> CandidateProfiles (1:Many)
        modelBuilder.Entity<Candidate>()
            .HasMany(c => c.Profiles)
            .WithOne(cp => cp.Candidate)
            .HasForeignKey(cp => cp.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        modelBuilder.Entity<Job>()
            .HasIndex(j => j.CompanyId);

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.CompanyId);

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
