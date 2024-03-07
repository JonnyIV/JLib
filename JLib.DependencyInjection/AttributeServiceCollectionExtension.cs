using System.Reflection;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace JLib.DependencyInjection;
public static class AttributeServiceCollectionExtension
{
    /// <summary>
    /// adds types with the <see cref="ServiceAttribute"/> to the service collection.<br/>
    /// if it is a class, it will be treated as implementation and will always be provided with the <see cref="ServiceLifetime"/> of <see cref="ServiceAttribute.ServiceLifetime"/><br/>
    /// if it is an interface, each class implementing this interface will be added.<br/>
    /// If the implementation has no <see cref="ServiceAttribute"/>, the implementation will be provided with the <see cref="ServiceLifetime"/> of the interface.<br/>
    /// If an implementation implements multiple Service Interfaces, all service interfaces and the implementation will be provided, all referring to the same instance<br/>
    /// If there are multiple implementations for the same service interface, the implementation with the <see cref="ServiceImplementationOverrideAttribute"/> will be used<br/>
    /// It is highly recommended to call<see cref="IExceptionProvider.ThrowIfNotEmpty"/> before building the service provider.
    /// <br/><br/>
    /// 
    /// references:
    /// <list type="bullet">
    /// <item> <seealso cref="ServiceCollectionHelper.AddAlias"/> </item>
    /// <item><seealso cref="ServiceAttribute"/></item>
    /// <item><seealso cref="ServiceImplementationOverrideAttribute"/></item>
    /// </list>
    ///
    /// Examples:
    /// <example>
    /// <code>
    /// // provided as singleton
    /// [Service(ServiceLifetime.Singleton)]
    /// class ShoppingService { }
    /// 
    /// // provided as singleton
    /// [Service(ServiceLifetime.Singleton)]
    /// class ShoppingService : IShoppingService { }
    /// // ignored
    /// interface IShoppingService { }
    /// 
    /// // not provided
    /// class ShoppingService : IShoppingService { }
    /// // provided as singleton with ShoppingService as implementation (type)
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingService { }
    /// 
    /// // provided as singleton
    /// [Service(ServiceLifetime.Singleton)]
    /// class ShoppingService : IShoppingService { }
    /// // provided as singleton with ShoppingService as alias factory
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingService { }
    /// 
    /// // throws exception, since the implementation has a lower lifetime than the interface
    /// [Service(ServiceLifetime.Scoped)]
    /// class ShoppingService : IShoppingService { }
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingService { } 
    /// 
    /// // provided as singleton
    /// [Service(ServiceLifetime.Singleton)]
    /// class ShoppingService : IShoppingService { }
    /// // provided as scoped with ShoppingService as alias factory - this means the service will act like a singleton but be injected as scoped
    /// [Service(ServiceLifetime.Scoped)]
    /// interface IShoppingService { }
    /// 
    /// // provided as singleton
    /// class ShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// // provided as singleton with ShoppingService as alias factory
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingQueryService { }
    /// // provided as scoped with ShoppingService as alias factory
    /// [Service(ServiceLifetime.Scoped)]
    /// interface IShoppingCommandService { }
    ///
    /// undesirable:
    /// // provided as singleton
    /// class ShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// // provided as singleton
    /// class MockShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// // provided twice as singleton with ShoppingService as alias factory and MockShoppingService as alias factory
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingQueryService { }
    /// // provided twice as singleton with ShoppingService as alias factory and MockShoppingService as alias factory
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingCommandService { }
    /// 
    ///  solution:
    /// // provided as singleton
    /// class ShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// // provided as singleton
    /// [ServiceImplementationOverride] // add this attribute to define implementation priority
    /// class MockShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// // provided as singleton with MockShoppingService as alias factory
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingQueryService { }
    /// // provided as singleton with MockShoppingService as alias factory
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingCommandService { }
    /// 
    /// undesirable:
    /// // provided as singleton
    /// class ShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// // provided as singleton
    /// [ServiceImplementationOverride]
    /// class MockShoppingService : IShoppingCommandService { }
    /// // provided as singleton with ShoppingService as alias factory - the non-mock implementation will be used since the mock does not implement this interface
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingQueryService { }
    /// // provided as singleton with MockShoppingService as alias factory
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingCommandService { }
    /// 
    /// solution:
    /// class ShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// [ServiceImplementationOverride(typeof(ShoppingService))] // causes an exception to be thrown indicating the missing interface
    /// class MockShoppingService : IShoppingQueryService, IShoppingCommandService { }
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingQueryService { }
    /// [Service(ServiceLifetime.Singleton)]
    /// interface IShoppingCommandService { }
    /// 
    /// </code>
    /// </example>
    /// </summary>
    public static IServiceCollection AddServicesWithAttributes(this IServiceCollection services, ITypeCache typeCache, ExceptionBuilder exceptions)
    {
        exceptions = exceptions.CreateChild(nameof(AddServicesWithAttributes));

        // contains all types which either have the service attribute directly or have an interface with the service attribute
        var serviceDetails = typeCache
            .KnownTypes
            .Where(t => t.IsClass)
            .Select(t => new
            {
                Implementation = t,
                Services = t
                    .GetInterfaces()
                    .Where(x => x.HasCustomAttribute<ServiceAttribute>())
                    .ToArray(),
                isOverride = t.HasCustomAttribute<ServiceImplementationOverrideAttribute>(),
                overriddenImplementation = t.GetCustomAttribute<ServiceImplementationOverrideAttribute>()?.MatchServiceInterfacesOfType
            })
            .Where(x => x.Services.Any() || x.Implementation.HasCustomAttribute<ServiceAttribute>())
            .ToReadOnlyCollection();

        var overrides = serviceDetails
            .Where(x => x.isOverride)
            .ToReadOnlyCollection();

        // override does neither have a service with the service attribute nor has it itself
        exceptions
            .CreateChild("override implementations are not associated to any service")
            .Add(typeCache.KnownTypes
                .Where(t => t.HasCustomAttribute<ServiceImplementationOverrideAttribute>())
                .Except(overrides.Select(o => o.Implementation))
                .Select(invalidOverride => new InvalidSetupException(invalidOverride.FullName()))
            );

        // check for missing service interfaces in override
        foreach (var @override in overrides.Where(o => o.overriddenImplementation is not null))
        {
            var subEx = exceptions.CreateChild($"override {@override.Implementation.FullName()} is invalid");

            var overridden = serviceDetails
                .SingleOrDefault(s => s.Implementation == @override.overriddenImplementation);
            if (overridden is null)
            {
                subEx.Add(new InvalidSetupException(
                    $"implementation is set to match ${@override.overriddenImplementation?.FullName()}, but this class does not have any services associated"));
                continue;
            }
            exceptions
                .CreateChild("missing services")
                .Add(overridden.Services.Except(@override.Services)
                    .Select(overriddenService => new InvalidSetupException(overriddenService.FullName()))
                );
        }

        // add the services to the collection
        foreach (var serviceDetail in serviceDetails)
        {
            try
            {
                var implementationType = serviceDetail.Implementation;
                var serviceTypes = serviceDetail.Services;

                var implementationLifetime = implementationType.GetCustomAttribute<ServiceAttribute>()?.ServiceLifetime;

                switch (serviceTypes.Length)
                {
                    case 0:
                        services.Add(new(
                            implementationType,
                            implementationType,
                            implementationLifetime
                                ?? throw new InvalidSetupException($"{implementationType.FullName(true)} service implementation lifetime not found")
                            ));
                        break;
                    case 1 when implementationLifetime.HasValue == false
                                && serviceDetail.isOverride == false // do not add the service if it is overridden
                                && overrides.None(o => o.Implementation == implementationType):

                        services.Add(new(
                            serviceTypes.Single(),
                            implementationType,
                            serviceTypes
                                .Single()
                                .GetCustomAttribute<ServiceAttribute>()?.ServiceLifetime
                                    ?? throw new InvalidSetupException($"{implementationType.FullName(true)} => {serviceTypes.Single().FullName(true)} service interface lifetime not found")
                          ));
                        break;
                    case 1: // when implementationLifetime.HasValue
                    case > 1:
                        implementationLifetime ??= serviceTypes.Min(st =>
                            st.GetCustomAttribute<ServiceAttribute>()?.ServiceLifetime);// gets the highest required scope

                        if (implementationLifetime.HasValue == false)
                        {
                            exceptions.Add(new InvalidSetupException(
                                $"{implementationType.FullName(true)} => {serviceTypes.Single().FullName(true)} service implementation lifetime not found"));
                            break;
                        }
                    
                        bool anyServiceProvided = false;
                        foreach (var serviceType in serviceTypes)
                        {
                            // override handling
                            if (!serviceDetail.isOverride
                                // overrides contains any implementation of this service
                                && overrides.Any(o => o.Services.Contains(serviceType))
                                )
                            {
                                continue;
                            }

                            try
                            {
                                var serviceLifetime = serviceType.GetCustomAttribute<ServiceAttribute>()?.ServiceLifetime;
                                if (serviceLifetime.HasValue == false)
                                {
                                    exceptions.Add(new InvalidSetupException(
                                        $"{implementationType.FullName(true)} => {serviceType.FullName(true)} service interface lifetime not found"));
                                    continue;
                                }
                                if (serviceLifetime < implementationLifetime)
                                {
                                    exceptions.Add(new InvalidSetupException(
                                        $"{implementationType.FullName(true)} => {serviceType.FullName(true)} service interface lifetime {serviceLifetime} is less than implementation lifetime {implementationLifetime}"));
                                    continue;
                                }
                                services.AddAlias(
                                    serviceType,
                                    implementationType,
                                    serviceLifetime.Value
                                );
                                anyServiceProvided = true;

                            }
                            catch (Exception e)
                            {
                                exceptions.Add(e);
                            }
                        }

                        // provide the implementation only, if there is any service referencing it, or it has the service attribute
                        if (anyServiceProvided ||
                            implementationType.HasCustomAttribute<ServiceImplementationOverrideAttribute>())
                        {
                            // the implementation provides itself, so that all interfaces can share the same instance
                            services.Add(new(
                                implementationType,
                                implementationType,
                                implementationLifetime.Value));
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        return services;
    }
}
