using System.Reflection;
using JLib.Exceptions;
using JLib.Helper;

namespace JLib.Reflection;

/// <summary>
/// classes with this given attribute will not be ignored by the typeCache
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreInCache : Attribute
{
}

/// <summary>
/// used by the <seealso cref="TvtFactoryAttribute.FactoryAttribute"/> to apply a custom factory to this value type
/// </summary>
public interface ITypeValueTypeFilter
{
    bool Filter(Type type);
}
public abstract class TvtFactoryAttribute : Attribute
{
    public abstract bool Filter(Type type);

    /// <summary>
    /// lowest wins
    /// <br/>default is 10_000
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PriorityAttribute : Attribute
    {
        public int Value { get; }

        public PriorityAttribute(int value)
        {
            Value = value;
        }

        public const int DefaultPriority = 10_000;
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FactoryAttribute : TvtFactoryAttribute
    {
        private readonly ITypeValueTypeFilter _factory;

        /// <summary>
        /// type has to be of type <see cref="ITypeValueTypeFilter"/>
        /// </summary>
        /// <param name="type">the filter to be applied. requires an empty constructor.</param>
        public FactoryAttribute(Type type)
        {
            var ctor = type.GetConstructor(Array.Empty<Type>())
                ?? throw new InvalidSetupException(
                    $"Type {type.FullName(true)} does not have an empty constructor");

            _factory = ctor.Invoke(null) as ITypeValueTypeFilter
                ?? throw new InvalidSetupException(
                    $"Type {type.FullName(true)} does not implement {nameof(ITypeValueTypeFilter)}");
        }

        public override bool Filter(Type type) => _factory.Filter(type);
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class IsInterfaceAttribute : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => type.IsInterface;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class IsClassAttribute : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => type.IsClass;
    }

    public class HasInterfaceWithAttributeAttribute : TvtFactoryAttribute
    {
        private readonly Type _attributeType;

        public HasInterfaceWithAttributeAttribute(Type attributeType)
        {
            _attributeType = attributeType;
        }

        public override bool Filter(Type type)
            => type.GetInterfaces().Any(i => i.HasCustomAttribute(_attributeType));
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class HasAttributeAttribute : TvtFactoryAttribute
    {
        public HasAttributeAttribute(Type attributeType)
        {
            AttributeType = attributeType;
        }

        public Type AttributeType { get; }

        public override bool Filter(Type type)
            => type.HasCustomAttribute(AttributeType);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NotAbstractAttribute : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => !type.IsAbstract;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class BeGenericAttribute : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => type.IsGenericType;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NotGenericAttribute : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => !type.IsGenericType;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DerivedFromAnyAttribute : TvtFactoryAttribute
    {
        private readonly Type _type;

        public DerivedFromAnyAttribute(Type type)
        {
            _type = type;
        }

        public override bool Filter(Type type)
            => type.IsDerivedFromAny(_type);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IsAssignableToAttribute : TvtFactoryAttribute
    {
        private readonly Type _type;

        public IsAssignableToAttribute(Type type)
        {
            _type = type;
        }

        public override bool Filter(Type type)
            => type.IsAssignableTo(_type);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IsDerivedFromAttribute : TvtFactoryAttribute
    {
        private readonly Type _type;

        public IsDerivedFromAttribute(Type type)
        {
            _type = type;
        }

        public override bool Filter(Type type)
            => type.IsAssignableTo(_type) && type != _type;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ImplementsAttribute : TvtFactoryAttribute
    {
        private readonly Type _type;

        public ImplementsAttribute(Type type)
        {
            _type = type;
        }

        public override bool Filter(Type type)
            => type.Implements(_type);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ImplementsAnyAttribute : TvtFactoryAttribute
    {
        private readonly Type _type;

        public ImplementsAnyAttribute(Type type)
        {
            _type = type;
        }

        public override bool Filter(Type type)
            => type.ImplementsAny(_type);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ImplementsNoneAttribute : TvtFactoryAttribute
    {
        private readonly Type _type;

        public ImplementsNoneAttribute(Type type)
        {
            _type = type;
        }

        public override bool Filter(Type type)
            => !type.ImplementsAny(_type);
    }
#if NET7_0_OR_GREATER
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IsAssignableToAttribute<T> : TvtFactoryAttribute
        where T : class
    {
        public override bool Filter(Type type)
            => type.IsAssignableTo<T>();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IsDerivedFromAttribute<T> : TvtFactoryAttribute
        where T : class
    {
        public override bool Filter(Type type)
            => type.IsAssignableTo<T>() && type != typeof(T);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DerivedFromAnyAttribute<T> : TvtFactoryAttribute
        where T : class
    {
        public override bool Filter(Type type)
            => type.IsDerivedFromAny<T>();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IsNotDerivedFromAny<T> : TvtFactoryAttribute
        where T : class
    {
        public override bool Filter(Type type)
            => !type.IsDerivedFromAny<T>();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IsNotThisTvtAttribute<TTvt> : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => typeof(TTvt).GetCustomAttributes()
                .OfType<TvtFactoryAttribute>()
                .All(a => a.Filter(type))
                    is false;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementsAttribute<T> : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => type.Implements<T>();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementsNot<T> : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => !type.Implements<T>();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementsAnyAttribute<T> : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => type.ImplementsAny<T>();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementsNoneAttribute<T> : TvtFactoryAttribute
    {
        public override bool Filter(Type type)
            => !type.ImplementsAny<T>();
    }
#endif
}