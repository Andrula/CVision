using Hangfire.Dashboard;

namespace CVision.Api.Configuration;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For development, allow all access
        // In production, you should implement proper authorization
        var httpContext = context.GetHttpContext();

        // TODO: Implement proper authorization for production
        // For now, only allow in development environment
        return httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();
    }
}
