using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace JLib.Helper;

public static class TypeHelper
{
    public static bool IsString(this Type type)
        => type == typeof(string);
    public static bool IsGuid(this Type type)
        => type == typeof(Guid);
    public static bool IsNullableGuid(this Type type)
        => type == typeof(Guid?);

    public static bool IsNumber(this Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => true,
            TypeCode.SByte => true,
            TypeCode.UInt16 => true,
            TypeCode.UInt32 => true,
            TypeCode.UInt64 => true,
            TypeCode.Int16 => true,
            TypeCode.Int32 => true,
            TypeCode.Int64 => true,
            TypeCode.Decimal => true,
            TypeCode.Double => true,
            TypeCode.Single => true,
            _ => false
        };
    }

    public static bool IsNullableNumber(this Type type)
        => Nullable.GetUnderlyingType(type)?.IsNumber() is true;

    public static IReadOnlyCollection<Type> GetDeclaringTypeTree(this Type type)
    {
        var cur = type;
        List<Type> res = new();
        do
        {
            res.Add(cur);
            cur = cur.DeclaringType;
        } while (cur is not null);

        return res;
    }
    public static IReadOnlyCollection<Type> GetBaseTypeTree(this Type type)
    {
        var cur = type;
        List<Type> res = new();

        do
        {
            res.Add(cur);
            cur = cur.BaseType;
        } while (cur is not null);

        return res;
    }

    /// <summary>
    /// a save variant of <see cref="TryGetGenericTypeDefinition"/>. If the type is not generic, it will simply return the given type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Type TryGetGenericTypeDefinition(Type type)
    {
        return !type.IsGenericType
            ? type
            : type.GetGenericTypeDefinition();
    }

    /// <summary>
    /// checks whether the given type is derived from the other type, ignoring all type parameters
    /// </summary>
    public static bool IsDerivedFromAny<T>(this Type type)
        => type.GetAnyBaseType<T>() is not null;

    /// <summary>
    /// <inheritdoc cref="IsDerivedFromAny{T}"/>
    /// </summary>
    public static bool IsDerivedFromAny(this Type type, Type baseType)
        => type.GetAnyBaseType(baseType) is not null;

    public static Type? GetAnyBaseType<T>(this Type type)
        => type.GetAnyBaseType(typeof(T));

    public static Type? GetAnyBaseType(this Type type, Type baseType)
    {
        var current = type;
        var compare = baseType.TryGetGenericTypeDefinition();
        while (current is not null && current.TryGetGenericTypeDefinition() != compare)
            current = current.BaseType;
        return current == compare
            ? null
            : current;
    }

    /// <summary>
    ///     checks if the type is an static class (i.e. abstract and sealed)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsStatic(this Type type)
        => type is { IsClass: true, IsAbstract: true, IsSealed: true };

    public static bool IsAssignableTo<T>(this Type type)
        => type.IsAssignableTo(typeof(T));

    /// <summary>
    /// if T is generic, the type-parameters will be ignored. Use the <see cref="Ignored"/> type in this case when possible<br/>
    /// if you don't want it to be ignored, use <see cref="Implements{T}"/> instead
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool ImplementsAny<T>(this Type type)
        => type.GetInterface(typeof(T).Name) is not null || type.IsInterface &&
            (type.TryGetGenericTypeDefinition()) == (typeof(T).TryGetGenericTypeDefinition());

    public static bool ImplementsAny(this Type type, Type @interface)
        => type.GetInterface(@interface.Name) is not null;

    /// <summary>
    /// if T is generic, the type-parameters will NOT be ignored.<br/>
    /// if you want it to be ignored, use <see cref="ImplementsAny{T}"/> instead
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool Implements<T>(this Type type)
        => type.Implements(typeof(T));

    public static bool Implements(this Type type, Type @interface)
        => type.GetInterface(@interface.Name) == @interface;

    public static IEnumerable<Type> WhichImplement<T>(this IEnumerable<Type> collection)
        => collection.Where(t => t.ImplementsAny<T>());

    public static IEnumerable<Type> WhichAreAssignableTo<T>(this IEnumerable<Type> collection)
        => collection.WhichAreAssignableTo(typeof(T));

    public static IEnumerable<Type> WhichAreAssignableTo(this IEnumerable<Type> collection, Type type)
        => collection.Where(t => t.IsAssignableTo(type));

    public static IEnumerable<Type> WhichAreInstantiable(this IEnumerable<Type> collection)
        => collection.Where(t => !t.IsAbstract && !t.IsInterface);

    public static Type? GetInterface<TInterface>(this Type type)
    {
        var tInt = typeof(TInterface);
        return type.GetInterface(tInt.FullName ?? tInt.Name);
    }

    public static Type? GetAnyInterface<TInterface>(this Type type)
    {
        var @interface = typeof(TInterface).TryGetGenericTypeDefinition();
        return type.TryGetGenericTypeDefinition() == @interface && type.IsInterface
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.TryGetGenericTypeDefinition() == @interface);
    }

    public static bool HasSameGenericTypeDefinition<T>(this Type type)
        => HasSameGenericTypeDefinition(type, typeof(T));

    public static bool HasSameGenericTypeDefinition(this Type type, Type other)
    {
        var t1 = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var t2 = other.IsGenericType ? other.GetGenericTypeDefinition() : other;
        return t1 == t2;
    }

    /// <summary>
    /// applies the given type parameters if they are applicable for the type
    /// <br/>if the type is not generic, the type is returned
    /// <br/>if the type arguments do not match but the type is generic, an exception will be thrown
    /// <br/>if the type is generic but the arguments are already filled, the arguments will be replaced
    /// </summary>
    public static Type TryMakeGenericType(this Type type, params Type[] genericParams)
    {
        type = type.TryGetGenericTypeDefinition();
        return type.IsGenericTypeDefinition
            ? type.GenericTypeArguments.SequenceEqual(genericParams)
                ? type.MakeGenericType(genericParams)
                : throw new InvalidOperationException("the type arguments do not match")
            : type;
    }

    /// <summary>
    /// Returns the full name of the method, including the class name, generic parameters or arguments, and parameter names.
    /// </summary>
    /// <param name="method">The <see cref="MethodBase"/> representing the method.</param>
    /// <param name="includeDeclaringType">Specifies whether to include the declared type name in the full method name.</param>
    /// <param name="includeReflectedType">Specifies whether to include the reflected type name in the full method name.</param>
    /// <param name="includeParameterName">Specifies whether to include the parameter names in the full method name.</param>
    /// <param name="includeNamespace">Specifies whether to include the namespace in the class name.</param>
    /// <returns>The full name of the method.</returns>
    public static string FullName(this MethodBase method, bool includeDeclaringType, bool includeReflectedType, bool includeParameterName, bool includeNamespace = false)
    {
        var result = new StringBuilder();
        // className
        if (includeReflectedType)
            result.Append(method.ReflectedType?.FullName(includeNamespace));
        if (includeDeclaringType && includeReflectedType)
            result.Append(':');
        if (includeDeclaringType)
            result.Append(method.ReflectedType?.FullName(includeNamespace));
        if (includeDeclaringType || includeReflectedType)
            result.Append('.');


        // name
        result.Append(method.Name);

        // generic parameters or arguments
        if (method.IsGenericMethodDefinition)
            result.Append('<')
                .Append(new string(',', method.GetGenericArguments().Length-1))
                .Append('>');
        else if (method.IsGenericMethod)
            result.Append('<')
                .Append(string.Join(", ", method.GetGenericArguments().Select(a => a.FullName(includeNamespace))))
                .Append('>');

        // parameters
        result.Append('(')
            .AppendJoin(", ", method.GetParameters()
                .Select(parameter =>

                    parameter.ParameterType.FullName(includeNamespace) +
                        (includeParameterName ? parameter.Name : "")
                ))
            .Append(')');

        return result.ToString();
    }

    public static IReadOnlyCollection<Type> GetNestingParents(this Type type)
    {
        var result = new List<Type>();
        var current = type;
        while (current.DeclaringType is not null)
        {
            result.Add(current.DeclaringType);
            current = current.DeclaringType;
        }
        return result;
    }

    /// <summary>
    /// the name of the class and its declaring type, excluding the namespace
    /// </summary>
    public static string FullName(this Type type, bool includeNamespace = false)
    {
        var name = (type.FullName ?? type.Name);
        string res = name
            .Split("[")
            .First();
        ;
        if (!includeNamespace)
            res = res.Split(".").Last();
        res = res
            .Replace("+", ".")
            .Split("`").First();

        if (type.IsGenericType)
            res += $"<{string.Join(", ", type.GenericTypeArguments.Select(a => a.FullName(includeNamespace)))}>";

        return res;
    }

}