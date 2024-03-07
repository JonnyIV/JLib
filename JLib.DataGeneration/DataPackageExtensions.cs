using JLib.DataGeneration.Abstractions;
using JLib.DependencyInjection;
using JLib.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace JLib.DataGeneration;

/// <summary>
/// Extension methods for configuring data packages.
/// </summary>
public static class DataPackageExtensions
{
    /// <summary>
    /// Adds the ID registry to the service collection. Should be omitted when calling <see cref="AddDataPackages"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultNamespace">The default namespace for ID group names. It will be removed from all <see cref="DataPackageValues.IdIdentifier"/>s</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddIdRegistry(this IServiceCollection services, string? defaultNamespace = null)
    {
        return services.AddSingleton<IIdRegistry, IdRegistry>(provider =>
        {
            var replaceNamespace = defaultNamespace is null 
                ? null 
                : defaultNamespace.EndsWith(".")
                    ? defaultNamespace
                    : defaultNamespace + ".";

            return new(provider, PostProcessor);

            DataPackageValues.IdIdentifier PostProcessor(DataPackageValues.IdIdentifier id)
            {
                if (replaceNamespace is null)
                    return id;
                return new(
                    new(id.IdGroupName.Value.Replace(replaceNamespace, "")),
                    new(id.IdName.Value.Replace(replaceNamespace, ""))
                );
            }
        });
    }

    /// <summary>
    /// Adds the testing ID generator to the service collection. Should be omitted when calling <see cref="AddDataPackages"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddTestingIdGenerator(this IServiceCollection services) 
        => services.AddSingleton<TestingIdGenerator>()
            .AddSingletonAlias<IIdGenerator, TestingIdGenerator>();

    /// <summary>
    /// Adds the data packages to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="typeCache">The type cache.</param>
    /// <param name="configuration">The data package configuration.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddDataPackages(this IServiceCollection services, ITypeCache typeCache, DataPackageConfiguration? configuration = null)
    {
        services.AddIdRegistry(configuration?.DefaultNamespace)
            .AddTestingIdGenerator();

        services.AddSingleton(configuration ?? new DataPackageConfiguration());

        services.AddSingleton<IDataPackageManager, DataPackageManager>();

        foreach (var package in typeCache.All<DataPackageType>())
            services.AddSingleton(package.Value);
        return services;
    }

    /// <summary>
    /// Includes the specified data packages in the service provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="packages">The data package types to include.</param>
    /// <returns>The modified service provider.</returns>
    public static IServiceProvider IncludeDataPackages(this IServiceProvider provider, params DataPackageType[] packages)
    {
        return provider.IncludeDataPackages(packages.Select(p => p.Value).ToArray());
    }

    #region overloads

    /// <summary>
    /// Includes the specified data packages in the service provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="packages">The data package types to include.</param>
    /// <returns>The modified service provider.</returns>
    public static IServiceProvider IncludeDataPackages(this IServiceProvider provider, params Type[] packages)
    {
        provider.GetRequiredService<IDataPackageManager>().IncludeDataPackages(packages);
        return provider;
    }

    /// <summary>
    /// Includes the specified data package in the service provider.
    /// </summary>
    /// <typeparam name="T">The type of the data package.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The modified service provider.</returns>
    public static IServiceProvider IncludeDataPackages<T>(this IServiceProvider provider)
        where T : DataPackage
    {
        return provider.IncludeDataPackages(typeof(T));
    }

    /// <summary>
    /// Includes the specified data packages in the service provider.
    /// </summary>
    /// <typeparam name="T1">The type of the first data package.</typeparam>
    /// <typeparam name="T2">The type of the second data package.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The modified service provider.</returns>
    public static IServiceProvider IncludeDataPackages<T1, T2>(this IServiceProvider provider)
        where T1 : DataPackage
        where T2 : DataPackage
    {
        return provider.IncludeDataPackages(typeof(T1), typeof(T2));
    }

    /// <summary>
    /// Includes the specified data packages in the service provider.
    /// </summary>
    /// <typeparam name="T1">The type of the first data package.</typeparam>
    /// <typeparam name="T2">The type of the second data package.</typeparam>
    /// <typeparam name="T3">The type of the third data package.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The modified service provider.</returns>
    public static IServiceProvider IncludeDataPackages<T1, T2, T3>(this IServiceProvider provider)
        where T1 : DataPackage
        where T2 : DataPackage
        where T3 : DataPackage
    {
        return provider.IncludeDataPackages(typeof(T1), typeof(T2), typeof(T3));
    }

    /// <summary>
    /// Includes the specified data packages in the service provider.
    /// </summary>
    /// <typeparam name="T1">The type of the first data package.</typeparam>
    /// <typeparam name="T2">The type of the second data package.</typeparam>
    /// <typeparam name="T3">The type of the third data package.</typeparam>
    /// <typeparam name="T4">The type of the fourth data package.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The modified service provider.</returns>
    public static IServiceProvider IncludeDataPackages<T1, T2, T3, T4>(this IServiceProvider provider)
        where T1 : DataPackage
        where T2 : DataPackage
        where T3 : DataPackage
        where T4 : DataPackage
    {
        return provider.IncludeDataPackages(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
    }

    /// <summary>
    /// Includes the specified data packages in the service provider.
    /// </summary>
    /// <typeparam name="T1">The type of the first data package.</typeparam>
    /// <typeparam name="T2">The type of the second data package.</typeparam>
    /// <typeparam name="T3">The type of the third data package.</typeparam>
    /// <typeparam name="T4">The type of the fourth data package.</typeparam>
    /// <typeparam name="T5">The type of the fifth data package.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The modified service provider.</returns>
    public static IServiceProvider IncludeDataPackages<T1, T2, T3, T4, T5>(this IServiceProvider provider)
        where T1 : DataPackage
        where T2 : DataPackage
        where T3 : DataPackage
        where T4 : DataPackage
        where T5 : DataPackage
    {
        return provider.IncludeDataPackages(typeof(T1), typeof(T2), typeof(T3), typeof(T5));
    }

    #endregion
}
