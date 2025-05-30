using System;

namespace Roxit.ZGW.Common.Exceptions;

public class ApiJsonParsingException : Exception
{
    public ApiJsonParsingException(string message, string propertyName)
        : base(message)
    {
        PropertyName = propertyName;
    }

    public ApiJsonParsingException(string message, string propertyName, Exception innerException)
        : base(message, innerException)
    {
        PropertyName = propertyName;
    }

    public string PropertyName { get; set; }
}
