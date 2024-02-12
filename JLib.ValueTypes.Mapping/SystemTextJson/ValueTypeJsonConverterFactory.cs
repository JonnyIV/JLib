﻿using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using JLib.Helper;

namespace JLib.ValueTypes.Mapping.SystemTextJson;

/// <summary>
/// used by the <see cref="ValueTypeJsonConverterFactory"/> to enable <see cref="ValueType{T}"/> interpretation of types<br/>
/// uses automapper to instantiate the <see cref="ValueType{T}"/>.<br/>
/// <seealso cref="ValueTypeProfile"/> can generate all required maps and is included in <seealso cref="AutoMapperHelper.AddProfiles"/> as long as the <seealso cref="ITypeCache"/> contains said <seealso cref="ValueType{T}"/>
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
    private readonly IMapper _mapper;
    private readonly ConcurrentDictionary<Type, JsonConverter> _converters = new();

    public ValueTypeJsonConverterFactory(IMapper mapper)
    {
        _mapper = mapper;
    }
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsDerivedFromAny(typeof(ValueType<Ignored>));

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => _converters.GetValueOrAdd(typeToConvert, type =>
        {
            Type nativeType = type
                .GetAnyBaseType<ValueType<Ignored>>()
                !.GenericTypeArguments
                .Single();

            Type converterType = nativeType switch
            {
                _ when nativeType.IsString() => typeof(StringValueTypeJsonConverter<>).MakeGenericType(typeToConvert),
                _ when nativeType.IsNumber() => typeof(NumericValueTypeJsonConverter<,>).MakeGenericType(typeToConvert, nativeType),
                _ when nativeType.IsGuid() => typeof(GuidValueTypeJsonConverter<>).MakeGenericType(typeToConvert),
                _ => throw new NotSupportedException(
                    $"Type {type.FullClassName()} is not supported. Only Numbers, strings and Guids are supported")
            };

            return Activator.CreateInstance(converterType, _mapper)
                       ?.As<JsonConverter>()
                   ?? throw new InvalidOperationException($"Activator failed to create converter of type {converterType.FullClassName()}");
        });
}