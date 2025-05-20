namespace OneGround.ZGW.Documenten.Services;

public interface IDocumentServicesResolver
{
    IDocumentService GetDefault();
    IDocumentService Find(string providerPrefix);
}
