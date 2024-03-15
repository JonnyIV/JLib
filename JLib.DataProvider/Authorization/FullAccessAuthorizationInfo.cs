using System.Linq.Expressions;
using JLib.Reflection;

namespace JLib.DataProvider.Authorization;

/// <summary>
/// Represents the authorization information for full access to a specific data object.
/// </summary>
/// <typeparam name="TDataObject">The type of the data object.</typeparam>
public class FullAccessAuthorizationInfo<TDataObject> : IAuthorizationInfo<TDataObject>
    where TDataObject : class, IDataObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FullAccessAuthorizationInfo{TDataObject}"/> class.
    /// </summary>
    /// <param name="typeCache">The type cache.</param>
    public FullAccessAuthorizationInfo(ITypeCache typeCache)
    {
        Target = typeCache.Get<DataObjectType>(typeof(TDataObject));
    }

    /// <summary>
    /// Gets the target data object type.
    /// </summary>
    public DataObjectType Target { get; }

    /// <summary>
    /// Gets the expression representing the authorization condition.
    /// </summary>
    /// <returns>The expression representing the authorization condition.</returns>
    public Expression<Func<TDataObject, bool>> Expression() => _ => true;

    /// <summary>
    /// Checks if the specified data object is authorized.
    /// </summary>
    /// <param name="dataObject">The data object to check.</param>
    /// <returns><c>true</c> if the data object is authorized; otherwise, <c>false</c>.</returns>
    public bool DataObject(TDataObject dataObject) => true;
}
