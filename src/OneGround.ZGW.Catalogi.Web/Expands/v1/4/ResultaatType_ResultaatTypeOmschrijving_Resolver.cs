using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "resultaattypeomschrijving" expand path of a ResultaatType: the document that its resultaattypeomschrijving url refers to.</summary>
public class ResultaatType_ResultaatTypeOmschrijving_Resolver(IExternalJsonClient externalJsonClient)
    : ExternalJsonResolver<ResultaatTypeResponseDto>(externalJsonClient)
{
    public override string Path => "resultaattypeomschrijving";

    protected override string GetUrl(ResultaatTypeResponseDto resultaatType) => resultaatType.ResultaatTypeOmschrijving;
}
