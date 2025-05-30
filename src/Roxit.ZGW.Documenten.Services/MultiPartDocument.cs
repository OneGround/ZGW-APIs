namespace Roxit.ZGW.Documenten.Services;

public class MultiPartDocument : IMultiPartDocument
{
    public MultiPartDocument(string context)
    {
        Context = context;
    }

    public string Context { get; set; }
}
