namespace OneGround.ZGW.Documenten.Messaging.Contracts;

public class DeleteObjectInformatieObjectResult
{
    public DeleteObjectInformatieObjectResult(string objectInformatieObjectUrl)
    {
        ObjectInformatieObjectUrl = objectInformatieObjectUrl;
    }

    public string ObjectInformatieObjectUrl { get; }
}
