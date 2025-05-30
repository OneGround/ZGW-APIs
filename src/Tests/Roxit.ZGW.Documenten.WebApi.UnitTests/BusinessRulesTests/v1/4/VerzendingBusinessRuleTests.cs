using System;
using System.Collections.Generic;
using System.Linq;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.BusinessRules.v1._5;
using Xunit;

namespace Roxit.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1._4;

public class VerzendingBusinessRuleTests
{
    [Fact]
    public void Add_With_Exactly_One_CorrespondentieAddress_Should_Be_Valid()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending
        {
            AardRelatie = AardRelatie.afzender,
            Ontvangstdatum = new DateOnly(2023, 11, 24),
            CorrespondentiePostadres = new CorrespondentiePostadres { },
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.True(actual);
        Assert.Empty(errors);
    }

    [Fact]
    public void Add_With_Not_Any_CorrespondentieAddress_Should_Be_Invalid()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending { AardRelatie = AardRelatie.afzender };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();
        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "correspondentieAdres");
    }

    [Fact]
    public void Add_With_More_Than_One_CorrespondentieAddress_Should_Be_Invalid()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending
        {
            AardRelatie = AardRelatie.afzender,
            CorrespondentiePostadres = new CorrespondentiePostadres { },
            BinnenlandsCorrespondentieAdres = new BinnenlandsCorrespondentieAdres { },
            BuitenlandsCorrespondentieAdres = new BuitenlandsCorrespondentieAdres { },
            EmailAdres = "somebody@gmail.com",
            Telefoonnummer = "0793614446",
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();
        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "correspondentieAdres");
    }

    [Fact]
    public void Add_With_Afzender_Should_Contain_Ontvangstdatum_And_Empty_Verzenddatum()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending
        {
            AardRelatie = AardRelatie.afzender,
            Ontvangstdatum = new DateOnly(2023, 11, 24),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.True(actual);
        Assert.Empty(errors);
    }

    [Fact]
    public void Add_With_Geadresseerde_Should_Contain_Verzenddatum_And_Empty_Ontvangstdatum()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending
        {
            AardRelatie = AardRelatie.geadresseerde,
            Verzenddatum = new DateOnly(2023, 11, 24),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.True(actual);
        Assert.Empty(errors);
    }

    [Fact]
    public void Add_With_Geadresseerde_With_Ontvangstdatum_And_Verzenddatum_Should_Fail()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending
        {
            AardRelatie = AardRelatie.geadresseerde,
            Ontvangstdatum = new DateOnly(2023, 11, 24),
            Verzenddatum = new DateOnly(2023, 11, 24),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "ontvangstdatum" && e.Code == ErrorCode.MustBeEmpty);
    }

    [Fact]
    public void Add_With_Afzender_With_Ontvangstdatum_And_Verzenddatum_Should_Fail()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending
        {
            AardRelatie = AardRelatie.afzender,
            Ontvangstdatum = new DateOnly(2023, 11, 24),
            Verzenddatum = new DateOnly(2023, 11, 24),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "verzenddatum" && e.Code == ErrorCode.MustBeEmpty);
    }

    [Fact]
    public void Add_With_Geadresseerde_Without_Ontvangstdatum_And_Without_Verzenddatum_Should_Fail()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending { AardRelatie = AardRelatie.geadresseerde, MijnOverheid = true };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "verzenddatum" && e.Code == ErrorCode.Required);
    }

    [Fact]
    public void Add_With_Afzender_Without_Ontvangstdatum_And_Without_Verzenddatum_Should_Fail()
    {
        var informatieobject = new EnkelvoudigInformatieObject();

        var verzending = new Verzending { AardRelatie = AardRelatie.afzender, MijnOverheid = true };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "ontvangstdatum" && e.Code == ErrorCode.Required);
    }

    [Fact]
    public void Add_With_An_Already_Existing_Afzender_Should_Fail()
    {
        var informatieobject = new EnkelvoudigInformatieObject
        {
            Id = new Guid("ad58455f-4d10-4fe3-8b78-f76d959b77b1"),
            Verzendingen =
            [
                new Verzending
                {
                    Id = new Guid("bfcd643e-4ec6-4e48-8672-022e677d16cf"),
                    AardRelatie = AardRelatie.afzender,
                    Ontvangstdatum = new DateOnly(2022, 11, 28),
                    InformatieObjectId = new Guid("85a5f9ce-93e1-4e0f-8767-2aea4942367f"),
                },
            ],
        };

        var verzending = new Verzending
        {
            Id = new Guid("ae6d4f60-0983-4718-a731-fb4a5f6a6ac3"),
            InformatieObject = informatieobject,
            InformatieObjectId = informatieobject.Id,
            AardRelatie = AardRelatie.afzender,
            Ontvangstdatum = new DateOnly(2022, 11, 28),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "nonFieldErrors" && e.Code == ErrorCode.Invalid);
    }

    [Fact]
    public void Add_With_An_Already_Existing_Geadresseerde_Should_Be_valid()
    {
        var informatieobject = new EnkelvoudigInformatieObject
        {
            Id = new Guid("ad58455f-4d10-4fe3-8b78-f76d959b77b1"),
            Verzendingen =
            [
                new Verzending
                {
                    Id = new Guid("bfcd643e-4ec6-4e48-8672-022e677d16cf"),
                    AardRelatie = AardRelatie.geadresseerde,
                    Verzenddatum = new DateOnly(2022, 11, 28),
                    InformatieObjectId = new Guid("85a5f9ce-93e1-4e0f-8767-2aea4942367f"),
                },
            ],
        };

        var verzending = new Verzending
        {
            Id = new Guid("ae6d4f60-0983-4718-a731-fb4a5f6a6ac3"),
            InformatieObject = informatieobject,
            InformatieObjectId = informatieobject.Id,
            AardRelatie = AardRelatie.geadresseerde,
            Verzenddatum = new DateOnly(2022, 11, 28),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, verzending, errors);

        Assert.True(actual);
        Assert.Empty(errors);
    }

    [Fact]
    public void Update_With_An_Already_Existing_Afzender_With_The_Same_InformatieObject_Should_Fail()
    {
        var informatieObjectId = new Guid("ad58455f-4d10-4fe3-8b78-f76d959b77b1");
        var informatieobject = new EnkelvoudigInformatieObject
        {
            Id = informatieObjectId,
            Verzendingen =
            [
                new Verzending
                {
                    Id = new Guid("ae6d4f60-0983-4718-a731-fb4a5f6a6ac3"),
                    InformatieObjectId = informatieObjectId,
                    AardRelatie = AardRelatie.afzender,
                },
                new Verzending
                {
                    Id = new Guid("8fe0f9df-466e-4a5e-9e70-9a1d3234f0e4"),
                    InformatieObjectId = informatieObjectId,
                    AardRelatie = AardRelatie.afzender,
                },
            ],
        };

        var existingVerzendingId = informatieobject.Verzendingen.Last().Id;

        var verzending = new Verzending
        {
            Id = existingVerzendingId,
            InformatieObject = informatieobject,
            InformatieObjectId = informatieobject.Id,
            AardRelatie = AardRelatie.afzender,
            Ontvangstdatum = new DateOnly(2022, 11, 28),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(informatieobject, existingVerzending: verzending, existingVerzendingId, errors);

        Assert.False(actual);
        Assert.Contains(errors, e => e.Name == "nonFieldErrors" && e.Code == ErrorCode.Invalid);
    }

    [Fact]
    public void Update_With_An_Already_Existing_Afzender_With_Another_InformatieObject_Should_Be_Valid()
    {
        var informatieObjectId = new Guid("ad58455f-4d10-4fe3-8b78-f76d959b77b1");
        var informatieobject = new EnkelvoudigInformatieObject
        {
            Id = informatieObjectId,
            Verzendingen =
            [
                new Verzending
                {
                    Id = new Guid("ae6d4f60-0983-4718-a731-fb4a5f6a6ac3"),
                    InformatieObjectId = informatieObjectId,
                    AardRelatie = AardRelatie.afzender,
                    Ontvangstdatum = new DateOnly(2022, 11, 28),
                },
                new Verzending
                {
                    Id = new Guid("8fe0f9df-466e-4a5e-9e70-9a1d3234f0e4"),
                    InformatieObjectId = informatieObjectId,
                    AardRelatie = AardRelatie.afzender,
                    Ontvangstdatum = new DateOnly(2022, 11, 28),
                },
            ],
        };

        var existingVerzendingId = informatieobject.Verzendingen.Last().Id;

        var anotherInformatieObjectId = new Guid("9a1291b4-0e65-4666-ac1d-a89eaf847111");
        var anotherInformatieobject = new EnkelvoudigInformatieObject
        {
            Id = anotherInformatieObjectId,
            Verzendingen =
            [
                new Verzending
                {
                    Id = new Guid("cb54e38b-3c6e-4d83-8208-58c2123e383a"),
                    InformatieObjectId = anotherInformatieObjectId,
                    AardRelatie = AardRelatie.geadresseerde,
                    Ontvangstdatum = new DateOnly(2022, 11, 28),
                },
            ],
        };

        var verzending = new Verzending
        {
            Id = existingVerzendingId,
            InformatieObject = anotherInformatieobject,
            InformatieObjectId = anotherInformatieObjectId,
            AardRelatie = AardRelatie.afzender,
            Ontvangstdatum = new DateOnly(2022, 11, 28),
            MijnOverheid = true,
        };

        var svc = new VerzendingBusinessRuleService();

        var errors = new List<ValidationError>();

        var actual = svc.Validate(anotherInformatieobject, existingVerzending: verzending, existingVerzendingId, errors);

        Assert.True(actual);
        Assert.Empty(errors);
    }
}
