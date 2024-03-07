using JLib.Exceptions;
using JLib.Helper;

namespace JLib.DataProvider;

/// <summary>
/// adds implementations for methods which can get their value from <see cref="Get()"/>,
/// assuming that all queryable features are supported
/// </summary>
/// <typeparam name="TDataObject"></typeparam>
public abstract class DataProviderRBase<TDataObject> : IDataProviderR<TDataObject>
    where TDataObject : IDataObject
{
    public abstract IQueryable<TDataObject> Get();

    public TDataObject Get(Guid id)
        => Get().Single(x => x.Id == id);

    /// <summary>
    /// raises a <see cref="KeyNotFoundException"/> when a key could not be found or the user is not authorized to access the given entity;
    /// </summary>
    /// <exception cref="KeyNotFoundException"></exception>
    public IReadOnlyDictionary<Guid, TDataObject> Get(IReadOnlyCollection<Guid> ids)
    {
        var res = Get()
            .Where(x => ids.Contains(x.Id))
            .ToDictionary(x => x.Id);
        ids.Except(res.Keys)
            .Select(id => new KeyNotFoundException(
                "could not find " + typeof(TDataObject).FullName() + ": " + id))
            .ThrowExceptionIfNotEmpty("Some Keys could not be found");
        return res;
    }

    public TDataObject? TryGet(Guid? id)
        => id.HasValue ? Get().SingleOrDefault(x => x.Id == id.Value) : default;

    public bool Contains(Guid? id)
        => id.HasValue && Get().Any(x => x.Id == id.Value);
}