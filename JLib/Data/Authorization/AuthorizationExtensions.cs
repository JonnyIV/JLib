﻿using JLib.Helper;
using JLib.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace JLib.Data.Authorization;
public static class AuthorizationExtensions
{
    /// <summary>
    /// adds data authorization
    /// <br/> requires <see cref="ServiceCollectionHelper.AddScopeProvider"/>
    /// <br/>services:
    /// <br/>+ singleton
    /// <br/>+-- <see cref="IAuthorizationManager"/>
    /// <br/>+ scoped
    /// <br/>+-- a <see cref="IAuthorizationInfo{TDataObject}"/> for each <see cref="DataObjectType"/>
    /// </summary>
    public static IServiceCollection AddDataAuthorization(this IServiceCollection services, ITypeCache typeCache)
    {
        services.AddSingleton<IAuthorizationManager, AuthorizationManager>();
        foreach (var dataObjectType in typeCache.All<DataObjectType>())
        {
            var t = typeof(IAuthorizationInfo<>).MakeGenericType(dataObjectType.Value);
            services.AddScoped(t,
                p => p.GetRequiredService<IAuthorizationManager>()
                    .Get(dataObjectType, p.GetRequiredService<IServiceScope>()));
        }
        return services;
    }
}
