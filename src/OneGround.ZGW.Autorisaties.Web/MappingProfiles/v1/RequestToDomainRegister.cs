using System.Linq;
using Mapster;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Contracts.v1.Requests.Queries;
using OneGround.ZGW.Autorisaties.Web.Models;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // The nullable-enum property this profile maps into (Autorisatie.MaxVertrouwelijkheidaanduiding,
        // type VertrouwelijkheidAanduiding?) lives in the *.DataModel assembly, which AddZgwMapster does
        // not scan by default. Register the string->Nullable<enum> rules for this service's entities here
        // so "" / unknown map to null (parity with the AutoMapper NullableEnumMapper).
        config.RegisterNullableEnumRules(new[] { typeof(Autorisatie).Assembly });

        config
            .NewConfig<GetAllApplicatiesQueryParameters, GetAllApplicatiesFilter>()
            .Map(dest => dest.ClientIds, src => ProfileHelper.ArrayFromString(src.ClientIds));

        config
            .NewConfig<ApplicatieRequestDto, Applicatie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.FutureAutorisaties)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.ClientIds, src => src.ClientIds.Select(client => new ApplicatieClient { ClientId = client }));

        config
            .NewConfig<AutorisatieRequestDto, Autorisatie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Applicatie)
            .Ignore(dest => dest.ApplicatieId)
            .Ignore(dest => dest.Owner);
    }
}
