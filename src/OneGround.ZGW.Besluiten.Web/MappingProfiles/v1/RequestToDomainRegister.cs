using Mapster;
using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Besluiten.Contracts.v1.Requests;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Models.v1;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Besluiten.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAllBesluitenQueryParameters, GetAllBesluitenFilter>();

        config
            .NewConfig<BesluitRequestDto, Besluit>()
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            .Map(dest => dest.Datum, src => ProfileHelper.DateFromString(src.Datum))
            .Map(dest => dest.IngangsDatum, src => ProfileHelper.DateFromString(src.IngangsDatum))
            .Map(dest => dest.VervalDatum, src => ProfileHelper.DateFromStringOptional(src.VervalDatum))
            .Map(dest => dest.PublicatieDatum, src => ProfileHelper.DateFromStringOptional(src.PublicatieDatum))
            .Map(dest => dest.UiterlijkeReactieDatum, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeReactieDatum))
            .Map(dest => dest.BesluitType, src => src.BesluitType.TrimEnd('/'))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.BesluitInformatieObjecten)
            .Ignore(dest => dest.ZaakBesluitUrl)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.LegacyAuditTrail);

        config.NewConfig<GetAllBesluitInformatieObjectenQueryParameters, GetAllBesluitInformatieObjectenFilter>();

        config
            .NewConfig<BesluitInformatieObjectRequestDto, BesluitInformatieObject>()
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Besluit)
            .Ignore(dest => dest.BesluitId)
            .Ignore(dest => dest.Registratiedatum)
            .Ignore(dest => dest.AardRelatie)
            .Ignore(dest => dest.Owner);
    }
}
