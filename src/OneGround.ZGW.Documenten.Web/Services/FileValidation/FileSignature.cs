namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public sealed record FileSignature(byte[] Signature, int Offset = 0)
{
    public int RequiredReadLength { get; } = Offset + Signature.Length;
}
