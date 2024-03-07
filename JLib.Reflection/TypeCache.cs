using System.Reflection;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection.Exceptions;
using JLib.ValueTypes;
using Microsoft.Extensions.Logging;

namespace JLib.Reflection;

/// <summary>
/// groups <see cref="Type"/>s by <see cref="TypeValueType"/>, validates them and initializes Navigation
/// <br/> Service interface for the <see cref="TypeCache"/>.
/// <br/>- <seealso cref="TypeValueType"/>
/// <br/>- <seealso cref="NavigatingTypeValueType"/>
/// <br/>- <seealso cref="IValidatedType"/>
/// <br/>- <seealso cref="IPostNavigationInitializedType"/>
/// <br/>- <seealso cref="TvtFactoryAttribute"/>
/// <br/>- <seealso cref="IgnoreInCache"/>
/// </summary>
public interface ITypeCache
{
    /// <summary>
    /// all known <see cref="TypeValueType"/>s 
    /// </summary>
    // todo: change type to TypeValueType
    public IReadOnlyCollection<Type> KnownTypeValueTypes { get; }

    /// <summary>
    /// all types known to the typeCache without any filters or applied valueTypes
    /// </summary>
    public IReadOnlyCollection<Type> KnownTypes { get; }

    public TTvt Get<TTvt>(Func<TTvt, bool> filter) where TTvt : class, ITypeValueType
        => All(filter).Single();

    public TTvt Get<TTvt>(Type weakType) where TTvt : class, ITypeValueType;

    public TTvt Get<TTvt, TType>() where TTvt : class, ITypeValueType
        => Get<TTvt>(typeof(TType));

    public TTvt? TryGet<TTvt>(Func<TTvt, bool> filter) where TTvt : class, ITypeValueType
        => All(filter).SingleOrDefault();

    public TTvt? TryGet<TTvt>(Type? weakType) where TTvt : class, ITypeValueType;

    public TTvt? TryGet<TTvt, TType>() where TTvt : class, ITypeValueType
        => TryGet<TTvt>(typeof(TType).TryGetGenericTypeDefinition());

    public IEnumerable<TTvt> All<TTvt>() where TTvt : class, ITypeValueType;

    public IEnumerable<TTvt> All<TTvt>(Func<TTvt, bool> filter) where TTvt : class, ITypeValueType
        => All<TTvt>().Where(filter);

    public TTvt? SingleOrDefault<TTvt>(Func<TTvt, bool> selector) where TTvt : class, ITypeValueType
    {
        var res = All<TTvt>().Where(selector).ToArray();
        if (res.Length > 1)
            throw new InvalidSetupException($"multiple selectors matched to be cast to {typeof(TTvt).Name}: " +
                                            string.Join(", ", res.Select(r => r.Value.Name)));
        return res.FirstOrDefault();
    }

    public TTvt Single<TTvt>(Func<TTvt, bool> selector) where TTvt : TypeValueType
        => SingleOrDefault(selector) ?? throw new InvalidSetupException("no selector matched");
}

/// <summary>
/// provides an easy-to-use way to group types by kind, i.e. entities
/// <br/>searches the Application for <see cref="TypeValueType"/> instances with <see cref="TvtFactoryAttribute.ITypeValueTypeFilterAttribute"/> attributes
/// and populates them with the types provided via constructor.
/// <br/> all reflection is done in the constructor
/// <br/> should be used as singleton
/// </summary>
public class TypeCache : ITypeCache
{
    private record ValueTypeForTypeValueTypes : ValueType<Type>
    {
        public ValueTypeForTypeValueTypes(Type Value) : base(Value)
        {
            if (!Value.IsAssignableTo(typeof(TypeValueType)))
                throw new TvtNavigationFailedException($"{Value.Name} does not derive from {nameof(TypeValueType)}");
            if (Value.IsAbstract)
                throw new TvtNavigationFailedException($"{Value.Name} is abstract");
        }

        public bool Filter(Type otherType)
            => Value.GetCustomAttributes()
                .OfType<TvtFactoryAttribute>()
                .All(filterAttr => filterAttr.Filter(otherType));

        public TypeValueType Create(Type type)
        {
            var ctor = Value.GetConstructor(new[] { typeof(Type) })
                       ?? throw new InvalidTypeException(Value, Value, $"ctor not found for {Value.Name}");
            var instance = ctor.Invoke(new object[] { type })
                           ?? throw new InvalidSetupException($"ctor could not be invoked for {Value.Name}");
            return instance as TypeValueType
                   ?? throw new InvalidSetupException($"instance of {Value} is not a {nameof(TypeValueType)}");
        }
    }

    private readonly TypeValueType[] _typeValueTypes;
    private readonly IReadOnlyDictionary<Type, TypeValueType> _typeMappings;
    private readonly ILogger _logger;
    public IReadOnlyCollection<Type> KnownTypeValueTypes { get; }

    public IReadOnlyCollection<Type> KnownTypes { get; }

    #region constructor

    public TypeCache(ITypePackage typePackage, ExceptionBuilder parentExceptionManager, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(ITypeCache)?.FullName ?? nameof(ITypeCache));
        using var _ = _logger.BeginScope(this);
        KnownTypes = typePackage.GetContent().ToArray();
        const string exceptionMessage = "Cache setup failed";
        var exceptions = parentExceptionManager.CreateChild(exceptionMessage);

        var availableTypeValueTypes = KnownTypes
            .Where(type => !type.HasCustomAttribute<IgnoreInCache>())
            .Where(type => type.IsAssignableTo<TypeValueType>() && !type.IsAbstract)
            .Select(tvt => new ValueTypeForTypeValueTypes(tvt))
            .ToArray();
        KnownTypeValueTypes = availableTypeValueTypes.Select(tvtt => tvtt.Value).ToArray();

