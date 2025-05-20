using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Documenten.Messaging.Configuration;
using OneGround.ZGW.Documenten.ServiceAgent.v1;

namespace OneGround.ZGW.Documenten.Messaging.Services;

public class DeleteDocumentService : BackgroundService
{
    private readonly IServiceScope _serviceScope;

    private ApplicationConfiguration _applicationConfiguration;
    private ILogger<DeleteDocumentService> _logger;
    private IEnkelvoudigInformatieObjectDeletionService _enkelvoudigInformatieObjectDeletionService;
    private IDocumentenServiceAgent _documentenServiceAgent;
    private IOrganisationContextFactory _organisationContextFactory;

    public DeleteDocumentService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<DeleteDocumentService>>();
        _enkelvoudigInformatieObjectDeletionService = _serviceScope.ServiceProvider.GetRequiredService<IEnkelvoudigInformatieObjectDeletionService>();
        _documentenServiceAgent = _serviceScope.ServiceProvider.GetRequiredService<IDocumentenServiceAgent>();
        _organisationContextFactory = _serviceScope.ServiceProvider.GetRequiredService<IOrganisationContextFactory>();

        _applicationConfiguration = _serviceScope
            .ServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection("Application:DocumentDeletionManagement")
            .Get<ApplicationConfiguration>();

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _serviceScope.Dispose();

        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background service started for scanning document deletions");

        _logger.LogInformation(
            "Setting {Setting}: {EnabledForRsins}",
            nameof(_applicationConfiguration.EnabledForRsins),
            _applicationConfiguration.EnabledForRsins
        );
        _logger.LogInformation(
            "Setting {Setting}: {PollingInterval}",
            nameof(_applicationConfiguration.PollingInterval),
            _applicationConfiguration.PollingInterval
        );
        _logger.LogInformation(
            "Setting {Setting}: {OlderThanDuration}",
            nameof(_applicationConfiguration.OlderThanDuration),
            _applicationConfiguration.OlderThanDuration
        );
        _logger.LogInformation("Setting {Setting}: {BatchSize}", nameof(_applicationConfiguration.BatchSize), _applicationConfiguration.BatchSize);

        var succeededDeletions = new List<string>();
        var failedDeletions = new Dictionary<string, ErrorResponse>();

        do
        {
            try
            {
                await Task.Delay(_applicationConfiguration.PollingInterval, cancellationToken);

                // Get all unreferenced documents within a batch
                var documents = (
                    await _enkelvoudigInformatieObjectDeletionService.GetDistinctAsync(
                        _applicationConfiguration.OlderThanDuration,
                        _applicationConfiguration.BatchSize,
                        _applicationConfiguration.EnabledForRsins,
                        cancellationToken
                    )
                ).ToList();

                if (documents.Count != 0)
                {
                    // Before we delete be sure that there are no added the same object+informatieobject combinations in DRC (oterwise we got an API validation error)
                    await _enkelvoudigInformatieObjectDeletionService.DeleteReferencedObjectsAsync(cancellationToken);

                    documents = (
                        await _enkelvoudigInformatieObjectDeletionService.GetDistinctAsync(
                            _applicationConfiguration.OlderThanDuration,
                            _applicationConfiguration.BatchSize,
                            _applicationConfiguration.EnabledForRsins,
                            cancellationToken
                        )
                    ).ToList();

                    foreach (var document in documents)
                    {
                        _logger.LogDebug("Deleting {EnkelvoudiginformatieobjectUrl}", document.EnkelvoudigInformatieObjectUrl);

                        _organisationContextFactory.Create(document.Rsin);

                        var result = await _documentenServiceAgent.DeleteEnkelvoudigInformatieObjectByUrlAsync(
                            document.EnkelvoudigInformatieObjectUrl
                        );
                        if (result.Success)
                        {
                            succeededDeletions.Add(document.EnkelvoudigInformatieObjectUrl);
                        }
                        else
                        {
                            _logger.LogError(
                                "Could not delete {EnkelvoudiginformatieobjectUrl}. Status={Status}. {Detail} {Exception}",
                                document.EnkelvoudigInformatieObjectUrl,
                                result.Error.Status,
                                result.Error.Detail,
                                result.Exception
                            );

                            failedDeletions.Add(document.EnkelvoudigInformatieObjectUrl, result.Error);
                        }
                    }

                    await _enkelvoudigInformatieObjectDeletionService.DeleteAsync(succeededDeletions, cancellationToken);

                    await _enkelvoudigInformatieObjectDeletionService.MarkAsErrorsAsync(failedDeletions, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occured while scanning/deleting documents.");
            }
            finally
            {
                succeededDeletions.Clear();
                failedDeletions.Clear();
            }
        } while (!cancellationToken.IsCancellationRequested);

        _logger.LogInformation("Background service for scanning document deletions finished");
    }
}
