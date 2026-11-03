using Mapster;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._4;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<GetAllCatalogussenQueryParameters, Models.v1.GetAllCatalogussenFilter>()
            .Map(dest => dest.Domein__in, src => ProfileHelper.ArrayFromString(src.Domein__in))
            .Map(dest => dest.Rsin__in, src => ProfileHelper.ArrayFromString(src.Rsin__in));

        config
            .NewConfig<GetAllZaakTypenQueryParameters, Models.v1.GetAllZaakTypenFilter>()
            .Map(dest => dest.Trefwoorden, src => ProfileHelper.ArrayFromString(src.Trefwoorden))
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<GetAllInformatieObjectTypenQueryParameters, Models.v1.GetAllInformatieObjectTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<GetAllBesluitTypenQueryParameters, Models.v1._3.GetAllBesluitTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<GetAllEigenschappenQueryParameters, Models.v1._3.GetAllEigenschappenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<GetAllResultaatTypenQueryParameters, Models.v1._3.GetAllResultaatTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<GetAllRolTypenQueryParameters, Models.v1._3.GetAllRolTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<GetAllStatusTypenQueryParameters, Models.v1._3.GetAllStatusTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));
    }
}
