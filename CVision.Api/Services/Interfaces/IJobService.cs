namespace CVision.Api.Services.Interfaces;

public interface IJobService
{
    Task<IEnumerable<JobWithCountDto>> GetAllJobsAsync();
    Task<Job?> GetJobByIdAsync(int id);
    Task<Job> CreateJobAsync(Job job);
    Task<bool> DeleteJobAsync(int id);
    Task<IEnumerable<object>> GetSkillDistributionAsync(int jobId);
}