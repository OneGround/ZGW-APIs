using System;

namespace OneGround.ZGW.Common.Exceptions;

public class OneGroundException : Exception
{
    public OneGroundException() { }

    public OneGroundException(string message)
        : base(message) { }

    public OneGroundException(string message, Exception exception)
        : base(message, exception) { }
}
