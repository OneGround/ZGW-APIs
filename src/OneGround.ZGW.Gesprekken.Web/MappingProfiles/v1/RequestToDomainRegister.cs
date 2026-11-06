using System;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Gesprekken.Contracts.v1;
using OneGround.ZGW.Gesprekken.Contracts.v1.Queries;
using OneGround.ZGW.Gesprekken.Contracts.v1.Requests;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Helpers;
using OneGround.ZGW.Gesprekken.Web.Models.v1;

namespace OneGround.ZGW.Gesprekken.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<GetAllGesprekkenQueryParameters, GetAllGesprekkenFilter>()
            .Map(dest => dest.AanvangsmomentGesprekGte, src => ProfileHelper.DateTimeFromString(src.AanvangsmomentGesprekGte))
            .Map(dest => dest.AanvangsmomentGesprekLte, src => ProfileHelper.DateTimeFromString(src.AanvangsmomentGesprekLte))
            .Map(dest => dest.GespreksdeelnamePartij, src => TrimUrl(src.GespreksdeelnamePartij))
            .Map(dest => dest.GespreksdeelnameActor, src => TrimUrl(src.GespreksdeelnameActor))
            .Map(dest => dest.AanleidingZaak, src => TrimUrl(src.AanleidingZaak))
            .Map(dest => dest.AanleidingPlan, src => TrimUrl(src.AanleidingPlan))
            .Map(dest => dest.Gespreksdeelname, src => GrcUrl.GetIdOrNull(src.Gespreksdeelname, GrcUrl.Gespreksdeelnames))
            .Map(
                dest => dest.GespreksdeelnameType,
                src => src.GespreksdeelnameType == null ? (DeelnameType?)null : Enum.Parse<DeelnameType>(src.GespreksdeelnameType)
            );

        config
            .NewConfig<GesprekRequestDto, Gesprek>()
            .Map(dest => dest.AanleidingZaak, src => TrimUrl(src.AanleidingZaak))
            .Map(dest => dest.AanleidingThema, src => TrimUrl(src.AanleidingThema))
            .Map(dest => dest.AanleidingProductDienst, src => TrimUrl(src.AanleidingProductDienst))
            .Map(dest => dest.AanleidingAgendaAfspraak, src => TrimUrl(src.AanleidingAgendaAfspraak))
            .Map(dest => dest.AanleidingPlan, src => TrimUrl(src.AanleidingPlan))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Referentie)
            .Ignore(dest => dest.AanvangsmomentGesprek)
            .Ignore(dest => dest.MomentEindeGesprek)
            .Ignore(dest => dest.Contactmoment)
            .Ignore(dest => dest.Deelnames)
            .Ignore(dest => dest.Bijdragen)
            .IgnoreAuditFields();

        config
            .NewConfig<DeelnameRequestDto, Gespreksdeelname>()
            .Map(dest => dest.Type, src => Enum.Parse<DeelnameType>(src.Type))
            .Map(dest => dest.Partij, src => TrimUrl(src.Partij))
            .Map(dest => dest.Actor, src => TrimUrl(src.Actor))
            .Map(dest => dest.Namens, src => TrimUrl(src.Namens))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.GesprekId)
            .Ignore(dest => dest.Gesprek)
            .Ignore(dest => dest.AanvangsmomentDeelname)
            .Ignore(dest => dest.MomentEindeDeelname)
            .IgnoreAuditFields();

        config
            .NewConfig<GespreksdeelnameRequestDto, Gespreksdeelname>()
            .Inherits<DeelnameRequestDto, Gespreksdeelname>()
            .Map(dest => dest.GesprekId, src => GrcUrl.GetIdOrNull(src.Gesprek, GrcUrl.Gesprekken) ?? Guid.Empty);

        config
            .NewConfig<EersteGespreksbijdrageRequestDto, Gespreksbijdrage>()
            .Map(dest => dest.Bijlagen, src => src.Bijlagen)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.GesprekId)
            .Ignore(dest => dest.Gesprek)
            .Ignore(dest => dest.VerzenderId)
            .Ignore(dest => dest.Verzender)
            .Ignore(dest => dest.IsReactieOpId)
            .Ignore(dest => dest.Automatisch)
            .Ignore(dest => dest.Leveringsmoment)
            .IgnoreAuditFields();

        config
            .NewConfig<GespreksbijdrageRequestDto, Gespreksbijdrage>()
            .Map(dest => dest.GesprekId, src => GrcUrl.GetIdOrNull(src.Gesprek, GrcUrl.Gesprekken) ?? Guid.Empty)
            .Map(dest => dest.IsReactieOpId, src => GrcUrl.GetIdOrNull(src.IsReactieOp, GrcUrl.Gespreksbijdragen))
            .Map(dest => dest.Bijlagen, src => src.Bijlagen)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Gesprek)
            .Ignore(dest => dest.VerzenderId)
            .Ignore(dest => dest.Verzender)
            .Ignore(dest => dest.Automatisch)
            .Ignore(dest => dest.Leveringsmoment)
            .IgnoreAuditFields();

        config
            .NewConfig<BijlageDto, Bijlage>()
            .Map(dest => dest.BestandId, src => GrcUrl.GetIdOrNull(src.Bestand, GrcUrl.Bestanden) ?? Guid.Empty)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.GespreksbijdrageId)
            .Ignore(dest => dest.Titel)
            .Ignore(dest => dest.Bestandsnaam)
            .Ignore(dest => dest.ContentType)
            .Ignore(dest => dest.Bestandsomvang);

        config
            .NewConfig<UploadSessieRequestDto, Bestand>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Status)
            .Ignore(dest => dest.Sleutel)
            .Ignore(dest => dest.AangemaaktOp)
            .Ignore(dest => dest.VerlooptOp)
            .Ignore(dest => dest.GeuploadOp)
            .IgnoreAuditFields();
    }

    private static string TrimUrl(string value) => string.IsNullOrEmpty(value) ? null : value.TrimEnd('/');
}

static class AuditFieldsMappingExtensions
{
    public static TypeAdapterSetter<TSource, TDestination> IgnoreAuditFields<TSource, TDestination>(
        this TypeAdapterSetter<TSource, TDestination> setter
    )
        where TDestination : AuditableEntity =>
        setter.Ignore(dest => dest.CreationTime).Ignore(dest => dest.ModificationTime).Ignore(dest => dest.CreatedBy).Ignore(dest => dest.ModifiedBy);
}
