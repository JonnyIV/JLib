using JLib.DependencyInjection;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JLib.DataProvider;
public static class DataProviderServiceCollectionExtensions
{
    #region AddRepositories

    /// <summary>
    /// adds all repositories to the service provider
    /// </summary>
    public static IServiceCollection AddRepositories(
        this IServiceCollection services, ITypeCache typeCache, ExceptionBuilder exceptions)
    {
        // making sure that no two repos are provided for the same DataObject
        var groupedRepos = typeCache.All<RepositoryType>()
            .GroupBy(x => x.ProvidedDataObject)
            .ToDictionary(x => x.Key, x => x.AsEnumerable());

        var invalidRepos = groupedRepos
            .Where(x => x.Value.Count() > 1)
            .Select(group => new InvalidSetupException(
                $"multiple repos have been provided for data object {group.Key.Value.FullName(true)}:" +
                $" {string.Join(", ", group.Value.Select(repo => repo.Value.FullName(true)).OrderBy(r => r))}"))
            .ToArray();

        if (invalidRepos.Any())
        {
            exceptions.CreateChild(nameof(AddRepositories)).Add(invalidRepos);
            return services;
        }

        foreach (var repo in groupedRepos.Select(x => x.Value.Single()))
        {
            services.AddScoped(repo.Value);
            services.AddAlias(
                typeof(IDataProviderR<>).MakeGenericType(repo.ProvidedDataObject.Value),
                repo.Value,
                ServiceLifetime.Scoped);

            if (repo.CanWrite)
                services.AddAlias(
                    typeof(IDataProviderRw<>).MakeGenericType(repo.ProvidedDataObject.Value),
                    repo.Value,
                    ServiceLifetime.Scoped);
        }

        return services;
    }

    #endregion

    #region AddDataProvider

