using JLib.Exceptions;
using JLib.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace JLib.DependencyInjection;

/// <summary>
/// <see cref="AttributeServiceCollectionExtension.AddServicesWithAttributes"/>
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class ServiceAttribute : Attribute
{
    public ServiceLifetime ServiceLifetime { get; }

    public ServiceAttribute(ServiceLifetime serviceLifetime)
    {
        ServiceLifetime = serviceLifetime;
    }
}

/// <summary>
/// <see cref="AttributeServiceCollectionExtension.AddServicesWithAttributes"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ServiceImplementationOverrideAttribute : Attribute
{
    public Type? MatchServiceInterfacesOfType { get; }
    /// <summary>
    /// when a service has multiple implementations, the implementation with this attribute will be used
    /// </summary>
    /// <param name="matchServiceInterfacesOfType">
    ///     an exception will be thrown, if the given type has other service interfaces than the decorated one<br/>
    ///     a service interface is an interface which is decorated with the <see cref="ServiceAttribute"/>
    /// </param>
    public ServiceImplementationOverrideAttribute(Type? matchServiceInterfacesOfType = null)
    {
        MatchServiceInterfacesOfType = matchServiceInterfacesOfType;
        if (matchServiceInterfacesOfType?.IsClass == false)
            throw new InvalidSetupException(
                $"the given {nameof(matchServiceInterfacesOfType)} {matchServiceInterfacesOfType.FullName()} is not a class and therefore not a valid implementation");
    }
}