using System.Reflection;
using JLib.DataProvider;
using JLib.Helper;
using JLib.Reflection;
using JLib.ValueTypes;
using static JLib.Reflection.TvtFactoryAttribute;

namespace JLib.HotChocolate;

[Implements(typeof(IQueryDataObject)), IsClass, NotAbstract]
public record QueryDataObjectType(Type Value) : DataObjectType(Value), IValidatedType
{
    public void Validate(ITypeCache cache, TypeValidationContext value)
    {
        var ctors = Value.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (ctors.Length == 1)
        {
            var ctor = ctors.Single();
            if (ctor.GetParameters().Any())
                value.Validate("parameters found on the only constructor. A parameterless cosntructor is required");
        }
        else
        {
            var propsToInitialize = Value.GetProperties().Any(prop =>
                prop.CanWrite && !prop.IsNullable() && !prop.PropertyType.ImplementsAny<IEnumerable<Ignored>>());
            var hasPublicParameterlessCtor = ctors.Any(ctor => ctor.GetParameters().None() && ctor.IsPublic);
            if (propsToInitialize && hasPublicParameterlessCtor)
                value.Validate("found a public parameterless ctor despite having non-nullable properties");
        }
    }
}
