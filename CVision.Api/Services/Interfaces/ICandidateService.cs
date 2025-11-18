namespace CVision.Api.Services.Interfaces;

public interface ICandidateService
{
    Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId);
    Task<CandidateProfile?> GetProfileAsync(int id);
    Task<Candidate> UploadCandidateAsync(int jobId, IFormFile file);
    Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto);
    Task<Stream?> GetCandidateCvStreamAsync(int id);
    Task<IEnumerable<object>> GetCandidatesWithMatchScoreAsync(int jobId);
    Task<CandidateProfile> ReparseProfileAsync(int profileId);  // ← Denne linje skal være der!
}