namespace OneGround.ZGW.Documenten.Services;

public class UploadPart : IUploadPart
{
    public UploadPart(string context)
    {
        Context = context;
    }

    public string Context { get; set; }
}
