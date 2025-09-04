using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using YummyZoom.Domain.Common.Models;
using System.Globalization;

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
		var result = (TId)_createMethod.Invoke(null, new object[] { value })!;
		return result;
	}

	public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
	{
		var primitive = (TValue)_valueProperty.GetValue(value)!;
		JsonSerializer.Serialize(writer, primitive, options);
	}

	public override void WriteAsPropertyName(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
	{
		var primitive = (TValue)_valueProperty.GetValue(value)!;
		if (primitive is Guid g)
		{
			writer.WritePropertyName(g.ToString());
			return;
		}
		if (primitive is string s)
		{
			writer.WritePropertyName(s);
			return;
		}
		if (primitive is int i)
		{
			writer.WritePropertyName(i.ToString(CultureInfo.InvariantCulture));
			return;
		}
		if (primitive is long l)
		{
			writer.WritePropertyName(l.ToString(CultureInfo.InvariantCulture));
			return;
		}
		// Fallback to ToString for other primitive-like types
		writer.WritePropertyName(primitive?.ToString() ?? string.Empty);
	}

	public override TId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var s = reader.GetString()!;
		object parsed;
		if (typeof(TValue) == typeof(Guid))
		{
			parsed = Guid.Parse(s);
		}
		else if (typeof(TValue) == typeof(string))
		{
			parsed = s;
		}
		else if (typeof(TValue) == typeof(int))
		{
			parsed = int.Parse(s, CultureInfo.InvariantCulture);
		}
		else if (typeof(TValue) == typeof(long))
		{
			parsed = long.Parse(s, CultureInfo.InvariantCulture);
		}
		else
		{
			// As a last resort attempt to deserialize from quoted string into target type
			parsed = (object)s;
		}
		var result = (TId)_createMethod.Invoke(null, new object[] { parsed })!;
		return result;
	}
}
