﻿namespace JLib.Data;


public interface IDataProviderR<TData>
    where TData : IDataObject
{
    public IQueryable<TData> Get();

    public TData Get(Guid id)
        => Get().Single(x => x.Id == id);
    public TData? TryGet(Guid? id)
        => id.HasValue ? Get().SingleOrDefault(x => x.Id == id.Value) : default;
    public bool Contains(Guid? id)
        => id.HasValue && Get().Any(x => x.Id == id.Value);
}

public interface ISourceDataProviderR<TData> : IDataProviderR<TData>
    where TData : IDataObject
{

}

public interface IDataProviderRw<TData> : IDataProviderR<TData>
    where TData : IEntity
{
    public void Add(TData item);
    public void Add(IEnumerable<TData> items);
    public void Remove(Guid itemId);
    public void Remove(IEnumerable<Guid> itemIds);
}

public interface ISourceDataProviderRw<TData> : IDataProviderRw<TData>, ISourceDataProviderR<TData>
    where TData : IEntity
{

}
