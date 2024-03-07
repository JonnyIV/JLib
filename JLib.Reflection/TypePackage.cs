using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using JLib.Helper;

namespace JLib.Reflection;

/// <summary>
/// Contains Content to be used by the <see cref="TypeCache"/>
/// </summary>
public sealed class TypePackage : ITypePackage
{
    private static Version? _version;

    public static ITypePackage Get(Assembly assembly, string? name = null)
    {
        var references = assembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(TypePackageProviderType.GetInstances);
        _version = assembly.GetName().Version;
        var root = new TypePackage(assembly.GetTypes(), references,
            name ?? assembly.GetName().Name ?? "Nameless Assembly with {Types} Types");

        return root;
    }

    public static ITypePackage Get(params Assembly[] assemblies)
        => Get(assemblies.CastTo<IReadOnlyCollection<Assembly>>());

    public static ITypePackage Get(IReadOnlyCollection<Assembly> assemblies)
        => new TypePackage(null, assemblies.Select(a => Get(a)), "{Children} Assemblies");

    public static ITypePackage Get(params Type[] types)
        => Get(types.CastTo<IReadOnlyCollection<Type>>());

    public static ITypePackage Get(IReadOnlyCollection<Type> types)
        => new TypePackage(types, null, "{Types} Types");

    /// <summary>
    /// creates a <see cref="ITypePackage"/> which contains all types nested in the given types, but not the types themselves.<br/>
    /// this can be usefull for testing purposes
    /// </summary>
    public static ITypePackage GetNested(params Type[] types)
        => new TypePackage(types.SelectMany(x => x.GetNestedTypes()), null,
            "nested types of " + string.Join(", ", types.Select(x => x.FullName())));

    /// <summary>
    /// creates a <see cref="ITypePackage"/> which contains all types nested in <typeparamref nameTemplate="T"/> but not <typeparamref nameTemplate="T"/> itself.
    /// </summary>
    public static ITypePackage GetNested<T>()
        => GetNested(typeof(T));

    public static ITypePackage Get(IEnumerable<Assembly> assemblies, IEnumerable<Type> types,
        string name = "{Children} Assemblies and {Types} types")
        => new TypePackage(types, assemblies.Select(a => Get(a)), name);

    public static ITypePackage Get(params ITypePackage[] packages)
        => Get(packages.AsEnumerable());

    public static ITypePackage Get(IEnumerable<ITypePackage> packages, string name = "{Children} Packages")
        // ReSharper disable  PossibleMultipleEnumeration
        => packages.Multiple()
            ? new TypePackage(
                null,
                packages,
                name)
            : packages.Single();
    // ReSharper restore PossibleMultipleEnumeration

    /// <summary>
    /// creates a <see cref="ITypePackage"/> which contains all types of all assemblies in the given directory which match any of the given prefixes.
    /// </summary>
    /// <param name="directory">defaults to <see cref="AppDomain.CurrentDomain"/>.BaseDirectory</param>
    /// <param name="includedPrefixes"></param>
    /// <param name="searchOption"></param>
    /// <returns></returns>
    public static ITypePackage Get(
        string? directory,
        string[] includedPrefixes,
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        directory ??= AppDomain.CurrentDomain.BaseDirectory;
        var assemblyNames = Directory.EnumerateFiles(directory, "*.dll", searchOption).Where(file =>
        {
            var filename = Path.GetFileName(file);
            return includedPrefixes.Any(p => filename.StartsWith(p));
        }).Select(AssemblyName.GetAssemblyName).ToArray();

        var assemblies = assemblyNames.Select(Assembly.Load).ToArray();
        return Get(assemblies);
    }

    private TypePackage(IEnumerable<Type>? types, IEnumerable<ITypePackage>? children, string nameTemplate, Version? version = null)
    {
        Types = types ?? Enumerable.Empty<Type>();
        Children = children ?? Array.Empty<ITypePackage>();
        DescriptionTemplate = nameTemplate;
        _content = new(() => GetContent(this).ToImmutableHashSet());
    }


    public IEnumerable<ITypePackage> Children { get; }
    public IEnumerable<Type> Types { get; }
    public string DescriptionTemplate { get; }
    private readonly Lazy<ImmutableHashSet<Type>> _content;
    public ImmutableHashSet<Type> GetContent() => _content.Value;

    private static IEnumerable<Type> GetContent(ITypePackage package)
        => package.Children.SelectMany(GetContent).Concat(package.Types).Distinct();

    public ITypePackage Combine(params ITypePackage[] packages)
        => Get(packages.Append(this));

    public override string ToString()
        => ToString(false);

    public string ToString(bool includeTypes, bool includeVersion = false)
    {
        var sb = new StringBuilder();
        ToString(this, 0, sb, includeTypes, includeVersion);
        return sb.ToString();
    }

    static void ToString(ITypePackage package, int indent, StringBuilder sb, bool includeTypes, bool includeVersion)
    {
        var indentStr = new string(' ', indent * 2);
        sb.Append(indentStr).Append('┐').AppendLine(package.DescriptionTemplate
            .Replace("{Children}", package.Children.Count().ToString())
            .Replace("{Types}", package.Types.Count().ToString())
        );
        if (includeVersion && _version is not null)
            sb.Append(indentStr).Append("├ Version:").AppendLine(_version.ToString());
        sb.Append(indentStr).Append("├ Types:").AppendLine(package.Types.Count().ToString());
        if (package.Types.Count() <= 10 || includeTypes)
        {
            foreach (var type in package.Types)
                sb.Append(indentStr).Append("│   ").AppendLine(type.FullName());
        }
        if (package.Children.Any())
        {
            sb.Append(indentStr).AppendLine("├ Children:");
            foreach (var child in package.Children)
                ToString(child, indent + 1, sb, includeTypes, includeVersion);
        }
    }
}