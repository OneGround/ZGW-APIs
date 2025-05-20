using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Web.Middleware;
using Serilog;

namespace OneGround.ZGW.Common.Web.Extensions.ApplicationBuilder;

public static class ZGWApiApplicationBuilderExtensions
{
    public static void ConfigureZGWApi(
        this IApplicationBuilder app,
        IWebHostEnvironment env,
        bool dontRegisterLogBadRequestMiddleware = false,
        Action<IApplicationBuilder> registerMiddleware = null
    )
    {
        app.UseSerilogRequestLogging();
        app.UseForwardedHeaders();
        app.UseMiddleware<ApiVersionMiddleware>();

        if (!dontRegisterLogBadRequestMiddleware)
        {
            app.UseMiddleware<LogBadRequestMiddleware>();
        }

        if (env.IsLocal())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
        }

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<EnrichedLoggingMiddleware>();

        // Register additional (API specific) Middleware. Should be done after Authentication, Authorization and Logging but before mapping the controllers, etc!
        registerMiddleware?.Invoke(app);

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapControllers();
        });
    }
}
