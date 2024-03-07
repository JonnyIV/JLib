using JLib.Helper;

namespace JLib.Reflection;
public static class TypeExtensions
{
    public static Type MakeGenericType(this Type type, params ITypeValueType[] genericParams)
    {
        var typeArguments = genericParams.Select(x => x.Value).ToArray();
        try
        {
            return type.MakeGenericType(typeArguments);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"adding <{string.Join(", ", typeArguments.Select(x => x.FullName(true)))}> as typeArguments to type {type.FullName(true)} failed: {Environment.NewLine}{e.Message}",
                e);
        }
    }
    public static TTvt CastValueType<TTvt>(this Type type, ITypeCache cache)
        where TTvt : TypeValueType
        => cache.Get<TTvt>(type);

    public static TTvt? AsValueType<TTvt>(this Type type, ITypeCache cache)
        where TTvt : TypeValueType
        => cache.TryGet<TTvt>(type);
}
