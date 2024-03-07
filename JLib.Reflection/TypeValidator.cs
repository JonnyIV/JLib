using System.Reflection;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection.Exceptions;
using JLib.ValueTypes;

namespace JLib.Reflection;

public class TypeValidator : ValueValidator<Type>
{
    private readonly TypeValueType _valueType;

    public TypeValidator(TypeValueType valueType, string valueTypeName) : base(valueType.Value, valueTypeName)
    {
        _valueType = valueType;
    }

    protected override Exception? BuildException(IReadOnlyCollection<string> messages, IReadOnlyCollection<IExceptionProvider> provider)
        => JLibAggregateException.ReturnIfNotEmpty(
            $"{_valueType.Value.FullName(true)} is not a valid {_valueType.GetType().FullName(true)}",
            messages.Select(msg => new InvalidTypeException(_valueType.GetType(), _valueType.Value, msg)));

    public TypeValidator ValidateProperties(Func<PropertyInfo, bool> filter, Action<PropertyInfoValidator> validator)
    {
        foreach (var property in Value.GetProperties().Where(filter))
        {
            var val = new PropertyInfoValidator(property, ValueTypeName);
            validator(val);
            AddSubValidators(val);
        }
        return this;
    }

    public TypeValidator ShouldBeGeneric(string? hint = null)
    {
        if (!Value.IsGenericType)
            AddError(string.Join(Environment.NewLine, "Must be Generic", hint));
        return this;
    }
    public TypeValidator ShouldBeStatic(string? hint = null)
    {
        if (!Value.IsStatic())
            AddError(string.Join(Environment.NewLine, "Must be Static", hint));
        return this;
    }

    public TypeValidator ShouldBeSealed(string? hint = null)
    {
        if (!Value.IsSealed)
            AddError(string.Join(Environment.NewLine, "Must be Sealed", hint));
        return this;
    }

    public TypeValidator ShouldNotBeGeneric(string? hint = null)
    {
        if (Value.IsGenericType)
            AddError(string.Join(Environment.NewLine, "Must not be Generic", hint));
        return this;
    }

    public TypeValidator ShouldHaveNTypeArguments(int argumentCount)
    {
        ShouldBeGeneric();

        if (!Value.IsGenericType)
            AddError("Must be Generic");
        if (Value.GenericTypeArguments.Length != argumentCount)
            AddError(
                $"It must have exactly {argumentCount} type arguments but got {Value.GenericTypeArguments.Length}");
        return this;
    }
    
    public TypeValidator ShouldImplementAny<TInterface>(string? hint = null)
    {
        if (!Value.ImplementsAny<TInterface>())
            AddError($"Should implement any {typeof(TInterface).TryGetGenericTypeDefinition().FullName(true)}",
                hint);
        return this;
    }

    public TypeValidator ShouldImplement<TInterface>(string? hint = null)
    {
        if (!Value.ImplementsAny<TInterface>())
            AddError($"Should implement {typeof(TInterface).FullName(true)}", hint);
        return this;
    }

    public TypeValidator ShouldNotImplementAny<TInterface>(string? hint = null)
    {
        if (Value.ImplementsAny<TInterface>())
            AddError($"Should not implement {typeof(TInterface).TryGetGenericTypeDefinition().FullName(true)}",
                hint);
        return this;
    }
    public TypeValidator ShouldHaveAttribute<TAttribute>(string hint)
        where TAttribute : Attribute
    {
        if (!Value.HasCustomAttribute<TAttribute>())
            AddError($"Should have {typeof(TAttribute).FullName(true)}", hint);
        return this;
    }

    public TypeValidator ShouldHaveName(string name)
    {
        if (Value.Name != name)
            AddError($"must have the name '{name}'");
        return this;
    }
    public TypeValidator ShouldHaveNameSuffix(string nameSuffix)
    {
        if (!Value.Name.EndsWith(nameSuffix))
            AddError($"must have the nameSuffix '{nameSuffix}'");
        return this;
    }
}