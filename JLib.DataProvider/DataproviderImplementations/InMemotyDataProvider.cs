using System.Collections.Concurrent;
using System.Reflection;
using JLib.DataProvider.Authorization;
using JLib.Exceptions;
using JLib.Helper;
using JLib.ValueTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JLib.DataProvider;

/// <summary>
/// hold all data in memory and does not save it to anywhere. should be primarily used for testing only.<br/>
/// should be provided as singleton
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class InMemoryDataProvider<TEntity> : DataProviderRBase<TEntity>, ISourceDataProviderRw<TEntity>
    where TEntity : class, IEntity
{
    private readonly ILogger<InMemoryDataProvider<TEntity>> _logger;
    private readonly PropertyInfo _idProperty;

    private readonly Func<Guid, object> _idGenerator;

    public InMemoryDataProvider(IServiceProvider serviceProvider, ILogger<InMemoryDataProvider<TEntity>> logger)
    {
        _logger = logger;
        _authorize = serviceProvider.GetService<IAuthorizationInfo<TEntity>>();
        // reflection to support vt ids
        _idProperty = typeof(TEntity)
            .GetProperties()
            .Single(x => x.Name == nameof(IEntity.Id));
        if (_idProperty.PropertyType.IsAssignableTo<GuidValueType>())
        {
            var idPropertyVtCtor = _idProperty.PropertyType.GetConstructor(new[] { typeof(Guid) }) ??
                                   throw new InvalidSetupException("vtId ctor not found");
            _idGenerator = id => idPropertyVtCtor.Invoke(new object[] { id }).CastTo<GuidValueType>();
        }
        else
            _idGenerator = id => id;

        logger.LogTrace("creating {type}", GetType().FullName());
    }

    private void CreateAndSetId(TEntity entity)
    {
        if (_idProperty.GetValue(entity) is not null)
            return;
        _idProperty.SetValue(entity, _idGenerator?.Invoke(Guid.NewGuid()));
    }

    private readonly ConcurrentDictionary<Guid, TEntity> _items = new();
    private readonly IAuthorizationInfo<TEntity>? _authorize;

    public override IQueryable<TEntity> Get()
    {
        _logger.LogTrace("InMemoryDataProvider Expression for {0}", typeof(TEntity).Name);
        return _authorize is null
         ? _items.Values.AsQueryable()
         : _items.Values.AsQueryable().Where(_authorize.Expression());
    }

    public void Add(TEntity item)
    {
        _authorize?.AndRaiseException(item);
        CreateAndSetId(item);
        if (!_items.TryAdd(item.Id, item))
            throw new InvalidOperationException("item could not be added");
    }

    public void Add(IReadOnlyCollection<TEntity> items)
    {
        foreach (var entity in items)
            Add(entity);
    }

    public void Remove(Guid itemId)
    {
        if (!_items.TryRemove(itemId, out _))
            throw new Exception("item could not be removed");
    }

    public void Remove(TEntity item) => Remove(item.Id);

    public void Remove(IReadOnlyCollection<Guid> itemIds)
    {
        foreach (var id in itemIds)
            Remove(id);
    }

    public void Remove(IReadOnlyCollection<TEntity> items)
    {
        foreach (var item in items)
            Remove(item);
    }
}