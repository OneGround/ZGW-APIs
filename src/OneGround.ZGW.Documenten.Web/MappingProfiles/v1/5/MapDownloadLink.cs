using System.Linq;
using AutoMapper;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;

public class MapDownloadLink : IMappingAction<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectResponseDto>
{
    private readonly IEntityUriService _uriService;

    public MapDownloadLink(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public void Process(EnkelvoudigInformatieObjectVersie src, EnkelvoudigInformatieObjectResponseDto dest, ResolutionContext context)
    {
        dest.Url = _uriService.GetUri(src.InformatieObject);

        if ((string.IsNullOrEmpty(src.Inhoud) && src.Bestandsomvang == 0) || src.BestandsDelen.Count != 0) // Note: New in v1.1
            dest.Inhoud = null;
        else
            dest.Inhoud = _uriService.GetUri(src);
    }
}
