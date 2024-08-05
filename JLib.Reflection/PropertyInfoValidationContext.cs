using System.Reflection;
using JLib.Helper;
using JLib.ValueTypes;

namespace JLib.Reflection;

public static class PropertyInfoValidationContextHelper
{
    public static IValidationContext<PropertyInfo> HaveName(this IValidationContext<PropertyInfo> context, string name)
    {
        if (context.Value.Name != name)
            context.Validate($"must have the name '{name}'");
        return context;
    }
    public static IValidationContext<PropertyInfo> HaveNameSuffix(this IValidationContext<PropertyInfo> context, string nameSuffix)
    {
        if (!context.Value.Name.EndsWith(nameSuffix))
            context.Validate($"must have the nameSuffix '{nameSuffix}'");
        return context;
    }
    public static IValidationContext<PropertyInfo> HavePublicInit(this IValidationContext<PropertyInfo> context)
    {
        if (!context.Value.IsInit())
            context.Validate("must have public static init");
        return context;
    }
    public static IValidationContext<PropertyInfo> HavePublicSet(this IValidationContext<PropertyInfo> context)
    {
        if (context.Value.SetMethod?.IsPublic != true)
            context.Validate("must have public static set");
        return context;
    }

    public static IValidationContext<PropertyInfo> BeOfType(this IValidationContext<PropertyInfo> context, Type propertyType)
    {
        if (context.Value.ReflectedType != propertyType)
            context.Validate($"must be of type {propertyType.FullName()}");
        return context;
    }


    public static IValidationContext<PropertyInfo> BeOfType<T>(this IValidationContext<PropertyInfo> context)
        => context.BeOfType(typeof(T));
    public static IValidationContext<PropertyInfo> HaveNoSet(this IValidationContext<PropertyInfo> context)
    {
        if (context.Value.SetMethod?.IsPrivate != true)
            context.Validate("must have no set");
        return context;
    }
    public static IValidationContext<PropertyInfo> HavePublicGet(this IValidationContext<PropertyInfo> context)
    {
        if (context.Value.GetMethod?.IsPublic != true)
            context.Validate("must have public static get");
        return context;
    }
    public static IValidationContext<PropertyInfo> BeStatic(this IValidationContext<PropertyInfo> context)
    {
        if (context.Value.GetMethod?.IsPublic != true)
            context.Validate("must have public static get");
        return context;
    }
    public static IValidationContext<PropertyInfo> BeTheOnlyProperty(this IValidationContext<PropertyInfo> context)
    {
        if (context.Value.ReflectedType?.GetProperties().Length != 1)
            context.Validate("must have only this property");
        return context;
    }
    public static IValidationContext<PropertyInfo> HaveAttribute<TAttribute>(this IValidationContext<PropertyInfo> context, string hint)
        where TAttribute : Attribute
    {
        if (!context.Value.HasCustomAttribute<TAttribute>())
            context.Validate($"Should have {typeof(TAttribute).FullName(true)}", hint);
        return context;
    }

}