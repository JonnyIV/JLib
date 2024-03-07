using System.Reflection;
using JLib.Exceptions;
using JLib.Helper;

namespace JLib.Reflection;

[TvtFactoryAttribute.HasAttribute(typeof(TypePackageProviderAttribute))]
public record TypePackageProviderType(Type Value) : TypeValueType(Value), IValidatedType
{
    public const string InstancePropertyName = "Instance";

    /// <summary>
    /// returns the Instance of the TypePackageProvider
    /// </summary>
    public static ITypePackage GetInstance(Type typePackageProviderType)
        => typePackageProviderType.GetProperty(InstancePropertyName, typeof(ITypePackage))
               ?.GetValue(null)?.As<ITypePackage>()
           ?? throw new InvalidSetupException($"Instance of Type Package {typePackageProviderType.FullName()} could not be retrieved. it should be named {InstancePropertyName} and be of type {nameof(ITypePackage)}.");

    /// <summary>
    /// returns the <see cref="ITypePackage"/> for the current assembly. It must be declared as follows:
    /// <example><code>
    /// using JLib.Reflection;
    /// 
    /// namespace [[Namespace]];
    /// 
    /// [TypePackageProvider]
    /// public static class [[AssemblyName]]Tp
    /// {
    ///     public static ITypePackage Instance { get; }
    ///         = TypePackage.Get(typeof([[AssemblyName]]Tp).Assembly);
    /// }
    /// </code></example>
    /// </summary>
    public static IEnumerable<ITypePackage> GetInstances(Assembly assembly) 
        => assembly.GetTypes()
            .Where(t => t.HasCustomAttribute<TypePackageProviderAttribute>())
            .Select(GetInstance)
            .WhereNotNull();

    public void Validate(ITypeCache cache, TypeValidator value)
    {
        value.ShouldBeStatic()
            .ShouldHaveNameSuffix("Tp")
            .ValidateProperties(_ => true, p => p
                .ShouldHaveName(InstancePropertyName)
                .ShouldHavePublicGet()
                .ShouldHaveNoSet()
                .ShouldBeOfType<ITypePackage>()
                .ShouldBeTheOnlyProperty()
                .ShouldBeStatic()
            );
    }
}