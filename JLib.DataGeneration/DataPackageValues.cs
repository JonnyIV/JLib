using System.Diagnostics;
using System.Reflection;
using JLib.Helper;
using JLib.ValueTypes;

namespace JLib.DataGeneration;

/// <summary>
/// Provides a set of value types for data package properties.
/// </summary>
public static class DataPackageValues
{
    /// <summary>
    /// Runtime id method calls are counted per IdScopeName.
    /// </summary>
    /// <param name="Value">the name of the scope</param>
    public record IdScopeName(string Value) : StringValueType(Value);

    /// <summary>
    /// Represents the name of an id.
    /// </summary>
    public record IdName(string Value) : StringValueType(Value)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdName"/> class with the specified property.
        /// </summary>
        /// <param name="property">The property to get the name from.</param>
        public IdName(PropertyInfo property) : this(property.Name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdName"/> class with the specified method and call number.
        /// </summary>
        /// <param name="scopeName"><inheritdoc cref="IdScopeName"/></param>
        /// <param name="method">The method to get the full name from.</param>
        /// <param name="callNumber">The call number.</param>
        public IdName(IdScopeName? scopeName, MethodBase method, int callNumber)
            : this((scopeName is null
                       ? ""
                       : $"[{scopeName.Value}]"
                )
                   + $"{method.FullName(false, false, false, true)}-{callNumber}")
        { }
    }

    /// <summary>
    /// Represents the name of a data package.
    /// </summary>
    public record IdGroupName(string Value) : StringValueType(Value)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdGroupName"/> class with the specified type.
        /// </summary>
        /// <param name="type">The type to get the full name from.</param>
        public IdGroupName(Type type)
            : this(type.FullName(true))
        {
        }

        internal IdGroupName(DataPackage dataPackage)
            : this(dataPackage.GetType())
        {
        }

        private static string ExtractKey(PropertyInfo property)
        {
            if (property.DeclaringType is null)
                return "No declaring type found";
            if (property.ReflectedType is null)
                return "No reflected type found";

            if (property.DeclaringType == property.ReflectedType)
                return property.ReflectedType.FullName(true);

            var baseTypeTree = property.ReflectedType
                .GetBaseTypeTree()
                .TakeWhile(t => t != property.DeclaringType)
                .Append(property.DeclaringType)
                .Select(t => t.FullName(true));

            return string.Join(":", baseTypeTree);
        }

        internal IdGroupName(PropertyInfo property) : this(ExtractKey(property))
        {
        }
    }

    /// <summary>
    /// Represents an identifier for a data package property.
    /// </summary>
    public record IdIdentifier(IdGroupName IdGroupName, IdName IdName)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdIdentifier"/> class with the specified property.
        /// </summary>
        /// <param name="property">The property to create the identifier from.</param>
        public IdIdentifier(PropertyInfo property) : this(new(property), new(property))
        {
        }

        /// <summary>
        /// Returns a string that represents the current identifier.
        /// </summary>
        /// <returns>A string that represents the current identifier.</returns>
        public override string ToString() => $"[{IdGroupName.Value}].[{IdName.Value}]";
    }
}
