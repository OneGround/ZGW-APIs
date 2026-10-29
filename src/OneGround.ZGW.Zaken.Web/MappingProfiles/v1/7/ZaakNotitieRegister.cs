using System;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1._7;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._7;

public class ZaakNotitieRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Note: Query-parameters to the internal filter model (dates in UTC; the enum values are validated before)
        config
            .NewConfig<GetAllZaakNotitiesQueryParameters, GetAllZaakNotitiesFilter>()
            .Map(dest => dest.Aanmaakdatum, src => DateTimeWindow.FromString(src.Aanmaakdatum))
            .Map(dest => dest.Aanmaakdatum__gt, src => ProfileHelper.DateTimeFromString(src.Aanmaakdatum__gt))
            .Map(dest => dest.Aanmaakdatum__gte, src => ProfileHelper.DateTimeFromString(src.Aanmaakdatum__gte))
            .Map(dest => dest.Aanmaakdatum__lt, src => ProfileHelper.DateTimeFromString(src.Aanmaakdatum__lt))
            .Map(dest => dest.Aanmaakdatum__lte, src => ProfileHelper.DateTimeFromString(src.Aanmaakdatum__lte))
            .Map(dest => dest.Wijzigingsdatum, src => DateTimeWindow.FromString(src.Wijzigingsdatum))
            .Map(dest => dest.Wijzigingsdatum__gt, src => ProfileHelper.DateTimeFromString(src.Wijzigingsdatum__gt))
            .Map(dest => dest.Wijzigingsdatum__gte, src => ProfileHelper.DateTimeFromString(src.Wijzigingsdatum__gte))
            .Map(dest => dest.Wijzigingsdatum__lt, src => ProfileHelper.DateTimeFromString(src.Wijzigingsdatum__lt))
            .Map(dest => dest.Wijzigingsdatum__lte, src => ProfileHelper.DateTimeFromString(src.Wijzigingsdatum__lte))
            .Map(
                dest => dest.NotitieType,
                src => string.IsNullOrEmpty(src.NotitieType) ? (ZaakNotitieType?)null : Enum.Parse<ZaakNotitieType>(src.NotitieType)
            )
            .Map(dest => dest.Status, src => string.IsNullOrEmpty(src.Status) ? (ZaakNotitieStatus?)null : Enum.Parse<ZaakNotitieStatus>(src.Status));

        // Note: notitieType and status are optional: a new notitie is an intern concept
        config
            .NewConfig<ZaakNotitieRequestDto, ZaakNotitie>()
            .Map(
                dest => dest.NotitieType,
                src => string.IsNullOrEmpty(src.NotitieType) ? ZaakNotitieType.intern : Enum.Parse<ZaakNotitieType>(src.NotitieType)
            )
            .Map(dest => dest.Status, src => string.IsNullOrEmpty(src.Status) ? ZaakNotitieStatus.concept : Enum.Parse<ZaakNotitieStatus>(src.Status))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        config
            .NewConfig<ZaakNotitie, ZaakNotitieResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Aanmaakdatum, src => src.CreationTime)
            .Map(dest => dest.Wijzigingsdatum, src => src.ModificationTime ?? src.CreationTime)
            .Map(dest => dest.GerelateerdAan, src => MapsterUrlResolver.ResolveUrl(src.Zaak));

        // Note: This map is used to merge an existing ZAAKNOTITIE with the PATCH operation
        config.NewConfig<ZaakNotitie, ZaakNotitieRequestDto>().Map(dest => dest.GerelateerdAan, src => MapsterUrlResolver.ResolveUrl(src.Zaak));
    }
}
