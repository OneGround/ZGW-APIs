using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Messaging.Filters;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Messaging.Configuration;
using OneGround.ZGW.Documenten.Messaging.Consumers;
using OneGround.ZGW.Documenten.Messaging.Contracts;
using OneGround.ZGW.Documenten.Messaging.Services;
using OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;

namespace OneGround.ZGW.Documenten.Messaging;

public class ServiceConfiguration
{
    private readonly IConfiguration _configuration;

    public ServiceConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDocumentenServiceAgent(_configuration);
        services.AddOrganisationContext();
        services.AddCorrelationId();
        services.AddScoped<BatchIdHandler>();
        services.AddBatchId();
        services.AddServiceEndpoints(_configuration);

        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

        services.AddSingleton<IEnkelvoudigInformatieObjectDeletionService, EnkelvoudigInformatieObjectDeletionService>();

        // Note: Install Background service for scanning document deletions (queued by DestroyEnkelvoudigInformatieObjectConsumer)
        //services.AddHostedService<DeleteDocumentService>();

        services.AddMassTransit(x =>
        {
            x.DisableUsageTelemetry();
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<AddObjectInformatieObjectConsumer>();
            x.AddConsumer<DeleteObjectInformatieObjectConsumer>();
            x.AddConsumer<DestroyEnkelvoudigInformatieObjectConsumer>();

            x.AddRequestClient<IAddObjectInformatieObject>();
            x.AddRequestClient<IDeleteObjectInformatieObject>();

            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    var eventbusConfiguration = _configuration.GetSection("Eventbus").Get<DocumentenEventBusConfiguration>();

                    cfg.Host(
                        eventbusConfiguration.HostName,
                        eventbusConfiguration.VirtualHost,
                        h =>
                        {
                            h.Username(eventbusConfiguration.UserName);
                            h.Password(eventbusConfiguration.Password);
                        }
                    );
                    cfg.ReceiveEndpoint(
                        eventbusConfiguration.ReceiveQueue,
                        e =>
                        {
                            e.PrefetchCount = eventbusConfiguration.ReceivePrefetchCount;
                            e.EnablePriority(2);

                            e.ConfigureConsumer<AddObjectInformatieObjectConsumer>(context);
                            e.ConfigureConsumer<DeleteObjectInformatieObjectConsumer>(context);
                            e.ConfigureConsumer<DestroyEnkelvoudigInformatieObjectConsumer>(context);
                            e.UseMessageRetry(r =>
                                r.Interval(eventbusConfiguration.ReceiveEndpointRetries, eventbusConfiguration.ReceiveEndpointTimeout * 1000)
                            );
                        }
                    );

                    cfg.UseConsumeFilter(typeof(RsinFilter<>), context);
                    cfg.UseConsumeFilter(typeof(CorrelationIdFilter<>), context);

                    cfg.UseConsumeFilter(typeof(BatchIdConsumingFilter<>), context);
                    cfg.UseSendFilter(typeof(BatchIdSendingFilter<>), context);
                    cfg.UsePublishFilter(typeof(BatchIdPublishFilter<>), context);

                    cfg.ConfigureEndpoints(context);
                }
            );
        });
    }
}
