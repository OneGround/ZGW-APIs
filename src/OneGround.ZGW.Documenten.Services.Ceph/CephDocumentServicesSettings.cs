namespace OneGround.ZGW.Documenten.Services.Ceph;

public class CephDocumentServicesSettings
{
    public string Endpoint { get; set; }
    public bool Ssl { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Bucket { get; set; }
}
