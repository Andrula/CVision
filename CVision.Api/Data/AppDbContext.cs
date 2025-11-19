public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Job> Jobs { get; set; }
    public DbSet<Candidate> Candidates { get; set;}
    public DbSet<CandidateProfile> CandidateProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add index for efficient caching lookup
        modelBuilder.Entity<Candidate>()
            .HasIndex(c => new { c.FileHash, c.JobId })
            .HasDatabaseName("IX_Candidates_FileHash_JobId");
    }
}
