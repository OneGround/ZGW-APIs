using System;
using Asp.Versioning;

namespace OneGround.ZGW.Common.Web.Versioning;

public class ZgwApiVersion : ApiVersion
{
    private readonly int _patchVersion;

    public ZgwApiVersion(DateOnly groupVersion, string status = null)
        : base(groupVersion, status)
    {
        _patchVersion = 0;
    }

    public ZgwApiVersion(int majorVersion, int minorVersion, int patchVersion)
        : base(majorVersion, minorVersion, status: null)
    {
        _patchVersion = patchVersion;
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override int CompareTo(ApiVersion other)
    {
        if (other == null || other is not ZgwApiVersion)
        {
            return base.CompareTo(other);
        }

        var zgwOther = other as ZgwApiVersion;

        int majorComparison = MajorVersion.GetValueOrDefault().CompareTo(zgwOther.MajorVersion.GetValueOrDefault());
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        int minorComparison = MinorVersion.GetValueOrDefault().CompareTo(zgwOther.MinorVersion.GetValueOrDefault());
        if (minorComparison != 0)
        {
            return minorComparison;
        }

        return _patchVersion.CompareTo(zgwOther._patchVersion);
    }

    public override string ToString()
    {
        return $"{MajorVersion}.{MinorVersion}.{_patchVersion}";
    }

    public override string ToString(string s, IFormatProvider provider)
    {
        return ToString();
    }
}
