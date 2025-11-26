using Hangfire.Dashboard;

internal class HangfireLocalAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true; // Note: For local usage only
    }
}
