using System.Reflection;
using JLib.Helper;
using JLib.ValueTypes;

namespace JLib.Reflection;

public class PropertyInfoValidator : ValueValidator<PropertyInfo>
{
    public PropertyInfoValidator(PropertyInfo value, string valueTypeName) 
        : base(value, valueTypeName)
    {
    }

    public PropertyInfoValidator ShouldHaveName(string name)
    {
        if (Value.Name != name)
            AddError($"must have the name '{name}'");
        return this;
    }
    public PropertyInfoValidator ShouldHaveNameSuffix(string nameSuffix)
    {
        if (!Value.Name.EndsWith(nameSuffix))
            AddError($"must have the nameSuffix '{nameSuffix}'");
        return this;
    }
    public PropertyInfoValidator ShouldHavePublicInit()
    {
        if (!Value.IsInit())
            AddError("must have public init");
        return this;
    }
    public PropertyInfoValidator ShouldHavePublicSet()
    {
        if (Value.SetMethod?.IsPublic != true)
            AddError("must have public set");
        return this;
    }

    public PropertyInfoValidator ShouldBeOfType(Type propertyType)
    {
        if (Value.ReflectedType != propertyType)
            AddError($"must be of type {propertyType.FullName()}");
        return this;
    }


    public PropertyInfoValidator ShouldBeOfType<T>()
        => ShouldBeOfType(typeof(T));
    public PropertyInfoValidator ShouldHaveNoSet()
    {
        if (Value.SetMethod?.IsPrivate != true)
            AddError("must have no set");
        return this;
    }
    public PropertyInfoValidator ShouldHavePublicGet()
    {
        if (Value.GetMethod?.IsPublic != true)
            AddError("must have public get");
        return this;
    }
    public PropertyInfoValidator ShouldBeStatic()
    {
        if (Value.GetMethod?.IsPublic != true)
            AddError("must have public get");
        return this;
    }
    public PropertyInfoValidator ShouldBeTheOnlyProperty()
    {
        if (Value.ReflectedType?.GetProperties().Length != 1)
            AddError("must have only this property");
        return this;
    }
    public void ShouldHaveAttribute<TAttribute>(string hint)
        where TAttribute : Attribute
    {
        if (!Value.HasCustomAttribute<TAttribute>())
            AddError($"Should have {typeof(TAttribute).FullName(true)}", hint);
    }

}