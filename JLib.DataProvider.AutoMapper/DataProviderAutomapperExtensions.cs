using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ITypeCache = JLib.Reflection.ITypeCache;

namespace JLib.DataProvider.AutoMapper;

public static class DataProviderAutomapperExtensions
{
    #region AddMapDataprovider

    /// <summary>
    /// adds map data providers for each <see cref="IMappedDataObjectType"/>.
    /// <br/>generates a <see cref="IDataProviderR{TData}"/> if <see cref="IMappedDataObjectType.ReverseMap"/> is false
    /// <br/>generates a <see cref="IDataProviderRw{TData}"/> and a <see cref="IDataProviderR{TData}"/> as alias if it is true
    /// </summary>
    public static IServiceCollection AddMapDataProvider(
        this IServiceCollection services,
        ITypeCache typeCache,
        ExceptionBuilder exceptions)
    {
        exceptions = exceptions.CreateChild(nameof(AddMapDataProvider));
        foreach (var mappedDataObjectType in typeCache.All<IMappedDataObjectType>())
        {
            foreach (var typeMappingInfoEx in mappedDataObjectType.MappingInfo)
            {
                var (sourceType, destinationType, providerMode) = typeMappingInfoEx;

                var repo = typeCache.TryGet<RepositoryType>(
                    r => r.ProvidedDataObject.Value == destinationType.Value);

                switch (providerMode)
                {
                    case MappingDataProviderMode.Disabled:
                        continue;
                    case MappingDataProviderMode.Read:
                        {
                            if (repo is { CanWrite: true })
                            {
                                exceptions.Add(new InvalidSetupException(
                                    $"Repository {repo.Value.FullName()} for Entity {destinationType.Value.FullName()} can write data but the mapping is configured to provide a {nameof(IDataProviderR<IDataObject>)}." +
                                    Environment.NewLine +
                                    $"To fix this issue either set the mapping to ReadWrite or don't implement {nameof(IDataProviderRw<IEntity>)} on the repository"));
                                continue;
                            }

                            var implementation = typeof(MapDataProviderR<,>).MakeGenericType(sourceType, destinationType);
                            services.AddScoped(implementation);
                            services.AddScoped(typeof(ISourceDataProviderR<>).MakeGenericType(destinationType),
                                implementation);
                            if (repo is null)
                                services.AddScoped(typeof(IDataProviderR<>).MakeGenericType(destinationType),
                                    implementation);
                        }
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Provides a <see cref="IDataProviderR{TData}"/> for each <typeparamref name="TTvt"/> which takes data from a <see cref="IDataProviderR{TData}"/> of the type retrieved by <see cref="sourcePropertyReader"/> and maps it using AutoMapper Projections
    /// </summary>
    public static IServiceCollection AddMapDataProvider<TTvt>(
        this IServiceCollection services,
        ITypeCache typeCache,
        Func<TTvt, bool>? filter,
        Func<TTvt, ITypeValueType?> sourcePropertyReader,
        Func<TTvt, bool> isReadOnly,
        ExceptionBuilder exceptions,
        ILoggerFactory loggerFactory)
        where TTvt : class, IDataObjectType
    {
        var logger = loggerFactory.CreateLogger(typeof(DataProviderAutomapperExtensions).FullName!);
        using var _ = logger.BeginScope(nameof(AddMapDataProvider));
        filter ??= _ => true;
        exceptions = exceptions.CreateChild(
            $"{nameof(AddMapDataProvider)} failed for valueType {typeof(TTvt).Name}");
        var typeArguments =
            new Func<TTvt, ITypeValueType>[]
            {
                destination => sourcePropertyReader(destination)!,
                destination => destination
            };
        logger.LogTrace("ReadOnly MapDataProvider");
        services.AddDataProvider<TTvt, MapDataProviderR<IEntity, IEntity>, IEntity>(
            typeCache,
            tvt => filter(tvt) && isReadOnly(tvt),
            null,
            typeArguments,
            exceptions,
            loggerFactory
        );
        //Log.ForContext(typeof(ServiceCollectionHelper)).ForContext<IDataProviderR<IDataObject>>().Verbose("WritableMapDataProvider");
        //services.AddDataProvider<TTvt, WritableMapDataProvider<IEntity, IEntity>, IEntity>(
        //    typeCache,
        //    tvt => filter(tvt) && !isReadOnly(tvt),
        //    null,
        //    typeArguments,
        //    exceptions
        //);
        return services;
    }

    #endregion
}