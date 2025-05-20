using System;
using Asp.Versioning;

namespace OneGround.ZGW.Common.Web.Versioning;

// Source: https://github.com/dotnet/aspnet-api-versioning/wiki/Custom-API-Version-Format
public class ZgwApiVersionParser : ApiVersionParser
{
    public override ApiVersion Parse(ReadOnlySpan<char> text)
    {
        if (!TryParse(text, out var version))
        {
            return null!; // Note: This wil be handled correctly
        }
        return version;
    }

    public override bool TryParse(ReadOnlySpan<char> text, out ApiVersion apiVersion)
    {
        apiVersion = null!;

        var splitted = text.ToString().Split('.');
        if (splitted.Length > 3)
        {
            return false;
        }

        var major = 0;
        var minor = 0;
        var patch = 0;

        if (splitted.Length >= 1)
        {
            major = int.Parse(splitted[0]);
        }
        if (splitted.Length >= 2)
        {
            minor = int.Parse(splitted[1]);
        }
        if (splitted.Length == 3)
        {
            if (!int.TryParse(splitted[2], out patch))
            {
                return false;
            }
        }

        apiVersion = new ZgwApiVersion(major, minor, patch);
        return true;
    }
}
