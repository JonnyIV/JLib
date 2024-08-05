using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using JLib.Exceptions;
using JLib.Helper;

namespace JLib.ValueTypes;

/// <summary>
/// represents a class which validates a value of type <typeparamref name="TValue"/><br/>
/// requires a constructor with the signature <code>public <see cref="ValidationContext{TValue}"/>(<typeparamref name="TValue"/> value, <see cref="string"/> valueTypeName)</code>
/// </summary>
/// <typeparam name="TValue">the type of the <see cref="Value"/> to be validated</typeparam>
public interface IValidationContext<out TValue> : IExceptionProvider
{
    /// <summary>
    /// the type this value will be assigned to
    /// </summary>
    public Type TargetType { get; }
    /// <summary>
    /// adds the <paramref name="message"/> and <paramref name="hint"/> to the <see cref="Messages"/>
    /// </summary>
    /// <param name="message">the message to be added</param>
    /// <param name="hint"></param>
    void Validate(string message, string? hint = null);
    /// <summary>
    /// Adds the given <paramref name="subProvider"/> as a validator to this one.<br/>
    /// This might be used to validate complex types (like <see cref="Type"/>)
    /// </summary>
    /// <param name="subProvider"></param>
    void AddSubValidators(IExceptionProvider subProvider);

}

/// <summary>
/// <see cref="IExceptionProvider"/> for validating values of type <typeparamref name="TValue"/><br/>
/// often used to validate <see cref="ValueType{TValue}"/>s
/// </summary>
public class ValidationContext<TValue> : IValidationContext<TValue>
{
    /// <summary>
    /// <inheritdoc cref="IValidationContext{TValue}.TargetType"/>
    /// </summary>
    public Type TargetType { get; }
    /// <summary>
    /// the value which is currently being validated
    /// </summary>
    private readonly List<Expression<Func<TValue, string?>>> _messages = new();
    private readonly List<IExceptionProvider> _subValidators = new();

    public ValidationContext(Type targetType)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// adds an <see cref="Exception"/> with the given <paramref name="message"/> as <see cref="Exception.Message"/> to the validation result
    /// </summary>
    /// <param name="message">the <see cref="Exception.Message"/></param>
    /// <param name="hint">a hint appended to the <see cref="Exception.Message"/> which might help the user to resolve this issue</param>
    public void Validate(Expression<Func<TValue,string?>> getError)
    {
        _messages.Add(getError);
    }

    /// <summary>
    /// adds the <paramref name="subProvider"/> as a sub validator to this validator
    /// </summary>
    /// <param name="subProvider"></param>
    public void AddSubValidators(IExceptionProvider subProvider)
        => _subValidators.Add(subProvider);

    public Expression<Func<string, Exception>> ExceptionFactory()
    {

    }

    public Expression<Func<string, bool>> IsValid()
    {

    }
}