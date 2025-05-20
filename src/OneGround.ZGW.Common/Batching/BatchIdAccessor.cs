using System.Threading;

namespace OneGround.ZGW.Common.Batching;

public class BatchIdAccessor : IBatchIdAccessor
{
    private static readonly AsyncLocal<string> _id = new();

    public string Id
    {
        get => _id.Value;
        set => _id.Value = value;
    }
}
