using System.Reflection;
using JLib.Exceptions;
using JLib.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace JLib.DataGeneration;

public interface IDataPackageManager
{
    internal DataPackageInitState InitState { get; }

    internal void IncludeDataPackages(Type[] packages);
    internal void SetIdPropertyValue(object packageInstance, PropertyInfo property);
}
/// <summary>
/// init manager and ctor parameter bundle for <see cref="DataPackage"/>s
/// </summary>
internal class DataPackageManager : IDataPackageManager
{
    private readonly IIdRegistry _idRegistry;
    private readonly IServiceProvider _provider;
    public DataPackageManager(IIdRegistry idRegistry, IServiceProvider provider, DataPackageConfiguration configuration)
    {
        _idRegistry = idRegistry;
        _provider = provider;
        Configuration = configuration;
    }

    public DataPackageInitState InitState { get; private set; }

    public DataPackageConfiguration Configuration { get; }

    public void IncludeDataPackages(Type[] packages)
    {
        if (InitState != DataPackageInitState.Uninitialized)
            throw new InvalidOperationException($"dataPackages are {InitState} and cannot be loaded again.");

        packages.Where(p => !p.IsAssignableTo<DataPackage>() || p.IsAbstract)
            .Select(p =>
                new InvalidSetupException(p.FullName() +
                                          "is not a valid typePackage. it must not be abstract and be Derived from DataPackage"))
            .ThrowExceptionIfNotEmpty("invalid DataPackages found");

        InitState = DataPackageInitState.Initializing;

        foreach (var dataPackageType in packages)
            _provider.GetRequiredService(dataPackageType);
        InitState = DataPackageInitState.Initialized;
    }

    public void SetIdPropertyValue(object packageInstance, PropertyInfo property)
        => _idRegistry.SetIdPropertyValue(packageInstance, property);
}