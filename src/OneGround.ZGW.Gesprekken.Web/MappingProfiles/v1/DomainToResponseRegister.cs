using System.Linq;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Gesprekken.Contracts.v1;
using OneGround.ZGW.Gesprekken.Contracts.v1.Responses;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Helpers;

namespace OneGround.ZGW.Gesprekken.Web.MappingProfiles.v1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Gesprek, GesprekResponseDto>()
            .Map(dest => dest.Uuid, src => src.Id.ToString())
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.AanvangsmomentGesprek, src => ProfileHelper.StringDateFromDateTime(src.AanvangsmomentGesprek, true))
            .Map(dest => dest.MomentEindeGesprek, src => ProfileHelper.StringDateFromDateTime(src.MomentEindeGesprek, true));

        config
            .NewConfig<Gesprek, GesprekInLijstResponseDto>()
            .Inherits<Gesprek, GesprekResponseDto>()
            .Map(dest => dest.LaatsteBijdrage, src => src.Bijdragen.OrderByDescending(b => b.Leveringsmoment).FirstOrDefault());

        config
            .NewConfig<Gespreksbijdrage, LaatsteBijdrageDto>()
            .Map(dest => dest.Uuid, src => src.Id.ToString())
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Verzender, src => GrcUrl.Of(GrcUrl.Gespreksdeelnames, src.VerzenderId))
            .Map(
                dest => dest.TekstFragment,
                src => src.TekstGespreksBijdrage.Length <= 200 ? src.TekstGespreksBijdrage : src.TekstGespreksBijdrage.Substring(0, 200)
            )
            .Map(dest => dest.Leveringsmoment, src => ProfileHelper.StringDateFromDateTime(src.Leveringsmoment, true))
            .Map(dest => dest.AantalBijlagen, src => src.Bijlagen.Count);

        config
            .NewConfig<Gesprek, GesprekDetailResponseDto>()
            .Inherits<Gesprek, GesprekResponseDto>()
            .Map(dest => dest.Gespreksdeelnames, src => src.Deelnames.OrderBy(d => d.AanvangsmomentDeelname))
            .Map(dest => dest.Gespreksbijdragen, src => src.Bijdragen.OrderBy(b => b.Leveringsmoment));

        config
            .NewConfig<Gespreksbijdrage, GespreksbijdrageResponseDto>()
            .Map(dest => dest.Uuid, src => src.Id.ToString())
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Leveringsmoment, src => ProfileHelper.StringDateFromDateTime(src.Leveringsmoment, true))
            .Map(dest => dest.Verzender, src => GrcUrl.Of(GrcUrl.Gespreksdeelnames, src.VerzenderId))
            .Map(dest => dest.Gesprek, src => GrcUrl.Of(GrcUrl.Gesprekken, src.GesprekId))
            .Map(dest => dest.IsReactieOp, src => GrcUrl.Of(GrcUrl.Gespreksbijdragen, src.IsReactieOpId));

        config.NewConfig<Bijlage, BijlageDto>().Map(dest => dest.Bestand, src => GrcUrl.Of(GrcUrl.Bestanden, src.BestandId));

        config
            .NewConfig<Bestand, BestandResponseDto>()
            .Map(dest => dest.FileId, src => src.Id.ToString())
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.AangemaaktOp, src => ProfileHelper.StringDateFromDateTime(src.AangemaaktOp, true))
            .Map(dest => dest.GeuploadOp, src => ProfileHelper.StringDateFromDateTime(src.GeuploadOp, true))
            .Ignore(dest => dest.DownloadUrl);

        config
            .NewConfig<Gespreksdeelname, GespreksdeelnameResponseDto>()
            .Map(dest => dest.Uuid, src => src.Id.ToString())
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Gesprek, src => GrcUrl.Of(GrcUrl.Gesprekken, src.GesprekId))
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.AanvangsmomentDeelname, src => ProfileHelper.StringDateFromDateTime(src.AanvangsmomentDeelname, true))
            .Map(dest => dest.MomentEindeDeelname, src => ProfileHelper.StringDateFromDateTime(src.MomentEindeDeelname, true));

        config
            .NewConfig<GelezenGespreksbijdrage, GelezenGespreksbijdrageResponseDto>()
            .Map(dest => dest.Uuid, src => src.Id.ToString())
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Gespreksdeelname, src => GrcUrl.Of(GrcUrl.Gespreksdeelnames, src.GespreksdeelnameId))
            .Map(dest => dest.Gespreksbijdrage, src => GrcUrl.Of(GrcUrl.Gespreksbijdragen, src.GespreksbijdrageId))
            .Map(dest => dest.MomentGelezen, src => ProfileHelper.StringDateFromDateTime(src.MomentGelezen, true));
    }
}
