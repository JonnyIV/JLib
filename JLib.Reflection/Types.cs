using JLib.Helper;
using JLib.ValueTypes;
using static JLib.Reflection.TvtFactoryAttribute;

namespace JLib.Reflection;

/// <summary>
/// <see cref="ValueType"/> for <see cref="Type"/>s
/// </summary>
/// <param name="Value"></param>
[DerivedFromAny(typeof(ValueType<>))]
public record ValueTypeType(Type Value) : TypeValueType(Value), IValidatedType
{

    public Type NativeType
    {
        get
        {
            try
            {
                return Value.GetAnyBaseType<ValueType<Ignored>>()?.GenericTypeArguments.First()!;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public bool Mapped => !Value.HasCustomAttribute<UnmappedAttribute>() && !Value.IsAbstract;

    void IValidatedType.Validate(ITypeCache cache, TypeValidationContext value)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (NativeType is null)
            value.Validate("the NativeType could not be found");
    }
}