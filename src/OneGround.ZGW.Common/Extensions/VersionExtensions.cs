﻿using System;

namespace OneGround.ZGW.Common.Extensions;

public static class VersionExtensions
{
    public static string ToVersionString(this Version version)
    {
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
