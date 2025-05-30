namespace Roxit.ZGW.Documenten.Messaging.Contracts;

public class AddObjectInformatieObjectResult
{
    public AddObjectInformatieObjectResult(string objectInformatieObjectUrl)
    {
        ObjectInformatieObjectUrl = objectInformatieObjectUrl;
    }

    public string ObjectInformatieObjectUrl { get; }
}
