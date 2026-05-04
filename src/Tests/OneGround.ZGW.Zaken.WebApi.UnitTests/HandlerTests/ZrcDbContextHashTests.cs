using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

public class ZrcDbContextHashTests
{
    private static UnitTestZrcDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase($"zrc-hash-{Guid.NewGuid()}").Options);

    private static Zaak CreateZaak() =>
        new()
        {
            Id = Guid.NewGuid(),
            Owner = "813264571",
            Zaaktype = "http://catalogi.local/api/v1/zaaktypen/1",
            Bronorganisatie = "813264571",
            Identificatie = $"ZAAK-{Guid.NewGuid()}",
            Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
            Archiefstatus = ArchiefStatus.nog_te_archiveren,
            Communicatiekanaal = string.Empty,
            Selectielijstklasse = string.Empty,
            VerantwoordelijkeOrganisatie = "813264571",
        };

    [Fact]
    public async Task SaveChangesAsync_WhenBsnResetToNull_ClearsHashAndVersion()
    {
        // Arrange — create a rol with a BSN, then reset BSN to null on update
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        var zaak = CreateZaak();
        var persoon = new NatuurlijkPersoonZaakRol { InpBsn = "999999990", InpBsnEncrypted = "999999990" };
        var rol = new ZaakRol
        {
            Zaak = zaak,
            RolType = "http://catalogi.local/api/v1/roltypen/1",
            Roltoelichting = "test",
            Omschrijving = "Initiator",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = persoon,
            Owner = zaak.Owner,
        };
        db.ZaakRollen.Add(rol);
        await db.SaveChangesAsync();

        Assert.NotNull(persoon.InpBsnHash);
        Assert.NotNull(persoon.InpBsnHashKeyVersion);

        // Act — reset BSN to null (simulate removing BSN on update)
        persoon.InpBsnEncrypted = null;
        await db.SaveChangesAsync();

        // Assert
        Assert.Null(persoon.InpBsnHash);
        Assert.Null(persoon.InpBsnHashKeyVersion);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenBsnAdded_SetsHashAndVersion()
    {
        // Arrange
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        var zaak = CreateZaak();
        var persoon = new NatuurlijkPersoonZaakRol { InpBsn = "999999990", InpBsnEncrypted = "999999990" };
        var rol = new ZaakRol
        {
            Zaak = zaak,
            RolType = "http://catalogi.local/api/v1/roltypen/1",
            Roltoelichting = "test",
            Omschrijving = "Initiator",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = persoon,
            Owner = zaak.Owner,
        };
        db.ZaakRollen.Add(rol);

        // Act
        await db.SaveChangesAsync();

        // Assert
        Assert.Equal("hashed-999999990", persoon.InpBsnHash);
        Assert.Equal("v1", persoon.InpBsnHashKeyVersion);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenBsnNullOnAdd_LeavesHashNull()
    {
        // Arrange
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        var zaak = CreateZaak();
        var persoon = new NatuurlijkPersoonZaakRol { InpBsnEncrypted = null };
        var rol = new ZaakRol
        {
            Zaak = zaak,
            RolType = "http://catalogi.local/api/v1/roltypen/1",
            Roltoelichting = "test",
            Omschrijving = "Initiator",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = persoon,
            Owner = zaak.Owner,
        };
        db.ZaakRollen.Add(rol);

        // Act
        await db.SaveChangesAsync();

        // Assert
        Assert.Null(persoon.InpBsnHash);
        Assert.Null(persoon.InpBsnHashKeyVersion);
    }
}
