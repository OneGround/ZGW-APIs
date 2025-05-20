using System;

namespace OneGround.ZGW.Documenten.Web.Services;

public class LockGenerator : ILockGenerator
{
    public string Generate()
    {
        var result = Guid.NewGuid().ToString().Replace("-", "");

        return result;
    }
}
