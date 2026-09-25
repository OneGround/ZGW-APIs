using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Shared with v1.1, which reuses these base query parameters (it has its own request/response bodies).
        config.NewConfig<GetAllEnkelvoudigInformatieObjectenQueryParameters, GetAllEnkelvoudigInformatieObjectenFilter>();

        config.NewConfig<GetAllObjectInformatieObjectenQueryParameters, GetAllObjectInformatieObjectenFilter>();

        config
            .NewConfig<ObjectInformatieObjectRequestDto, ObjectInformatieObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.InformatieObject)
            .Ignore(dest => dest.InformatieObjectId)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.RowVersion);

        config
            .NewConfig<GetAllGebruiksRechtenQueryParameters, GetAllGebruiksRechtenFilter>()
            .Map(dest => dest.Startdatum__gt, src => ProfileHelper.DateTimeFromString(src.Startdatum__gt))
            .Map(dest => dest.Startdatum__gte, src => ProfileHelper.DateTimeFromString(src.Startdatum__gte))
            .Map(dest => dest.Startdatum__lt, src => ProfileHelper.DateTimeFromString(src.Startdatum__lt))
            .Map(dest => dest.Startdatum__lte, src => ProfileHelper.DateTimeFromString(src.Startdatum__lte))
            .Map(dest => dest.Einddatum__gt, src => ProfileHelper.DateTimeFromString(src.Einddatum__gt))
            .Map(dest => dest.Einddatum__gte, src => ProfileHelper.DateTimeFromString(src.Einddatum__gte))
            .Map(dest => dest.Einddatum__lt, src => ProfileHelper.DateTimeFromString(src.Einddatum__lt))
            .Map(dest => dest.Einddatum__lte, src => ProfileHelper.DateTimeFromString(src.Einddatum__lte));

        config
            .NewConfig<GebruiksRechtRequestDto, GebruiksRecht>()
            .Map(dest => dest.Startdatum, src => ProfileHelper.DateTimeFromString(src.Startdatum))
            .Map(dest => dest.Einddatum, src => ProfileHelper.DateTimeFromString(src.Einddatum))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.InformatieObject)
            .Ignore(dest => dest.InformatieObjectId)
            .Ignore(dest => dest.RowVersion);
    }
}
