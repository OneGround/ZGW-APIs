using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests;

public class MockMemoryCache : IMemoryCache
{
    private readonly Mock<ICacheEntry> _cacheEntry = new Mock<ICacheEntry>();

    public ICacheEntry CreateEntry(object key)
    {
        return _cacheEntry.Object;
    }

    public void Dispose() { }

    public void Remove(object key) { }

    public bool TryGetValue(object key, out object value)
    {
        value = null;
        return false;
    }
}
