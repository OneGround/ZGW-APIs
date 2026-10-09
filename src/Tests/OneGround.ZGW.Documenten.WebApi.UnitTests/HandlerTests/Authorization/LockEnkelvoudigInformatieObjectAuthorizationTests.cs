using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Concurrency;
using Xunit;
using static OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests.Authorization.DrcAuthorizationTestSupport;
using V1_1 = OneGround.ZGW.Documenten.Web.Handlers.v1._1;
using V1_5 = OneGround.ZGW.Documenten.Web.Handlers.v1._5;
using V1_7 = OneGround.ZGW.Documenten.Web.Handlers.v1._7;
using VertrouwelijkheidAanduiding = OneGround.ZGW.Common.DataModel.VertrouwelijkheidAanduiding;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests.Authorization;

public class LockEnkelvoudigInformatieObjectAuthorizationTests : IDisposable
{
    private const string ExistingLock = "0123456789abcdef0123456789abcdef";

    private static readonly string[] LockScopes = [AuthorizationScopes.Documenten.Lock];
    private static readonly string[] UnlockScopes = [AuthorizationScopes.Documenten.Lock, AuthorizationScopes.Documenten.ForcedUnlock];

    private readonly DrcDbContext _context = CreateDbContext();
    private readonly Mock<IAuditTrailFactory> _auditTrailFactory = AuditTrailFactory();

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData("v1.1")]
    [InlineData("v1.5")]
    [InlineData("v1.7")]
    public async Task Locking_a_document_of_a_type_the_client_is_not_authorized_for_is_forbidden_and_leaves_it_unlocked(string version)
    {
        var document = await SeedDocumentAsync(_context, TypeA);
        var client = Client(LockScopes, Permission(TypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Lock));

        var result = await HandleAsync(version, document.Id, set: true, lockId: null, client);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        Assert.False(Reload(document.Id).Locked);
        _auditTrailFactory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData("v1.1")]
    [InlineData("v1.5")]
    [InlineData("v1.7")]
    public async Task Locking_a_document_above_the_clients_maximum_vertrouwelijkheidaanduiding_is_forbidden(string version)
    {
        var document = await SeedDocumentAsync(_context, TypeA, VertrouwelijkheidAanduiding.geheim);
        var client = Client(LockScopes, Permission(TypeA, VertrouwelijkheidAanduiding.vertrouwelijk, AuthorizationScopes.Documenten.Lock));

        var result = await HandleAsync(version, document.Id, set: true, lockId: null, client);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
    }

    [Theory]
    [InlineData("v1.1")]
    [InlineData("v1.5")]
    [InlineData("v1.7")]
    public async Task Locking_a_document_of_an_authorized_type_locks_it(string version)
    {
        var document = await SeedDocumentAsync(_context, TypeA);
        var client = Client(LockScopes, Permission(TypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Lock));

        var result = await HandleAsync(version, document.Id, set: true, lockId: null, client);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.True(Reload(document.Id).Locked);
    }

    [Theory]
    [InlineData("v1.1")]
    [InlineData("v1.5")]
    [InlineData("v1.7")]
    public async Task Unlocking_with_the_lock_id_on_a_type_the_client_is_not_authorized_for_is_forbidden(string version)
    {
        var document = await SeedLockedDocumentAsync(TypeA);
        var client = Client(UnlockScopes, Permission(TypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Lock));

        var result = await HandleAsync(version, document.Id, set: false, lockId: ExistingLock, client);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        Assert.True(Reload(document.Id).Locked);
    }

    [Theory]
    [InlineData("v1.1")]
    [InlineData("v1.5")]
    [InlineData("v1.7")]
    public async Task Forced_unlock_right_on_another_type_does_not_force_unlock_this_document(string version)
    {
        var document = await SeedLockedDocumentAsync(TypeA);
        var client = Client(
            UnlockScopes,
            Permission(TypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Lock),
            Permission(TypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.ForcedUnlock)
        );

        var result = await HandleAsync(version, document.Id, set: false, lockId: null, client);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Contains(result.Errors, e => e.Code == ErrorCode.MissingLockId);
        Assert.True(Reload(document.Id).Locked);
    }

    [Theory]
    [InlineData("v1.1")]
    [InlineData("v1.5")]
    [InlineData("v1.7")]
    public async Task Forced_unlock_right_on_the_documents_type_unlocks_it(string version)
    {
        var document = await SeedLockedDocumentAsync(TypeA);
        var client = Client(UnlockScopes, Permission(TypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.ForcedUnlock));

        var result = await HandleAsync(version, document.Id, set: false, lockId: null, client);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.False(Reload(document.Id).Locked);
    }

    private async Task<EnkelvoudigInformatieObject> SeedLockedDocumentAsync(string type)
    {
        var document = await SeedDocumentAsync(_context, type);
        var tracked = _context.EnkelvoudigInformatieObjecten.Single(e => e.Id == document.Id);
        tracked.Locked = true;
        tracked.Lock = ExistingLock;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return document;
    }

    private EnkelvoudigInformatieObject Reload(Guid id)
    {
        _context.ChangeTracker.Clear();
        return _context.EnkelvoudigInformatieObjecten.Single(e => e.Id == id);
    }

    private Task<CommandResult<string>> HandleAsync(string version, Guid id, bool set, string lockId, IAuthorizationContextAccessor client)
    {
        var configuration = CreateConfiguration();
        var resolver = new Mock<IDocumentServicesResolver>();
        resolver.Setup(r => r.GetDefault()).Returns(Mock.Of<IDocumentService>());
        var retryOptions = new Mock<IOptionsMonitor<HttpRetryStrategyOptions>>();
        retryOptions.Setup(o => o.CurrentValue).Returns(new HttpRetryStrategyOptions());
        var pipeline = new ResilienceConcurrencyRetryPipeline<EnkelvoudigInformatieObject>(
            NullLogger<ResilienceConcurrencyRetryPipeline<EnkelvoudigInformatieObject>>.Instance,
            retryOptions.Object
        );

        return version switch
        {
            "v1.1" => new V1_1.LockEnkelvoudigInformatieObjectCommandHandler(
                NullLogger<V1_1.LockEnkelvoudigInformatieObjectCommandHandler>.Instance,
                configuration,
                _context,
                Mock.Of<IEntityUriService>(),
                _auditTrailFactory.Object,
                client,
                Mock.Of<INotificatieService>(),
                resolver.Object,
                KenmerkenResolver().Object,
                pipeline
            ).Handle(
                new V1_1.LockEnkelvoudigInformatieObjectCommand
                {
                    Id = id,
                    Set = set,
                    Lock = lockId,
                },
                CancellationToken.None
            ),
            "v1.5" => new V1_5.LockEnkelvoudigInformatieObjectCommandHandler(
                NullLogger<V1_5.LockEnkelvoudigInformatieObjectCommandHandler>.Instance,
                configuration,
                _context,
                Mock.Of<IEntityUriService>(),
                _auditTrailFactory.Object,
                client,
                Mock.Of<INotificatieService>(),
                resolver.Object,
                KenmerkenResolver().Object,
                pipeline
            ).Handle(
                new V1_5.LockEnkelvoudigInformatieObjectCommand
                {
                    Id = id,
                    Set = set,
                    Lock = lockId,
                },
                CancellationToken.None
            ),
            "v1.7" => new V1_7.LockEnkelvoudigInformatieObjectCommandHandler(
                NullLogger<V1_7.LockEnkelvoudigInformatieObjectCommandHandler>.Instance,
                configuration,
                _context,
                Mock.Of<IEntityUriService>(),
                _auditTrailFactory.Object,
                client,
                Mock.Of<INotificatieService>(),
                resolver.Object,
                KenmerkenResolver().Object,
                pipeline
            ).Handle(
                new V1_7.LockEnkelvoudigInformatieObjectCommand
                {
                    Id = id,
                    Set = set,
                    Lock = lockId,
                },
                CancellationToken.None
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }
}
