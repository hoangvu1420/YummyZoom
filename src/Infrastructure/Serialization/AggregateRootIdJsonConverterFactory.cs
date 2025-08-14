using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Serialization;

public sealed class AggregateRootIdJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
		=> IsAggregateRootId(typeToConvert);

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var valueType = GetIdValueType(typeToConvert);
		var converterType = typeof(AggregateRootIdJsonConverter<,>).MakeGenericType(typeToConvert, valueType);
		return (JsonConverter)Activator.CreateInstance(converterType)!;
	}

	private static bool IsAggregateRootId(Type t)
		=> GetAggregateRootIdBase(t) is not null;

	private static Type? GetAggregateRootIdBase(Type t)
		=> t == typeof(object)
			? null
			: t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AggregateRootId<>)
				? t
				: t.BaseType is null
					? null
					: GetAggregateRootIdBase(t.BaseType);

	private static Type GetIdValueType(Type t)
		=> GetAggregateRootIdBase(t)!.GetGenericArguments()[0];
}

public sealed class AggregateRootIdJsonConverter<TId, TValue> : JsonConverter<TId>
{
	private readonly PropertyInfo _valueProperty;
	private readonly MethodInfo _createMethod;

	public AggregateRootIdJsonConverter()
	{
		_valueProperty = typeof(TId).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!
			?? throw new InvalidOperationException($"{typeof(TId).Name} must have a public Value property.");

		_createMethod = typeof(TId).GetMethod("Create", BindingFlags.Public | BindingFlags.Static, new[] { typeof(TValue) })!
			?? throw new InvalidOperationException($"{typeof(TId).Name} must have static Create({typeof(TValue).Name}) method.");
	}

	public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
		return (TId)_createMethod.Invoke(null, new object[] { value })!;
	}

	public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
	{
		var primitive = (TValue)_valueProperty.GetValue(value)!;
		JsonSerializer.Serialize(writer, primitive, options);
	}
}
