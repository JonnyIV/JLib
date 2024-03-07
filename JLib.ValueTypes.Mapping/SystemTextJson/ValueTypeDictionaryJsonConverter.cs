using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using JLib.Exceptions;
using JLib.Helper;

namespace JLib.ValueTypes.Mapping.SystemTextJson;
internal class ValueTypeDictionaryJsonConverter<TKey, TKeyNative, TValue> : JsonConverter<Dictionary<TKey, TValue>>
where TKey : ValueType<TKeyNative>
where TKeyNative : notnull
{
    private readonly IMapper _mapper;
    private static readonly Type VtDictType = typeof(Dictionary<TKey, TValue>);
    private static readonly Type NativeDictType = typeof(Dictionary<TKeyNative, TValue>);
    public ValueTypeDictionaryJsonConverter(IMapper mapper)
    {
        _mapper = mapper;
    }
    public override Dictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var converterRaw = options.GetConverter(NativeDictType)
            ?? throw new InvalidSetupException("converter could not be created found");
        var converter = converterRaw as JsonConverter<Dictionary<TKeyNative, TValue>>
            ?? throw new InvalidSetupException("converter could not be cast");

        var dict = converter.Read(ref reader, NativeDictType, options);
        return dict?.ToDictionary(
            x => _mapper.Map<TKey>(x.Key),
            x => x.Value
        );
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        var mapped = value.ToDictionary(
            x => x.Key.Value,
            x => x.Value
            );
        var converter = options.GetConverter(mapped.GetType());
        var writeMi = converter.GetType().GetMethod(nameof(JsonConverter<Ignored>.Write))
            ?? throw new InvalidSetupException(
                $"Write method not found on converter {converter.GetType().FullName()}");

        writeMi.Invoke(converter, new object[] { writer, mapped, options });
    }
}
