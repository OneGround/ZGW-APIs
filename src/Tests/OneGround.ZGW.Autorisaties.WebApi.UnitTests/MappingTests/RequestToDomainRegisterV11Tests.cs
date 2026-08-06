using System;
using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1._1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using Xunit;
using AutorisatieRequestDtoV1 = OneGround.ZGW.Autorisaties.Contracts.v1.Requests.AutorisatieRequestDto;
using RequestToDomainRegisterV1 = OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1.RequestToDomainRegister;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

/// <summary>
/// The v1.1 counterpart of <see cref="RequestToDomainProfileTests"/>. v1.1's APPLICATIE request carries
/// <c>AlleenIsGereedVoorPublicatie</c> and reuses v1's AUTORISATIE request DTO, so v1's register is added
/// to the same config — mirroring the single scanned config the real seam builds.
/// </summary>
public class RequestToDomainRegisterV11Tests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly IMapper _mapper;

    public RequestToDomainRegisterV11Tests()
    {
        var config = new TypeAdapterConfig();
        // The real seam (AddZgwMapster) registers the global nullable-enum rule once, before any
        // IRegister.Register runs. This test builds its config directly from the registers, so the rule
        // must be added explicitly here too.
        config.RegisterNullableEnumRule();
        new RequestToDomainRegisterV1().Register(config);
        new RequestToDomainRegister().Register(config);
        config.Compile();
        _mapper = new Mapper(config);
    }

    [Fact]
    public void ApplicatieRequestDto_Maps_To_Applicatie()
    {
        _fixture.Customize<ApplicatieRequestDto>(c =>
            c.With(p => p.ClientIds, ["id1, id2"]).With(p => p.AlleenIsGereedVoorPublicatie, true).Without(p => p.Autorisaties)
        );

        var value = _fixture.Create<ApplicatieRequestDto>();
        var result = _mapper.Map<Applicatie>(value);

        Assert.True(result.ClientIds.All(c => value.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        // The field v1.1 adds — the whole reason this contract version exists.
        Assert.True(result.AlleenIsGereedVoorPublicatie);
        Assert.Equal(value.Label, result.Label);
        // The following must remain default (Ignored) — never populated from the request:
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.CreatedBy);
        Assert.Null(result.Owner);
    }

    [Fact]
    public void ApplicatieRequestDto_Maps_Nested_Autorisaties()
    {
        _fixture.Customize<AutorisatieRequestDtoV1>(c =>
            c.With(p => p.Component, Component.zrc.ToString())
                .With(p => p.MaxVertrouwelijkheidaanduiding, VertrouwelijkheidAanduiding.geheim.ToString())
        );
        _fixture.Customize<ApplicatieRequestDto>(c => c.With(p => p.ClientIds, ["id1"]));

        var value = _fixture.Create<ApplicatieRequestDto>();
        var result = _mapper.Map<Applicatie>(value);

        Assert.NotEmpty(value.Autorisaties);
        Assert.Equal(value.Autorisaties.Count, result.Autorisaties.Count);
        // v1.1 reuses v1's AutorisatieRequestDto, so the nested string->enum conversions can only work if
        // convention-based nested mapping picked up v1's AutorisatieRequestDto -> Autorisatie rule.
        Assert.Equal(Component.zrc, result.Autorisaties[0].Component);
        Assert.Equal(VertrouwelijkheidAanduiding.geheim, result.Autorisaties[0].MaxVertrouwelijkheidaanduiding);
        // Owner is Ignored on the nested map too — handlers set it from the authenticated context.
        Assert.Null(result.Autorisaties[0].Owner);
    }
}
