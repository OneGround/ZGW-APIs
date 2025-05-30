using System;

namespace Roxit.ZGW.Common.Exceptions;

/// <summary>
/// Wraps any geometry serialization exception to <see cref="InvalidGeometryException"/>.
/// </summary>
public class InvalidGeometryException : Exception
{
    public string PropertyName { get; }

    public InvalidGeometryException(string propertyName, string message)
        : base(message)
    {
        PropertyName = propertyName;
    }
}
