using System.Collections.Generic;

namespace OneGround.ZGW.Common.Contracts;

public interface IExpandable
{
    Dictionary<string, object> Expand { get; set; }
}
