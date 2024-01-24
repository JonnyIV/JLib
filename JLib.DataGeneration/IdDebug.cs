﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JLib.Helper;
using JLib.ValueTypes;

namespace JLib.DataGeneration;

public record struct IdSnapshotInformation(string IdGroupName, string IdName, object? Value) : IComparable<IdSnapshotInformation>
{
    public readonly int CompareTo(IdSnapshotInformation other) => Value?.ToString()?.CompareTo(other.Value?.ToString()) ?? -1;

    public readonly override string ToString() => $"{IdGroupName}.{IdName} = {Value}";
}
public record struct IdInformation(Type Type, DataPackageValues.IdIdentifier Identifier, object? Value) : IComparable<IdInformation>
{
    public readonly int CompareTo(IdInformation other) => Value?.ToString()?.CompareTo(other.Value?.ToString()) ?? -1;

    public readonly override string ToString() => Type.FullClassName() + " " + Identifier + " = " + Value;

    public readonly IdSnapshotInformation ToSnapshotInfo()
        => new(Identifier.IdGroupName.Value, Identifier.IdName.Value, Value);
}
/// <summary>
/// adds debug methods to resolve the name of an id managed by a <see cref="IIdRegistry"/>
/// </summary>
// ReSharper disable once CheckNamespace
public static class IdDebug
{
    // there might be more than one instance if the tests are executed in parallel. If that's the case, there are no new keys expected.
    private static readonly object InstancesLock = new();
    private static readonly List<IIdRegistry> Instances = new();


    /// <summary>
    /// searches for the given id across all IdRegistry Instances and returns all associated keys
    /// </summary>
    public static string GetIdInfo(object? value, IIdRegistry? idRegistry = null)
    {
        if (value is null)
            return "value is null";

        var nativeValue = value switch
        {
            IntValueType intVt => intVt.Value,
            StringValueType stringVt => stringVt.Value,
            GuidValueType guidVt => guidVt.Value,
            _ => value
        };
        if (idRegistry is not null)
            return GetInfoString(idRegistry.GetIdentifierOfId(value));

        lock (InstancesLock)
        {
            var values = Instances.Select(dict => dict.GetIdentifierOfId(value))
                .WhereNotNull()
                .Select(GetInfoString)
                .Distinct()
                .ToArray();

            return values.Length switch
            {
                0 => "not found",
                1 => values.Single(),
                _ => JsonSerializer.Serialize(values)
            };
        }

        string GetInfoString(DataPackageValues.IdIdentifier? identifier) =>
            $"{value?.GetType().FullClassName()} {identifier} = {nativeValue}";
    }

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/> as structured data
    /// </summary>
    public static IdInformation GetIdInfoObj(object? value, IIdRegistry? idRegistry = null)
    {
        if (value is null)
            return new(typeof(object), new(new("unknown"), new("value is null")), null);

        if (idRegistry is null)
        {
            lock (InstancesLock)
                return Instances.Select(dict => dict.GetIdentifierOfId(value))
                    .WhereNotNull()
                    .Select(identifier => new IdInformation(value.GetType(), identifier, value))
                    .Distinct()
                    .Single();
        }

        var identifier = idRegistry.GetIdentifierOfId(value);
        return identifier is null
            ? new(value.GetType(), new(new("unknown"), new("this id is not registered")), value)
            : new(value.GetType(), identifier, value);
    }

    #region idInfo
    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this Guid? id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this Guid id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this GuidValueType? id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this int? id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this int id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this IntValueType? id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this string? id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string IdInfo(this StringValueType? id, IIdRegistry? idRegistry = null)
        => GetIdInfo(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfo"/>
    /// </summary>
    public static string GuidInfo(this string? id, IIdRegistry? idRegistry = null)
        => id is null
        ? GetIdInfo(null, idRegistry)
        : Guid.TryParse(id, out var guid)
            ? GetIdInfo(guid, idRegistry)
            : throw new FormatException($"'{id}' is not a guid");
    #endregion

    #region idInfoObj
    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this Guid id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this Guid? id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this GuidValueType? id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this int id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this int? id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this IntValueType? id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this string? id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    /// <summary>
    /// <inheritdoc cref="GetIdInfoObj"/>
    /// </summary>
    public static IdInformation IdInfoObj(this StringValueType? id, IIdRegistry? idRegistry = null)
        => GetIdInfoObj(id, idRegistry);

    #endregion

    #region idInfoObj
    /// <summary>
    /// Creates a snapshot optimized object which contains the identifier and value of the given id
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this Guid id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo();

    /// <summary>
    /// <inheritdoc cref="IdSnapshot(Guid,IIdRegistry)"/>
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this Guid? id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo();

    /// <summary>
    /// <inheritdoc cref="IdSnapshot(Guid,IIdRegistry)"/>
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this GuidValueType? id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo();

    /// <summary>
    /// <inheritdoc cref="IdSnapshot(Guid,IIdRegistry)"/>
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this int id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo();

    /// <summary>
    /// <inheritdoc cref="IdSnapshot(Guid,IIdRegistry)"/>
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this int? id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo();

    /// <summary>
    /// <inheritdoc cref="IdSnapshot(Guid,IIdRegistry)"/>
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this IntValueType? id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo();

    /// <summary>
    /// <inheritdoc cref="IdSnapshot(Guid,IIdRegistry)"/>
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this string? id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo();

    /// <summary>
    /// <inheritdoc cref="IdSnapshot(Guid,IIdRegistry)"/>
    /// </summary>
    public static IdSnapshotInformation IdSnapshot(this StringValueType? id, IIdRegistry idRegistry)
        => GetIdInfoObj(id, idRegistry).ToSnapshotInfo()!;

    public static IdSnapshotInformation GuidSnapshot(this string? id, IIdRegistry idRegistry)
        => id is null
            ? GetIdInfoObj(null, idRegistry).ToSnapshotInfo()
            : Guid.TryParse(id, out var guid)
                ? guid.IdSnapshot(idRegistry)
                : new("invalid", "value is not a guid", id);

    #endregion


    internal static void Register(IIdRegistry idRegistry)
    {
        lock (InstancesLock)
            Instances.Add(idRegistry);
    }

    internal static void UnRegister(IIdRegistry idRegistry)
    {
        lock (InstancesLock)
            Instances.Remove(idRegistry);
    }
}