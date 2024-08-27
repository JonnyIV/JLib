﻿using System.Linq.Expressions;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using static JLib.Reflection.TvtFactoryAttribute;

namespace JLib.DataProvider.Authorization;

/// <summary>
/// Represents a non-abstract type that is derived from <see cref="AuthorizationProfile"/>
/// </summary>
/// <param name="Value"></param>
[NotAbstract, IsDerivedFrom(typeof(AuthorizationProfile))]
public record AuthorizationProfileType(Type Value) : TypeValueType(Value), IPostNavigationInitializedType
{
    /// <summary>
    /// the singleton instance of this <see cref="Value"/>
    /// </summary>
    public AuthorizationProfile Instance { get; private set; } = null!;

    /// <summary>
    /// Internal use only. Initializes the instance of the AuthorizationProfile
    /// </summary>
    public void Initialize(ITypeCache cache, ExceptionBuilder exceptions)
    {
        Instance = Activator.CreateInstance(Value, new object[] { cache })?.As<AuthorizationProfile>()
                   ?? throw new("instance could not be created");
    }
}

/// <summary>
/// can be derived from to add unboundAuthorization to an entity
/// <br/> the resulting unboundAuthorization can be applied using the <see cref="IAuthorizationManager"/>
/// </summary>
public abstract class AuthorizationProfile
{
    private readonly ITypeCache _typeCache;
    private readonly List<IUnboundAuthorizationInfo> _authorizationProvider = new();

    /// <summary>
    /// the types that are authorized by this profile
    /// </summary>
    protected IEnumerable<DataObjectType> AuthorizedTypes => _authorizationProvider.Select(x => x.Target);

    internal IReadOnlyCollection<IUnboundAuthorizationInfo> Build()
        => _authorizationProvider;

    /// <summary>
    /// Creates a new instance of <see cref="AuthorizationProfile"/>
    /// </summary>
    /// <param name="typeCache"></param>
    protected AuthorizationProfile(ITypeCache typeCache)
    {
        _typeCache = typeCache;
    }

    #region AddAuthorization

    /// <summary>
    /// adds an authorization rule for all <typeparamref name="TDataObject"/>s
    /// <br/>the <paramref name="queryPredicate"/> and <paramref name="dataObjectPredicate"/> are applied as is
    /// </summary>
    /// <typeparam name="TDataObject">the dataObject to be authorized</typeparam>
    /// <typeparam name="TS1">the first service</typeparam>
    /// <param name="queryPredicate">used to authorize queries</param>
    /// <param name="dataObjectPredicate">used to authorize materialized dataObjects</param>
    protected void AddAuthorization<TDataObject, TS1>(
        Func<TS1, Expression<Func<TDataObject, bool>>> queryPredicate,
        Func<TS1, TDataObject, bool> dataObjectPredicate
    )
        where TDataObject : class, IDataObject
        where TS1 : notnull
        => _authorizationProvider.Add(
            new UnboundAuthorizationInfo<TDataObject, TS1>(queryPredicate, dataObjectPredicate, _typeCache));

    /// <summary>
    /// adds an authorization rule for all <typeparamref name="TDataObject"/>
    /// the <paramref name="predicate"/> will be
    /// - once compiled and used for data object Authorization
    /// - visited so that the service parameter becomes a constant. this happens each time a query is authorized
    /// <br/>
    /// </summary>
    /// <typeparam name="TDataObject">the dataObject to be authorized</typeparam>
    /// <typeparam name="TS1">the first service</typeparam>
    /// <param name="predicate">the authorization rule for this dataObject</param>
    protected void AddAuthorization<TDataObject, TS1>(
        Expression<Func<TS1, TDataObject, bool>> predicate
    )
        where TDataObject : class, IDataObject
        where TS1 : notnull
    {
        var p1 = predicate.Parameters[0];
        var p2 = predicate.Parameters[1];


        _authorizationProvider.Add(new UnboundAuthorizationInfo<TDataObject, TS1>(ExpressionGenerator,
            predicate.Compile(), _typeCache));

        return;

        Expression<Func<TDataObject, bool>> ExpressionGenerator(TS1 service)
        {
            var c = Expression.Constant(service, typeof(TS1));
            var body = new ParToConstVisitor(p1, c).Visit(predicate.Body);
            return Expression.Lambda<Func<TDataObject, bool>>(body, p2);
        }
    }

    #endregion

    /// <summary>
    /// Checks whether this <see cref="AuthorizationProfile"/> contains Rules for all instances of <typeparamref name="TDataObjectType"/> contained in the current <see cref="ITypeCache"/>
    /// </summary>
    /// <typeparam name="TDataObjectType"></typeparam>
    /// <returns>an <see cref="IExceptionProvider"/> which contains an <see cref="Exception"/> for each <typeparamref name="TDataObjectType"/> that are not authorized within this <see cref="AuthorizationProfile"/></returns>
    public IExceptionProvider EnsureAuthorised<TDataObjectType>()
        where TDataObjectType : class, IDataObjectType
    {
        var exceptions = new ExceptionBuilder($"Some {typeof(TDataObjectType).FullName(true)} types are not authorized");

        if (_typeCache.KnownTypeValueTypes.Contains(typeof(TDataObjectType)) is false)
            exceptions.Add(new UnknownDataObjectTypeException<TDataObjectType>());

        exceptions.Add(
            _typeCache.All<TDataObjectType>()
                .Except(this.AuthorizedTypes.OfType<TDataObjectType>())
                .Select(tdo => new MissingAuthorizationException(tdo))
            );
        return exceptions;
    }

}
/// <summary>
/// Indicates, that <see cref="TypeWithoutAuthorization"/> Is not authorized in a given <see cref="AuthorizationProfile"/> but should be.
/// </summary>
public class MissingAuthorizationException : JLibException
{
    /// <summary>
    /// the type which is not authorized
    /// </summary>
    public IDataObjectType TypeWithoutAuthorization { get; }

    internal MissingAuthorizationException(IDataObjectType typeWithoutAuthorization)
    {
        TypeWithoutAuthorization = typeWithoutAuthorization;
        Data.Add(nameof(TypeWithoutAuthorization), typeWithoutAuthorization);
    }
}

/// <summary>
/// Indicates, that the type parameter of <see cref="AuthorizationProfile.EnsureAuthorised{TDataObjectType}"/> is not known to the <see cref="ITypeCache"/>.
/// </summary>
public abstract class UnknownDataObjectTypeException : InvalidSetupException
{
    /// <summary>
    /// the type which is not known to the type cache
    /// </summary>
    public Type UnknownDataObjectType { get; }

    internal UnknownDataObjectTypeException(Type unknownDataObjectType) : base($"The {nameof(IDataObjectType)} {unknownDataObjectType.FullName()} is not known to the type cache")
    {
        UnknownDataObjectType = unknownDataObjectType;
        Data[nameof(UnknownDataObjectType)] = unknownDataObjectType;
    }
}

/// <summary>
/// <inheritdoc cref="UnknownDataObjectTypeException"/>
/// </summary>
public sealed class UnknownDataObjectTypeException<TDataObjectType> : UnknownDataObjectTypeException
    where TDataObjectType : class, IDataObjectType
{
    internal UnknownDataObjectTypeException() : base(typeof(IDataObjectType)) { }
}
internal class ParToConstVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _par;
    private readonly ConstantExpression _const;

    public ParToConstVisitor(ParameterExpression par, ConstantExpression @const)
    {
        _par = par;
        _const = @const;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _par
            ? _const
            : base.VisitParameter(node);
    }
}