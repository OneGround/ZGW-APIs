using System.Collections.Generic;

namespace OneGround.ZGW.Common.Web.Expands;

public interface IExpandable
{
    Dictionary<string, object?>? Expand { get; set; }
}
