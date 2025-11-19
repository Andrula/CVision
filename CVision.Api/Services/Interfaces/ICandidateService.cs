namespace CVision.Api.Services.Interfaces;

public interface ICandidateService
{
<<<<<<< HEAD
    Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId);
    Task<CandidateProfile?> GetProfileAsync(int id);
    Task<Candidate> UploadCandidateAsync(int jobId, IFormFile file);
    Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto);
    Task<Stream?> GetCandidateCvStreamAsync(int id);
    Task<IEnumerable<object>> GetCandidatesWithMatchScoreAsync(int jobId);
    Task<CandidateProfile> ReparseProfileAsync(int profileId);  // ← Denne linje skal være der!
=======
    Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId, int companyId);
    Task<CandidateProfile?> GetProfileAsync(int id, int companyId);
    Task<Candidate> UploadCandidateAsync(int jobId, int companyId, IFormFile file, string userId);
    Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto, int companyId, string userId);
    Task<Stream?> GetCandidateCvStreamAsync(int id, int companyId);
    Task<IEnumerable<object>> GetCandidatesWithMatchScoreAsync(int jobId, int companyId);
>>>>>>> origin/feature/identity-authentication
}