        exceptions.CreateChild(
            "some Types have no filter attributes",
            availableTypeValueTypes.Where(tvtt => tvtt.Value
                .CustomAttributes.None(a =>
                    a.AttributeType.IsAssignableTo<TvtFactoryAttribute>())
            ).Select(tvtt => new InvalidTypeException(tvtt.GetType(), tvtt.Value,
                tvtt.Value.FullName(true)))
        );
        var discoveryExceptions = exceptions.CreateChild("type discovery failed");
        try
        {
            _typeValueTypes = KnownTypes
                .Where(type => !type.HasCustomAttribute<IgnoreInCache>() && !type.IsAssignableTo<TypeValueType>())
                .Select(type =>
                {
                    try
                    {
                        var validTvtGroups = availableTypeValueTypes
                            .Where(availableTvtt => availableTvtt.Filter(type))
                            .ToLookup(t =>
                                t.Value.GetCustomAttribute<TvtFactoryAttribute.PriorityAttribute>()?.Value
                                ?? TvtFactoryAttribute.PriorityAttribute.DefaultPriority);
                        var validTvts = validTvtGroups.MinBy(x => x.Key)?
                            .ToArray() ?? Array.Empty<ValueTypeForTypeValueTypes>();
                        switch (validTvts.Length)
                        {
                            case > 1:
                                discoveryExceptions.Add(new InvalidSetupException(
                                    $"multiple tvt candidates found for type {type.Name} : " +
                                    $@"[ {string.Join(", ", validTvts.Select(tvt =>
                                    {
                                        var priority = tvt.Value.GetCustomAttribute<TvtFactoryAttribute.PriorityAttribute>()?.Value
                                                       ?? TvtFactoryAttribute.PriorityAttribute.DefaultPriority;
                                        return $"{tvt.Value.Name}(priority {priority})";
                                    }).OrderBy(d => d))} ]"));
                                return null;
                            case 0:
                                return null;
                            default:
                                return validTvts.Single().Create(type);
                        }
                    }
                    catch (Exception e)
                    {
                        discoveryExceptions.Add(e);
                        return null;
                    }
                }).WhereNotNull()
                .ToArray();

            _typeMappings = _typeValueTypes.ToDictionary(tvt => tvt.Value);
        }
        catch (Exception ex)
        {
            discoveryExceptions.Add(ex);
            if (_typeValueTypes is null || _typeMappings is null)
                throw exceptions.GetException()!;
        }


        foreach (var typeValueType in _typeValueTypes.OfType<NavigatingTypeValueType>())
        {
            try
            {
                typeValueType.SetCache(this);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        foreach (var typeValueType in _typeValueTypes.OfType<NavigatingTypeValueType>())
        {
            try
            {
                typeValueType.MaterializeNavigation();
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                exceptions.Add(e.InnerException);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        foreach (var typeValueType in _typeValueTypes.OfType<IPostNavigationInitializedType>())
        {
            try
            {
                typeValueType.Initialize(this, exceptions.CreateChild("Initialization failed"));
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        foreach (var typeValueType in _typeValueTypes.OfType<IValidatedType>())
        {
            try
            {
                var tvtValidator = new TypeValidator(typeValueType.CastTo<TypeValueType>(),
                    typeValueType.GetType().FullName());
                typeValueType.Validate(this, tvtValidator);
                exceptions.AddChild(tvtValidator);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }
        
        WriteLog();
    }

    #endregion

    public T Get<T>(Type weakType)
        where T : class, ITypeValueType
        => _typeMappings[weakType].CastTo<T>();

    public T? TryGet<T>(Type? weakType)
        where T : class, ITypeValueType
        => weakType is null
            ? null
            : _typeMappings.TryGetValue(weakType, out var tvt)
                ? tvt.As<T?>()
                : null;

    public IEnumerable<T> All<T>()
        where T : class, ITypeValueType
        => _typeValueTypes.OfType<T>();

    public void WriteLog()
    {
        using var _ = _logger.BeginScope(this);
        _logger.LogInformation("Initialized TypeCache with a total of {typeCount} types", _typeValueTypes.Length);
        WriteDebug();

        var missing = KnownTypeValueTypes.Except(_typeValueTypes.Select(x => x.GetType()).Distinct()).ToArray();
        if (missing.Any())
            _logger.LogWarning("  No types found for: {TypeValueTypeName}", missing);
        return;

        void WriteDebug()
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            var typesByAssembly = _typeValueTypes
                .ToLookup(tvt => tvt.Value.Assembly.FullName)
                .OrderBy(g => g.Key)
                .ToArray();

            foreach (var typesInAssembly in typesByAssembly)
            {
                _logger.LogDebug("  Found {typeCount} types in Assemlby {assemblyName}", typesInAssembly.Count(),
                    typesInAssembly.Key);
                WriteTypes(typesInAssembly);
            }
            //Log.Verbose("  Total Types:");
            //WriteTypes(_typeValueTypes);
        }

        void WriteTypes(IEnumerable<TypeValueType> types)
        {
            var registeredTypes = types
                .ToLookup(tvt => tvt.GetType())
                .OrderBy(g => g.Key.Name)
                .ToArray();
            foreach (var group in registeredTypes)
            {
                _logger.LogDebug("    ValueTypeType     + {TypeValueTypeName}", group.Key);

                if (!_logger.IsEnabled(LogLevel.Trace))
                    continue;
                foreach (var tvt in group)
                    _logger.LogTrace("      DiscoveredType    - {TypeName}", tvt.Name);
            }
        }
    }
}