using Microsoft.Extensions.DependencyInjection;

namespace JLib.DataGeneration.Abstractions;

/// <summary>
/// Extension methods for adding the <see cref="IIdGenerator"/> to the <see cref="IServiceCollection"/>.
/// </summary>
public static class IdGeneratorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IIdGenerator"/> implementation to the <see cref="IServiceCollection"/> as a singleton.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="IIdGenerator"/> to.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddIdGenerator(this IServiceCollection services)
    {
        services.AddSingleton<IIdGenerator, IdGenerator>();
        return services;
    }
}