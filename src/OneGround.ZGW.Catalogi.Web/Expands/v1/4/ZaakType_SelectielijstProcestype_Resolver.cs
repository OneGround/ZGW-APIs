using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "selectielijstprocestype" expand path of a ZaakType: the document that its selectielijstProcestype url refers to.</summary>
public class ZaakType_SelectielijstProcestype_Resolver(IExternalJsonClient externalJsonClient)
    : ExternalJsonResolver<ZaakTypeResponseDto>(externalJsonClient)
{
    public override string Path => "selectielijstprocestype";

    protected override string GetUrl(ZaakTypeResponseDto zaakType) => zaakType.SelectielijstProcestype;
}
