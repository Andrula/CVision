namespace CVision.Api.Services.Interfaces;

public interface ICandidateService
{
    Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId, int companyId);
    Task<CandidateProfile?> GetProfileAsync(int id, int companyId);
    Task<Candidate> UploadCandidateAsync(int jobId, int companyId, IFormFile file, string userId);
    Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto, int companyId, string userId);
    Task<Stream?> GetCandidateCvStreamAsync(int id, int companyId);
    Task<IEnumerable<object>> GetCandidatesWithMatchScoreAsync(int jobId, int companyId);
    Task<CandidateProfile> ReparseProfileAsync(int profileId);  

}