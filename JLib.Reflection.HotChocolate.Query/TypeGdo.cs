using HotChocolate;
using JLib.Helper;

namespace JLib.Reflection.HotChocolate.Query;


/// <summary>
/// GraphQl Compatible <see cref="System.Type"/> wrapper
/// </summary>
[GraphQLDescription("Represents a wrapper class for System.Type with additional functionality.")]
public class TypeGdo
{
    /// <summary>
    /// Gets the name of the type.
    /// </summary>
    public string Name => _src.Name;

    /// <summary>
    /// Gets the type arguments of the generic type, if the type is generic.
    /// </summary>
    public List<TypeGdo>? TypeArguments
        => _src.IsGenericType
        ? _src.GetGenericArguments().Select(x => new TypeGdo(x)).ToList()
        : null;

    /// <summary>
    /// Gets the implemented interfaces of the type.
    /// </summary>
    public List<TypeGdo> ImplementedInterfaces
        => _src.GetInterfaces().Select(x => new TypeGdo(x)).ToList();

    /// <summary>
    /// Gets the namespace of the type.
    /// </summary>
    public string? Namespace => _src.Namespace;

    /// <summary>
    /// Gets the full name of the type.
    /// </summary>
    /// <param name="includeNamespace">Indicates whether to include the namespace in the full name.</param>
    /// <returns>The full name of the type.</returns>
    public string FullName(bool includeNamespace = false) => _src.FullName(includeNamespace);

    private readonly Type _src;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeGdo"/> class.
    /// </summary>
    /// <param name="src">The source <see cref="System.Type"/>.</param>
    public TypeGdo(Type src)
    {
        _src = src;
    }
}
