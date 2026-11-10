using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Web.Http;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "selectielijstklasse" expand path of a ResultaatType: the document that its selectielijstklasse url refers to.</summary>
public class ResultaatType_SelectielijstKlasse_Resolver(IExternalJsonClient externalJsonClient)
    : ExternalJson_ResolverBase<ResultaatTypeResponseDto>(externalJsonClient)
{
    public override string Path => "selectielijstklasse";

    protected override string GetUrl(ResultaatTypeResponseDto resultaatType) => resultaatType.SelectieLijstKlasse;
}
