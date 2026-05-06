using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using NodaTime;
using NodaTime.Text;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.DataAccess.Encryption;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.Authorization;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

/// <summary>
/// In-memory ZrcDbContext subclass for unit testing.
/// Skips PostgreSQL-specific model config (PostGIS, pgcrypto, collations, GiST indexes).
/// Provides a functional IVersionedHmacHasher mock so SaveChangesAsync auto-hashing works.
/// </summary>
internal class UnitTestZrcDbContext : ZrcDbContext
{
    public UnitTestZrcDbContext(DbContextOptions<ZrcDbContext> options, IVersionedHmacHasher? versionedHasher = null)
        : base(options, new Mock<IDatabaseProtector<ZrcDbContext>>().Object, versionedHasher ?? CreateDefaultHasherMock()) { }

    private static IVersionedHmacHasher CreateDefaultHasherMock()
    {
        var mock = new Mock<IVersionedHmacHasher>();
        mock.Setup(h => h.ComputeHash(It.IsAny<string>())).Returns((string p) => $"hashed-{p}");
        mock.Setup(h => h.ComputeHash(It.IsAny<string>(), It.IsAny<string>())).Returns((string p, string _) => $"hashed-{p}");
        mock.Setup(h => h.Latest).Returns("v1");
        mock.Setup(h => h.ComputeAllHashes(It.IsAny<string>())).Returns((string p) => new Dictionary<string, string> { { "v1", $"hashed-{p}" } });
        return mock.Object;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Minimal model configuration for in-memory testing.
        // Skip: HasPostgresExtension, ApplyDataProtectionConverters, UseCollation, HasMethod("gist"),
        //       TempZaakAuthorization view, NotAnEntity, ci_collation

        modelBuilder.Entity<ZaakRol>().HasIndex(p => p.Betrokkene);
        modelBuilder.Entity<ZaakRol>().HasIndex(p => p.BetrokkeneType);
        modelBuilder.Entity<ZaakRol>().HasIndex(p => p.Omschrijving);
        modelBuilder.Entity<ZaakRol>().HasIndex(p => p.OmschrijvingGeneriek);
        modelBuilder.Entity<ZaakRol>().HasIndex(p => p.RolType);
        modelBuilder.Entity<ZaakRol>().HasIndex(p => p.Owner);

        modelBuilder.Entity<NatuurlijkPersoonZaakRol>(entity =>
        {
            entity.Property(p => p.InpBsnEncrypted).HasColumnName("inpbsn_encrypted");
            entity.Property(p => p.InpBsnHash).HasColumnName("inpbsn_hash").HasMaxLength(64);
            entity.Property(p => p.InpBsnHashKeyVersion).HasColumnName("inpbsn_hash_key_version").HasMaxLength(10);
            entity.HasIndex(p => p.InpBsnHash);
        });

        modelBuilder.Entity<NatuurlijkPersoonZaakRol>().HasIndex(p => p.InpBsn);

        modelBuilder.Entity<Zaak>().Property(p => p.Archiefstatus).HasDefaultValue(ArchiefStatus.nog_te_archiveren);
        modelBuilder.Entity<Zaak>().Property(p => p.BetalingsIndicatie).HasDefaultValue(BetalingsIndicatie.nvt);
        modelBuilder
            .Entity<Zaak>()
            .Property(p => p.VertrouwelijkheidAanduiding)
            .HasDefaultValue(Common.DataModel.VertrouwelijkheidAanduiding.openbaar);

        modelBuilder.Entity<AuditTrailRegel>().HasIndex(p => p.HoofdObjectId);

        // Exclude entities that depend on PostgreSQL features
        modelBuilder.Ignore<NotAnEntity>();
        modelBuilder.Ignore<TempZaakAuthorization>();

        // NodaTime Period type doesn't have a parameterless constructor for InMemory provider
        modelBuilder
            .Entity<ZaakVerlenging>()
            .Property(p => p.Duur)
            .HasConversion(p => p != null ? p.ToString() : null, s => s != null ? PeriodPattern.Roundtrip.Parse(s).Value : Period.Zero);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }
}
