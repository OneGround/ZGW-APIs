using System.Linq;
using AutoMapper;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Contracts.v1._1.Responses;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.MappingProfiles.v1._1;

public class MapDownloadLink : IMappingAction<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectResponseDto>
{
    private readonly IEntityUriService _uriService;

    public MapDownloadLink(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public void Process(EnkelvoudigInformatieObjectVersie src, EnkelvoudigInformatieObjectResponseDto dest, ResolutionContext context)
    {
        if ((string.IsNullOrEmpty(src.Inhoud) && src.Bestandsomvang == 0) || src.BestandsDelen.Count != 0) // Note: New in v1.1
            dest.Inhoud = null;
        else
            dest.Inhoud = _uriService.GetUri(src);
    }
}