    /// <summary>
    /// Provides the following services for each <typeparamref name="TTvt"/> under the stated conditions:
    /// <br/>new Services:
    /// <br/>ㅤAlways - <typeparamref name="TImplementation"/> as using <paramref name="implementationTypeArgumentResolver"/> to resolve the type arguments
    /// <br/>Alias for <typeparamref name="TImplementation"/>
    /// <br/>ㅤAlways - <see cref="ISourceDataProviderR{TData}"/> as Alias 
    /// <br/>ㅤ<typeparamref name="TImplementation"/> implements <see cref="ISourceDataProviderRw{TData}"/> - <see cref="ISourceDataProviderRw{TData}"/>
    /// <br/>ㅤno <see cref="RepositoryType"/> for the given <typeparamref name="TTvt"/> - <see cref="IDataProviderR{TData}"/>
    /// <br/>ㅤno <see cref="RepositoryType"/> for the given <typeparamref name="TTvt"/> and <typeparamref name="TImplementation"/> implements <see cref="IDataProviderRw{TData}"/> - <see cref="IDataProviderRw{TData}"/>
    /// </summary>
    /// <typeparam name="TTvt">the <see cref="Reflection.TypeValueType"/> which instances will be added as <see cref="IDataProviderR{TDataObject}"/></typeparam>
    /// <typeparam name="TImplementation">the implementation of the <see cref="IDataProviderR{TDataObject}"/> to be used. Generics will be ignored.</typeparam>
    /// <typeparam name="TIgnoredDataObject">the ignored type argument of <see cref="IDataProviderR{TDataObject}"/> implemented by <typeparamref name="TImplementation"/></typeparam>
    /// <param name="services"></param>
    /// <param name="typeCache"><see cref="AddTypeCache(IServiceCollection,out ITypeCache, ExceptionBuilder, ITypePackage[])"/></param>
    /// <param name="filter">if the filter is provided and returns false, no <see cref="IDataProviderR{TDataObject}"/> will be created for the given <see cref="Reflection.TypeValueType"/><br/>
    /// null defaults to '_=>true'</param>
    /// <param name="forceReadOnly">if provided and true, only <see cref="IDataProviderR{TDataObject}"/> and <see cref="ISourceDataProviderR{TData}"/> will be provided but not <see cref="IDataProviderRw{TData}"/> or <see cref="ISourceDataProviderRw{TData}"/><br/>
    /// null defaults to '_=>false'</param>
    /// <param name="implementationTypeArgumentResolver">
    /// resolves the type arguments for the implementation in order.<br/>
    /// null defaults to 'new[]{ tvt => tvt }'<br/>
    /// <p>Example</p>
    /// <code>
    /// DataProvider: ComplexDataProvider&lt;TEntity,TEntityInterface&gt; : IDataProviderR&lt;TEntity&gt;
    /// TypeValueType: ComplexEntity { Interface : TypeValueType }
    /// value of <paramref name="implementationTypeArgumentResolver"/>: new[]
    /// {
    ///     tvt=>tvt,
    ///     (ComplexEntity tvt) => tvt.Interface
    /// }
    /// </code>
    /// </param>
    /// <param name="exceptions"></param>
    /// <param name="lifetime">the lifetime of the services to be added</param>
    public static IServiceCollection AddDataProvider<TTvt, TImplementation, TIgnoredDataObject>(
        this IServiceCollection services,
        ITypeCache typeCache,
        Func<TTvt, bool>? filter,
        Func<TTvt, bool>? forceReadOnly,
        Func<TTvt, ITypeValueType>[]? implementationTypeArgumentResolver,
        ExceptionBuilder exceptions,
        ILoggerFactory loggerFactory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TTvt : class, IDataObjectType
        where TImplementation : IDataProviderR<TIgnoredDataObject>
        where TIgnoredDataObject : IDataObject
    {


        filter ??= _ => true;
        forceReadOnly ??= _ => false;
        var implementation = typeof(TImplementation).TryGetGenericTypeDefinition();
        var msg = typeof(TTvt).FullName();
        exceptions = exceptions.CreateChild(msg);

        services.AddGenericServices(typeCache, implementation, implementation,
            lifetime, exceptions, loggerFactory, filter, implementationTypeArgumentResolver,
            implementationTypeArgumentResolver);

        #region read/write mode mismatch check

        var repos = typeCache.All<RepositoryType>(repo => repo.ProvidedDataObject is TTvt).ToArray();
        var implementationCanWrite = implementation.ImplementsAny<IDataProviderRw<IEntity>>();
        foreach (var repo in repos)
        {
            var repoIsReadOnly = repo.Value.ImplementsAny<IDataProviderRw<IEntity>>() is false;
            if (repo.ProvidedDataObject is not TTvt tvt || !filter(tvt))
                continue;
            var readOnlyForced = forceReadOnly(tvt);
            var dataProviderIsReadOnly = !implementationCanWrite || readOnlyForced;
            var args = implementationTypeArgumentResolver?.Select(x => x(tvt)).ToArray() ?? new[] { tvt };
            Type impl;
            try
            {
                impl = implementation.MakeGenericType(args);
            }
            // thrown, when the typeConstraints are violated. Doing this manually is not really a good idea
            catch (Exception e) when (e.InnerException is ArgumentException { HResult: -2147024809 })
            {
                continue;
            }

            if (dataProviderIsReadOnly == repoIsReadOnly)
                continue;

            string errorText =
                dataProviderIsReadOnly
                    ? readOnlyForced
                        ? $"The data provider Implementation {impl.FullName(true)} is forced read only but the Repository {repo.Value.FullName(true)} can write data. {Environment.NewLine}" +
                          $"Not forcing the DataProvider to be read only or implementing {nameof(IDataProviderRw<IEntity>)} will solve this issue"
                        : $"The data provider Implementation {impl.FullName(true)} is read only but the Repository {repo.Value.FullName(true)} can write data. {Environment.NewLine}" +
                          $"You can resolve this issue by not implementing {nameof(IDataProviderRw<IEntity>)} with the Repository or using a data provider which implements {nameof(ISourceDataProviderRw<IEntity>)}"
                    : $"The data provider Implementation {impl.FullName(true)} can write data but the Repository {repo.Value.FullName(true)} can not. {Environment.NewLine}" +
                      $"Force the dataProvider to be ReadOnly or Implement {nameof(IDataProviderRw<IEntity>)} with the repository.";
            exceptions.Add(new InvalidSetupException(errorText));
        }

        #endregion

        foreach (var serviceType in new[]
                 {
                     typeof(IDataProviderR<>), typeof(IDataProviderRw<>), typeof(ISourceDataProviderR<>),
                     typeof(ISourceDataProviderRw<>)
                 })
            services.AddGenericAlias(typeCache, serviceType, implementation,
                lifetime, exceptions, loggerFactory, FilterIt(implementation, serviceType, repos),
                null, implementationTypeArgumentResolver);

        return services;

        // Warning: thw following code governs the entire behavior for which service alias is provided when. All Updates which modify the current behavior are breaking changes.
        // run the TypeCacheTests before pushing any changes
        Func<TTvt, bool> FilterIt(Type myImplementation, Type serviceType, RepositoryType[] repositories)
            => tvt =>
            {
                var filterResult = filter!.Invoke(tvt);
                var interfaceImplemented = myImplementation.ImplementsAny(serviceType);
                var excludeWritable = !(serviceType.ImplementsAny<IDataProviderRw<IEntity>>() && forceReadOnly(tvt));
                var excludeRepos = serviceType.ImplementsAny<ISourceDataProviderR<IDataObject>>() ||
                                   repositories.None(repo => repo.ProvidedDataObject as IDataObjectType == tvt);
                return filterResult && interfaceImplemented && excludeWritable && excludeRepos;
            };
    }

    #endregion

}
