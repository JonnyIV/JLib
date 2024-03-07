using JLib.Helper;
using JLib.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;

namespace JLib.Testing;
public static class EnumerableSnapshotExtensions
{
    /// <summary>
    /// groups the enumerable by namespace and then by typeValueType, using json objects to identify each group
    /// <br/>used to call <see cref="SnapshotExtension.MatchSnapshot(object,Func{MatchOptions,MatchOptions})"/> afterwards (prevents having to provide a <see cref="SnapshotNameExtension"/>)
    /// </summary>
    public static object PrepareSnapshot(this IEnumerable<ITypeValueType> source)
        => source.ToLookup(tvt => tvt.Value.Namespace ?? "")
            .OrderBy(x => x.Key)
            .ToDictionary(tvtGroup1 => tvtGroup1.Key, tvt => tvt
                .ToLookup(tvt1 => tvt1.GetType().FullName(true))
                .OrderBy(x => x.Key)
                .ToDictionary(
                    tvtGroup2 => tvtGroup2.Key,
                    tvtGroup2 => tvtGroup2
                        .Select(tvt2 => tvt2.Value.FullName(true))
                        .OrderBy(x => x)
                )
            );

    /// <summary>
    /// groups all services of the provider by the first generic type argument or under the non-generic group if they are not generic
    /// <br/>then transforms the service provider to a object which contains all relevant information, including the implementation service type but excluding the implementation details.
    /// <br/>used to call <see cref="SnapshotExtension.MatchSnapshot(object,Func{MatchOptions,MatchOptions})"/> afterwards (prevents having to provide a <see cref="SnapshotNameExtension"/>)
    /// <br/>
    /// <br/><b>!!!</b> builds a provider and creates a scope <b>!!!</b>
    /// <br/><b>!!!</b> creates a service instance for each service using a factory as resolver - which is used to implement a alias <b>!!!</b>
    /// </summary>
    public static object PrepareSnapshot(this IServiceCollection services)
    {
        var disposables = new List<IDisposable>();
        var p = new Lazy<IServiceProvider>(() =>
        {
            var p = services.BuildServiceProvider();
            disposables.Add(p);
            return p;
        });
        var sp = new Lazy<IServiceProvider>(() =>
        {
            var s = p.Value.CreateScope();
            disposables.Add(s);
            return s.ServiceProvider;
        });
        var res = services.ToLookup(
                x => x.ServiceType.GenericTypeArguments.LastOrDefault()?.FullName(true)
                     ?? "non-generic",
                serviceDescriptor =>
                {
                    var implementation = serviceDescriptor.ImplementationType
                                         ?? serviceDescriptor.ImplementationInstance?.GetType()
                                         ?? serviceDescriptor.ImplementationFactory?.Invoke(sp.Value).GetType();
                    var implementationSrc = serviceDescriptor switch
                    {
                        { ImplementationFactory: not null } => "Factory",
                        { ImplementationInstance: not null } => "Instance",
                        { ImplementationType: not null } => "Type",
                        _ => null
                    };

                    return new
                    {
                        serviceDescriptor.Lifetime,
                        ImplementationSource = implementationSrc,
                        ServiceType = serviceDescriptor.ServiceType.TryGetGenericTypeDefinition().FullName(true),
                        ServiceArguments = GetTypeArgs(serviceDescriptor.ServiceType),
                        ImplementationType = implementation?.TryGetGenericTypeDefinition().FullName(true),
                        ImplementationTypeArguments = GetTypeArgs(implementation),
                    };

                    static string[] GetTypeArgs(Type? t)
                        => t?.GenericTypeArguments.Select(t => t.FullName(true)).ToArray() ??
                           Array.Empty<string>();
                })
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, descriptorInfos
                => descriptorInfos
                    .ToLookup(service => service.ServiceType)
                    .OrderBy(x => x.Key)
                    .ToDictionary(
                        serviceImplementations => serviceImplementations.Key,
                        x => x.OrderBy(y => y)));
        disposables.ForEach(d => d.Dispose());
        return res;
    }

    public static ExceptionSnapshotInfo? PrepareSnapshot(this Exception? exception)
        => exception is null
            ? null
            : new ExceptionSnapshotInfo(exception);
}