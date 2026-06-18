namespace AnatomiQ.Core
{
    /// <summary>
    /// Marker base interface for any cross-cutting service that registers itself with the
    /// <see cref="ServiceRegistry"/> at runtime. Lets the registry hold a common type and
    /// lets tests supply mock implementations.
    /// </summary>
    public interface IService
    {
    }
}
