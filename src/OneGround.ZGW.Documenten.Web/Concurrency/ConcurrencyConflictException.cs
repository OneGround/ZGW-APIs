using System;

namespace OneGround.ZGW.Documenten.Web.Concurrency;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Guid id)
        : base(message)
    {
        Id = id;
    }

    public ConcurrencyConflictException(string message, Exception innerException, Guid id)
        : base(message, innerException)
    {
        Id = id;
    }

    public Guid Id { get; private set; }
}
