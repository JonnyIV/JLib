using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using JLib.Exceptions;
using JLib.Helper;

namespace JLib.ValueTypes;


/// <summary>
/// Enables Validating a value of type <typeparamref name="T"/>
/// using all methods decorated with the <see cref="ValidationAttribute"/>. in the <see cref="TargetType"/><br/>
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IValidationProfile<in T>
{
    /// <summary>
    /// the type which validators are used to <see cref="Validate"/> the given value<br/>
    /// Validators are detected by the <see cref="ValidationAttribute"/>.
    /// </summary>
    public Type TargetType { get; }
    /// <summary>
    /// validates the given <paramref name="value"/> using all validators defined by the <see cref="TargetType"/><br/>
    /// Validators are detected by the <see cref="ValidationAttribute"/>.<br/>
    /// if the validator does not match the required signature, an exception containing all violations will be thrown
    /// </summary>
    /// <param name="value">the value to be validated</param>
    /// <returns>an exception provider which contains all validation errors, if any</returns>
    public IExceptionProvider Validate(T? value);
}

/// <summary>
/// A Profile which contains all validators for a given type<br/>
/// 
/// </summary>
/// <typeparam name="TValue"></typeparam>
internal sealed class ValidationProfile<TValue> : IValidationProfile<TValue>
{
    /// <summary>
    /// <inheritdoc cref="IValidationProfile{T}.TargetType"/>
    /// </summary>
    public Type TargetType { get; }
    private readonly IReadOnlyCollection<Func<TValue, IValidationContext<TValue>>> _validatorFactories;

    private static readonly ConcurrentDictionary<Type, ValidationProfile<TValue>> ProfileCache = new();

    /// <summary>
    /// creates a <see cref="ValidationProfile{TValue}"/> for the given <paramref name="targetType"/><br/>
    /// when validating, the profile will use all methods decorated with the <see cref="ValidationAttribute"/> in the <paramref name="targetType"/><br/> and all its base types.<br/>
    /// validators must follow this signature: <code>[<see cref="ValidationAttribute"/>>]</code>
    /// after the first call, instances will be reused.<br/>
    /// thread-safe.
    /// </summary>
    public static IValidationProfile<TValue> Get(Type targetType)
        => ProfileCache.GetOrAdd(targetType, _ => new(targetType));
    /// <summary>
    /// creates a <see cref="ValidationProfile{TValue}"/> for the given <typeparam name="TTargetType"/><br/>
    /// when validating, the profile will use all methods decorated with the <see cref="ValidationAttribute"/> in the <paramref name="targetType"/><br/> and all its base types.<br/>
    /// validators must follow this signature: <code>[<see cref="ValidationAttribute"/>>]</code>
    /// after the first call, instances will be reused.<br/>
    /// thread-safe.
    /// </summary>
    public static IValidationProfile<TValue> Get<TTargetType>()
        => ProfileCache.GetOrAdd(typeof(TTargetType), _ => new(typeof(TTargetType)));

    private ValidationProfile(Type targetValueType)
    {
        TargetType = targetValueType;
        const BindingFlags sharedFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        // issue: private static methods of base types are not returned by this method
        // fix: search for all base types and their non-public functions for private ones
        var validationMethods = targetValueType.GetMethods(
                BindingFlags.Public | sharedFlags)
            .Concat(
                targetValueType
                    .GetBaseTypeTree()
                    .Skip(1)// skips the targetValueType itself
                    .SelectMany(t => t.GetMethods(sharedFlags))
                )
            .Where(method => method.HasCustomAttribute<ValidationAttribute>()
                && method.DeclaringType == method.ReflectedType)
            .ToReadOnlyCollection();

        using var signatureErrors = new ExceptionBuilder($"Signature for validator in {targetValueType.FullName()}");
        foreach (var method in validationMethods)
        {
            using var methodErrors = signatureErrors.CreateChild(
                method.FullName(true, true, false));
            var parameters = method.GetParameters();
            var validationContextType = parameters.FirstOrDefault()?.ParameterType;
            if (parameters.Length != 1)
                methodErrors.Add("must have exactly one parameter");
            if (method.ReturnType != typeof(void))
                methodErrors.Add("must have a void return type");
            if (method.IsPrivate == false)
                methodErrors.Add("must be private");
            if (method.IsStatic == false)
                methodErrors.Add("must be static");
            if (method
                    .GetParameters()
                    .FirstOrDefault()
                    ?.ParameterType
                    .IsAssignableTo<IValidationContext<TValue>>() != true)
                methodErrors.Add("must have a single parameter assignable to " + typeof(IValidationContext<TValue>).FullName(true));

            if (validationContextType == typeof(IValidationContext<TValue>)) 
                continue; // we will just use the base implementation

            if (validationContextType?.Implements<IValidationContext<TValue>>() is false or null)
                methodErrors.Add("parameter is not assignable to " +
                                 typeof(IValidationContext<TValue>).FullName(true));
            if (validationContextType?.IsInstantiable() is false or null)
                methodErrors.Add("parameter can not be created (it is either static or an interface)");
            var validationContextCtors = validationContextType?.GetConstructors() ?? Array.Empty<ConstructorInfo>();
            if (validationContextCtors.Length != 1)
                methodErrors.Add($"the ValidationContext {validationContextType?.FullName()} should have only one constructor");
            var validationContextCtorParameters = 
                validationContextCtors.FirstOrDefault()?.GetParameters() 
               ?? Array.Empty<ParameterInfo>();
            if (validationContextCtorParameters.Length != 2)
                methodErrors.Add($"the ValidationContext {validationContextType?.FullName()} requires exactly 2 parameters");
            if (validationContextCtorParameters.ElementAtOrDefault(0)?.ParameterType != typeof(TValue))
                methodErrors.Add($"the first ctor parameter of ValidationContext {validationContextType?.FullName()} must be of type {typeof(TValue).FullName()}");
            if (validationContextCtorParameters.ElementAtOrDefault(1)?.ParameterType != typeof(Type))
                methodErrors.Add($"the second ctor parameter of ValidationContext {validationContextType?.FullName()} must be of type {typeof(Type).FullName()}");
        }

        _validatorFactories = validationMethods.Select(method =>
        {
            var validatorType = method.GetParameters().Single().ParameterType;
            if (validatorType == typeof(IValidationContext<TValue>))// using the base implementation as noted above
                validatorType = typeof(ValidationContext<TValue>);

            var valueParameter = Expression.Parameter(typeof(TValue), "value");

            var validatorVariable = Expression.Variable(validatorType, "validator");

            var doCheck = Expression.Block(
                new[]
                {
                    validatorVariable
                },
                Expression.Assign(validatorVariable,
                    Expression.New(validatorType.GetConstructors().Single(),
                            valueParameter,
                            Expression.Constant(targetValueType, typeof(Type))
                        )
                ),
                Expression.Call(null, method, validatorVariable),
                validatorVariable
            );

            var lambda = Expression.Lambda<Func<TValue, IValidationContext<TValue>>>(doCheck, valueParameter);

            return lambda.Compile();
        }).ToReadOnlyCollection();
    }


    IExceptionProvider IValidationProfile<TValue>.Validate(TValue? value)
    {
        var ex = new ExceptionBuilder(TargetType.FullName(true));
        ex.AddChildren(_validatorFactories
            .Select(validator => validator(value!))
            .ToReadOnlyCollection());
        return ex;
    }
}