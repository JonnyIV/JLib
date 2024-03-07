using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using JLib.AutoMapper;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;

namespace JLib.ValueTypes.Mapping.SystemTextJson;

/// <summary>
/// used by the <see cref="ValueTypeJsonConverterFactory"/> to enable <see cref="ValueType{T}"/> interpretation of types<br/>
/// uses automapper to instantiate the <see cref="ValueType{T}"/>.<br/>
/// <seealso cref="ValueTypeProfile"/> can generate all required maps and is included in <seealso cref="AutoMapperHelper.AddProfiles"/> as long as the <seealso cref="ITypeCache"/> contains said <seealso cref="ValueType{T}"/><br/>
/// supports <seealso cref="Dictionary{TKey,TValue}"/> conversions where the key is a value type
/// <list type="bullet">
/// <item><see cref="Guid"/></item>
/// <item><see cref="string"/></item>
/// <item><see cref="byte"/></item>
/// <item><see cref="sbyte"/></item>
/// <item><see cref="short"/></item>
/// <item><see cref="ushort"/></item>
/// <item><see cref="int"/></item>
/// <item><see cref="uint"/></item>
/// <item><see cref="long"/></item>
/// <item><see cref="ulong"/></item>
/// <item><see cref="decimal"/></item>
/// <item><see cref="double"/></item>
/// <item><see cref="float"/></item>
/// </list>
/// </summary>
public class ValueTypeJsonConverterFactory : JsonConverterFactory
{
    private IMapper? _mapper;
    private readonly ConcurrentDictionary<Type, JsonConverter> _converters = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapper">
    ///     when null, it has to be set later using <see cref="AddMapper"/>.<br/>
    ///     this enabled the mapper initialization 
    /// </param>
    public ValueTypeJsonConverterFactory(IMapper? mapper)
    {
        _mapper = mapper;
    }

    /// <summary>
    /// adds the automapper reference after the constructor has been called with a null argument.<br/>
    /// </summary>
    /// <param name="mapper"></param>
    public void AddMapper(IMapper mapper)
    {
        if (_mapper is not null)
            throw new InvalidOperationException("Mapper has already been set");
        _mapper = mapper;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return IsValueType(typeToConvert) || IsValueTypeDictionary(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => _converters.GetValueOrAdd(typeToConvert, CreateConverter);



    private JsonConverter CreateConverter(Type type)
    {
        if (IsValueType(type))
            return CreateValueTypeConverter(type);
        if(IsValueTypeDictionary(type))
            return CreateValueTypeDictionaryConverter(type);
        throw new NotSupportedException($"Type {type.FullName()} is not supported");
    }

    private bool IsValueType(Type type)
        => type.IsDerivedFromAny<ValueType<Ignored>>();
    private JsonConverter CreateValueTypeConverter(Type typeToConvert)
    {
        Type nativeType = typeToConvert
            .GetAnyBaseType<ValueType<Ignored>>()
            !.GenericTypeArguments
            .Single();

        Type converterType = nativeType switch
        {
            _ when nativeType.IsString() => typeof(StringValueTypeJsonConverter<>).MakeGenericType(typeToConvert),
            _ when nativeType.IsNumber() => typeof(NumericValueTypeJsonConverter<,>).MakeGenericType(typeToConvert, nativeType),
            _ when nativeType.IsGuid() => typeof(GuidValueTypeJsonConverter<>).MakeGenericType(typeToConvert),
            _ => throw new NotSupportedException(
                $"Type {typeToConvert.FullName()} is not supported. Only Numbers, strings and Guids are supported")
        };

        if (_mapper is null)
            throw new InvalidOperationException($"Mapper has not been set. Use {AddMapper} to set the AutoMapper reference");

        return Activator.CreateInstance(converterType, _mapper)
                   ?.As<JsonConverter>()
               ?? throw new InvalidOperationException($"Activator failed to create converter of type {converterType.FullName()}");

    }

    private bool IsValueTypeDictionary(Type type)
    {
        var i = type.GetAnyInterface<IDictionary<Ignored, Ignored>>();
        return i is not null
               && i.GenericTypeArguments.First().IsDerivedFromAny<ValueType<Ignored>>();
    }

    private JsonConverter CreateValueTypeDictionaryConverter(Type typeToConvert)
    {
        var args = typeToConvert.GetAnyInterface<IDictionary<Ignored, Ignored>>()?.GenericTypeArguments
            ?? throw new NotSupportedException($"{typeToConvert.FullName(true)} is not a dictionary");
        var keyValueType = args[0];
        var keyNativeType = keyValueType.GetAnyBaseType<ValueType<Ignored>>()?.GenericTypeArguments.Single()
            ?? throw new InvalidSetupException("native type not found");
        var valueType = args[1];
        var converterType = typeof(ValueTypeDictionaryJsonConverter<,,>).MakeGenericType(keyValueType, keyNativeType, valueType);
        if (_mapper is null)
            throw new InvalidOperationException($"Mapper has not been set. Use {AddMapper} to set the AutoMapper reference");

        return Activator.CreateInstance(converterType, _mapper)
                   ?.As<JsonConverter>()
               ?? throw new InvalidOperationException($"Activator failed to create converter of type {converterType.FullName()}");
    }

}