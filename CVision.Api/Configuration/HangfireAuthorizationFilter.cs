using Hangfire.Dashboard;

namespace CVision.Api.Configuration;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, you should implement proper authorization
        // For now, allow all requests in development
        return true;
    }
}
