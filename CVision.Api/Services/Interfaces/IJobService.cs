namespace CVision.Api.Services.Interfaces;

public interface IJobService
{
    Task<IEnumerable<JobWithCountDto>> GetAllJobsAsync(int companyId);
    Task<Job?> GetJobByIdAsync(int id, int companyId);
    Task<Job> CreateJobAsync(Job job);
    Task<bool> DeleteJobAsync(int id, int companyId);
    Task<IEnumerable<object>> GetSkillDistributionAsync(int jobId, int companyId);
}