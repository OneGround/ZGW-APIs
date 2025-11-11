using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using Roxit.ZGW.Documenten.Jobs;
using Roxit.ZGW.Documenten.Jobs.InformatieObjecten;

namespace OneGround.ZGW.Documenten.Messaging.Handlers;

class DeleteObjectInformatieObjectHandler : IRequestHandler<DeleteObjectInformatieObjectCommand, CommandResult<string>>
{
    private readonly ILogger<DeleteObjectInformatieObjectHandler> _logger;

    public DeleteObjectInformatieObjectHandler(ILogger<DeleteObjectInformatieObjectHandler> logger)
    {
        _logger = logger;
    }

    public Task<CommandResult<string>> Handle(DeleteObjectInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        var batchId = request.BatchId;

        // Enqueue Hangfire job which adds the related zaak- or besluit-informatieobject (objectinformatieobject) into the DRC
        //   Note: Zaak- or Besluit-informatieobjecten deleted with a batchid should be handled with low priority (so choose a different queue)
        var job = BackgroundJob.Enqueue<DeleteObjectInformatieObjectJob>(
            queue: batchId.HasValue ? Constants.DrcListenerLowPriorityQueue : Constants.DrcListenerQueue,
            methodCall: h => h.ExecuteAsync(request.Rsin, request.Object, request.CorrelationId, batchId, request.InformatieObjectKenmerk, null)
        );

        _logger.LogInformation("Enqueued {DeleteObjectInformatieObjectJob} with Job ID: {JobId}", nameof(DeleteObjectInformatieObjectJob), job);

        return Task.FromResult(new CommandResult<string>($"Enqueued Job: {job}", CommandStatus.OK));
    }
}

class DeleteObjectInformatieObjectCommand : IRequest<CommandResult<string>>
{
    public string Rsin { get; internal set; }
    public Guid CorrelationId { get; internal set; }
    public Guid? BatchId { get; internal set; }
    public string Object { get; internal set; }
    public KeyValuePair<string, string> InformatieObjectKenmerk { get; internal set; }
}
