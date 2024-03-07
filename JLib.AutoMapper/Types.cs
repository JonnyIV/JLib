using System.Reflection;
using AutoMapper;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using Microsoft.Extensions.Logging;

namespace JLib.AutoMapper;

/// <summary>
/// Identifies a type as <see cref="Profile"/>
/// </summary>
/// <param name="Value"></param>
[TvtFactoryAttribute.IsDerivedFrom(typeof(Profile)), TvtFactoryAttribute.NotAbstract]
public record AutoMapperProfileType(Type Value) : TypeValueType(Value)
{
    static readonly MethodInfo CreateLogger =
        typeof(LoggerFactoryExtensions)
            .GetMethod(
                nameof(LoggerFactoryExtensions.CreateLogger),
                1,
                new[] { typeof(ILoggerFactory) })
        ?? throw new InvalidSetupException("CreateLogger not found");
    /// <summary>
    /// Instantiates this profile, supporting <see cref="ILoggerFactory"/> and <see cref="ITypeCache"/> as constructor parameters<br/>
    /// Throws an <see cref="InvalidOperationException"/> if the <see cref="Profile"/> cannot be instantiated
    /// </summary>
    /// <param name="typeCache"></param>
    /// <param name="loggerFactory"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Profile Create(ITypeCache typeCache, ILoggerFactory loggerFactory)
    {
        var ctor = Value.GetConstructors().Single();

        var args = ctor.GetParameters().Select(p =>
            p.ParameterType == typeof(ITypeCache)
                ? typeCache.As<object>()
                : p.ParameterType == typeof(ILoggerFactory)
                    ? loggerFactory.As<object>()
                    : p.ParameterType == typeof(ILogger<>).MakeGenericType(Value)
                        ? CreateLogger.MakeGenericMethod(Value).Invoke(null, new object[] { loggerFactory })
                        : throw new InvalidOperationException(
                        $"unexpected ctor parameter {p.Name} of type {p.ParameterType.Name} in {Value.FullName()}")
        ).ToArray();

        return ctor.Invoke(args).As<Profile>()
               ?? throw new InvalidOperationException($"Instantiation of {Name} failed.");
    }
}