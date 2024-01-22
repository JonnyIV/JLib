﻿using System.Diagnostics.CodeAnalysis;
using JLib.Data;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using JLib.ValueTypes;
using static JLib.Reflection.Attributes.TvtFactoryAttributes;

namespace JLib.DataGeneration;

[IsDerivedFrom(typeof(DataPackage)), NotAbstract]
public record DataPackageType(Type Value) : TypeValueType(Value);

public abstract class DataPackage
{
    public string GetInfoText(string propertyName)
    {
        var property = GetType().GetProperty(propertyName) ??
                                      throw new InvalidSetupException(
                                          $"property {propertyName} not found on {GetType().FullClassName()}");
        var res = $"{property.DeclaringType?.FullClassName()}.{property.Name}";
        if (property.DeclaringType != property.ReflectedType)
            res = $"{property.ReflectedType?.FullClassName()}:" + res;
        return res;
    }

    readonly IDataPackageStore _dataPackages;
    protected DataPackage(IDataPackageStore dataPackages)
    {
        _dataPackages = dataPackages;
        foreach (var propertyInfo in GetType().GetProperties())
        {
            if (!propertyInfo.PropertyType.IsAssignableTo<GuidValueType>())
                continue;
            if (propertyInfo.GetMethod?.IsPublic is not true)
                continue;
            if (propertyInfo.CanWrite is false)
                throw new(propertyInfo.DeclaringType?.FullClassName() + "." + propertyInfo.Name +
                          " can not be written");
            if (propertyInfo.SetMethod?.IsPublic is true)
                throw new(propertyInfo.DeclaringType?.FullClassName() + "." + propertyInfo.Name +
                          " set method must be protected");
            var id = _dataPackages.RetrieveId(propertyInfo);
            propertyInfo.SetValue(this, id);
        }
    }

    protected TEntity AddEntity<TEntity>(TEntity entity)
        where TEntity : IEntity
        => _dataPackages.AddEntities(new[] { entity }).First();
    protected TEntity[] AddEntities<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : IEntity
        => _dataPackages.AddEntities(entities);

    [return: NotNullIfNotNull("id")]
    protected TId? DeriveId<TId>(GuidValueType? id)
        where TId : GuidValueType
        => _dataPackages.DeriveId<TId>(id, GetType());

    protected TId? DeriveId<TId>(GuidValueType? idN, GuidValueType? idM)
        where TId : GuidValueType
        => _dataPackages.DeriveId<TId>(idN, idM, GetType());
}
