public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Job> Jobs { get; set; }
    public DbSet<Candidate> Candidates { get; set;}
    public DbSet<CandidateProfile> CandidateProfiles { get; set; }

}
