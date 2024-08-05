using System.ComponentModel.DataAnnotations;
using System.Reflection;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection.Exceptions;
using JLib.ValueTypes;
using ValueType = JLib.ValueTypes.ValueType;

namespace JLib.Reflection;

public class TypeValidationContext : ValidationContext<Type>
{
    private readonly TypeValueType _valueType;

    public TypeValidationContext(TypeValueType valueType, Type targetType) : base(valueType.Value, targetType)
    {
        _valueType = valueType;
    }

    protected override Exception? BuildException(IReadOnlyCollection<string> messages, IReadOnlyCollection<IExceptionProvider> provider)
        => JLibAggregateException.ReturnIfNotEmpty(
            $"{_valueType.Value.FullName(true)} is not a valid {_valueType.GetType().FullName(true)}",
            messages.Select(msg => new InvalidTypeException(_valueType.GetType(), _valueType.Value, msg)));

    public TypeValidationContext ValidateProperties(Func<PropertyInfo, bool> filter, Action<ValidationContext<PropertyInfo>> validator)
    {
        foreach (var property in Value.GetProperties().Where(filter))
        {
            var val = new ValidationContext<PropertyInfo>(property, TargetType);
            validator(val);
            AddSubValidators(val);
        }
        return this;
    }

    public TypeValidationContext ShouldBeGeneric(string? hint = null)
    {
        if (!Value.IsGenericType)
            Validate(string.Join(Environment.NewLine, "Must be Generic", hint));
        return this;
    }
    public TypeValidationContext ShouldBeStatic(string? hint = null)
    {
        if (!Value.IsStatic())
            Validate(string.Join(Environment.NewLine, "Must be Static", hint));
        return this;
    }

    public TypeValidationContext ShouldBeSealed(string? hint = null)
    {
        if (!Value.IsSealed)
            Validate(string.Join(Environment.NewLine, "Must be Sealed", hint));
        return this;
    }

    public TypeValidationContext ShouldNotBeGeneric(string? hint = null)
    {
        if (Value.IsGenericType)
            Validate(string.Join(Environment.NewLine, "Must not be Generic", hint));
        return this;
    }

    public TypeValidationContext ShouldHaveNTypeArguments(int argumentCount)
    {
        ShouldBeGeneric();

        if (!Value.IsGenericType)
            Validate("Must be Generic");
        if (Value.GenericTypeArguments.Length != argumentCount)
            Validate(
                $"It must have exactly {argumentCount} type arguments but got {Value.GenericTypeArguments.Length}");
        return this;
    }
    
    public TypeValidationContext ShouldImplementAny<TInterface>(string? hint = null)
    {
        if (!Value.ImplementsAny<TInterface>())
            Validate($"Should implement any {typeof(TInterface).TryGetGenericTypeDefinition().FullName(true)}",
                hint);
        return this;
    }

    public TypeValidationContext ShouldImplement<TInterface>(string? hint = null)
    {
        if (!Value.ImplementsAny<TInterface>())
            Validate($"Should implement {typeof(TInterface).FullName(true)}", hint);
        return this;
    }

    public TypeValidationContext ShouldNotImplementAny<TInterface>(string? hint = null)
    {
        if (Value.ImplementsAny<TInterface>())
            Validate($"Should not implement {typeof(TInterface).TryGetGenericTypeDefinition().FullName(true)}",
                hint);
        return this;
    }
    public TypeValidationContext ShouldHaveAttribute<TAttribute>(string hint)
        where TAttribute : Attribute
    {
        if (!Value.HasCustomAttribute<TAttribute>())
            Validate($"Should have {typeof(TAttribute).FullName(true)}", hint);
        return this;
    }

    public TypeValidationContext ShouldHaveName(string name)
    {
        if (Value.Name != name)
            Validate($"must have the name '{name}'");
        return this;
    }
    public TypeValidationContext ShouldHaveNameSuffix(string nameSuffix)
    {
        if (!Value.Name.EndsWith(nameSuffix))
            Validate($"must have the nameSuffix '{nameSuffix}'");
        return this;
    }
}