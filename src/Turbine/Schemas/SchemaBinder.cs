using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Turbine;

/// <summary>
/// Cross-schema dispatch for JSON binding. Each concrete <see cref="ISchema"/> type's
/// <c>ToJson</c>/<c>FromJson</c> calls land here to convert values, so that nested schemas
/// (object inside array, array inside object, oneOf inside object, etc.) compose without
/// each schema knowing about every other.
/// </summary>
internal static class SchemaBinder
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static JsonNode? ValueToNode(ISchema schema, object? value)
    {
        if (value is null)
        {
            return null;
        }
        switch (schema)
        {
            case StringSchema:
                return JsonValue.Create((string) value);
            case BooleanSchema:
                return JsonValue.Create((bool) value);
            case DateOnlySchema:
                return JsonValue.Create(((DateOnly) value).ToString("O", CultureInfo.InvariantCulture));
            case DateTimeOffsetSchema:
                return JsonValue.Create(((DateTimeOffset) value).ToString("O", CultureInfo.InvariantCulture));
        }
        var type = schema.GetType();
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(NumericSchema<>))
            {
                return NumericValueToNode(value);
            }
            if (def == typeof(EnumSchema<>))
            {
                return JsonValue.Create(value.ToString());
            }
            if (def == typeof(ArraySchema<>))
            {
                return ArrayValueToNode(schema, value);
            }
            if (def == typeof(OneOfSchema<>))
            {
                return OneOfValueToNode(schema, value);
            }
        }
        if (schema is IObjectSchema objectSchema)
        {
            return ObjectValueToNode(objectSchema, value);
        }
        throw new InvalidOperationException(
            $"Cannot serialize value: unsupported schema type '{type.FullName}'.");
    }

    internal static object? NodeToValue(ISchema schema, JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }
        switch (schema)
        {
            case StringSchema:
                return element.GetString();
            case BooleanSchema:
                return element.GetBoolean();
            case DateOnlySchema:
                return DateOnly.Parse(element.GetString()!, CultureInfo.InvariantCulture);
            case DateTimeOffsetSchema:
                return DateTimeOffset.Parse(element.GetString()!, CultureInfo.InvariantCulture);
        }
        var type = schema.GetType();
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(NumericSchema<>))
            {
                return ConvertNumeric(element, NonNullable(targetType));
            }
            if (def == typeof(EnumSchema<>))
            {
                var raw = element.GetString();
                if (!Enum.TryParse(NonNullable(targetType), raw, ignoreCase: true, out var parsed))
                {
                    throw new TurbineBindingException(
                        $"'{raw}' is not a valid value for enum '{NonNullable(targetType).Name}'.");
                }
                return parsed;
            }
            if (def == typeof(ArraySchema<>))
            {
                return ArrayElementToValue(schema, element, targetType);
            }
            if (def == typeof(OneOfSchema<>))
            {
                return OneOfElementToValue(schema, element);
            }
        }
        if (schema is IObjectSchema objectSchema)
        {
            return ObjectElementToValue(objectSchema, element);
        }
        throw new InvalidOperationException(
            $"Cannot deserialize: unsupported schema type '{type.FullName}'.");
    }

    internal static void PopulateInstance(IObjectSchema schema, JsonElement element, object instance)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Expected JSON object, got {element.ValueKind}.");
        }
        foreach (var property in schema.Properties)
        {
            if (!TryGetProperty(element, property.Name, out var propertyElement))
            {
                continue;
            }
            ApplyPropertyToInstance(property, propertyElement, instance);
        }
    }

    private static void ApplyPropertyToInstance(ObjectProperty property, JsonElement element, object instance)
    {
        if (property.FromJson is not null)
        {
            // Custom FromJson takes (JsonValue, TDomain). The JsonElement may be any kind here.
            var node = JsonNode.Parse(element.GetRawText()) as JsonValue;
            if (node is null)
            {
                return;
            }
            property.FromJson.DynamicInvoke(node, instance);
            return;
        }
        if (property.Member is null)
        {
            // Custom expression-only property without setter — read-only, skip.
            return;
        }
        if (!property.Member.CanWrite)
        {
            return;
        }
        if (property.Schema is IObjectSchema childObject && property.Member.GetValue(instance) is { } existing)
        {
            // Reuse the existing instance to preserve identity / EF-tracked references.
            PopulateInstance(childObject, element, existing);
            return;
        }
        var value = NodeToValue(property.Schema, element, property.Member.PropertyType);
        if (value is null && property.Member.PropertyType.IsValueType
            && Nullable.GetUnderlyingType(property.Member.PropertyType) is null)
        {
            return;
        }
        property.Member.SetValue(instance, value);
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        // Case-insensitive lookup to be lenient with client casing conventions.
        if (element.TryGetProperty(name, out value))
        {
            return true;
        }
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static JsonNode? NumericValueToNode(object value)
    {
        return value switch
        {
            byte v => JsonValue.Create(v),
            sbyte v => JsonValue.Create(v),
            short v => JsonValue.Create(v),
            ushort v => JsonValue.Create(v),
            int v => JsonValue.Create(v),
            uint v => JsonValue.Create(v),
            long v => JsonValue.Create(v),
            ulong v => JsonValue.Create(v),
            float v => JsonValue.Create(v),
            double v => JsonValue.Create(v),
            decimal v => JsonValue.Create(v),
            _ => JsonValue.Create(System.Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
        };
    }

    private static object ConvertNumeric(JsonElement element, Type target)
    {
        if (target == typeof(byte)) return element.GetByte();
        if (target == typeof(sbyte)) return element.GetSByte();
        if (target == typeof(short)) return element.GetInt16();
        if (target == typeof(ushort)) return element.GetUInt16();
        if (target == typeof(int)) return element.GetInt32();
        if (target == typeof(uint)) return element.GetUInt32();
        if (target == typeof(long)) return element.GetInt64();
        if (target == typeof(ulong)) return element.GetUInt64();
        if (target == typeof(float)) return element.GetSingle();
        if (target == typeof(double)) return element.GetDouble();
        if (target == typeof(decimal)) return element.GetDecimal();
        throw new InvalidOperationException($"Unsupported numeric target type '{target.FullName}'.");
    }

    private static JsonArray ArrayValueToNode(ISchema arraySchema, object value)
    {
        var itemsProperty = arraySchema.GetType().GetProperty("Items")!;
        var itemSchema = (ISchema?) itemsProperty.GetValue(arraySchema);
        var array = new JsonArray();
        foreach (var item in (IEnumerable) value)
        {
            if (itemSchema is null)
            {
                array.Add(item is null ? null : JsonValue.Create(item.ToString()));
                continue;
            }
            array.Add(ValueToNode(itemSchema, item));
        }
        return array;
    }

    private static object ArrayElementToValue(ISchema arraySchema, JsonElement element, Type targetType)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Expected JSON array for {arraySchema.GetType().Name}, got {element.ValueKind}.");
        }
        var itemType = arraySchema.GetType().GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = (IList) Activator.CreateInstance(listType)!;
        var itemsProperty = arraySchema.GetType().GetProperty("Items")!;
        var itemSchema = (ISchema?) itemsProperty.GetValue(arraySchema);
        foreach (var entry in element.EnumerateArray())
        {
            if (itemSchema is null)
            {
                continue;
            }
            list.Add(NodeToValue(itemSchema, entry, itemType));
        }
        return list;
    }

    private static JsonObject ObjectValueToNode(IObjectSchema schema, object instance)
    {
        var obj = new JsonObject();
        foreach (var property in schema.Properties)
        {
            JsonNode? node;
            if (property.ToJson is not null)
            {
                var customResult = property.ToJson.DynamicInvoke(instance);
                node = customResult switch
                {
                    null => null,
                    JsonNode jn => jn.DeepClone(),
                    _ => JsonValue.Create(customResult.ToString()),
                };
            }
            else
            {
                var raw = ReadRaw(property, instance);
                node = ValueToNode(property.Schema, raw);
            }
            obj[property.Name] = node;
        }
        return obj;
    }

    private static object ObjectElementToValue(IObjectSchema schema, JsonElement element)
    {
        var domainType = schema.GetType().GetGenericArguments()[0];
        var instance = Activator.CreateInstance(domainType)
            ?? throw new InvalidOperationException(
                $"Could not instantiate '{domainType.FullName}'.");
        PopulateInstance(schema, element, instance);
        return instance;
    }

    private static object? ReadRaw(ObjectProperty property, object instance)
    {
        if (property.ValueExpression is not null)
        {
            return property.ValueExpression.DynamicInvoke(instance);
        }
        if (property.Member is not null)
        {
            return property.Member.GetValue(instance);
        }
        return null;
    }

    private static JsonObject? OneOfValueToNode(ISchema oneOfSchema, object value)
    {
        var (mapping, key, discriminator) = ResolveOneOfMapping(oneOfSchema, value.GetType().Name);
        if (mapping is null || key is null)
        {
            return null;
        }
        var obj = ObjectValueToNode(mapping, value);
        if (!string.IsNullOrEmpty(discriminator))
        {
            obj[discriminator] = JsonValue.Create(key);
        }
        return obj;
    }

    private static object OneOfElementToValue(ISchema oneOfSchema, JsonElement element)
    {
        var discriminator = (string) oneOfSchema.GetType()
            .GetProperty(nameof(OneOfSchema<object>.Discriminator))!
            .GetValue(oneOfSchema)!;
        if (!TryGetProperty(element, discriminator, out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
        {
            throw new TurbineBindingException(
                $"OneOf payload missing discriminator '{discriminator}'.");
        }
        var typeName = typeElement.GetString()!;
        var (mapping, _, _) = ResolveOneOfMapping(oneOfSchema, typeName);
        if (mapping is null)
        {
            throw new TurbineBindingException(
                $"OneOf has no mapping for discriminator value '{typeName}'.");
        }
        return ObjectElementToValue(mapping, element);
    }

    internal static (IObjectSchema? Mapping, string? Key, string Discriminator) ResolveOneOfMapping(
        ISchema oneOfSchema,
        string typeName)
    {
        var type = oneOfSchema.GetType();
        var discriminator = (string) type
            .GetProperty(nameof(OneOfSchema<object>.Discriminator))!
            .GetValue(oneOfSchema)!;
        var mappingsObj = type.GetProperty("Mappings", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(oneOfSchema);
        var mappings = (IReadOnlyDictionary<string, IObjectSchema>) mappingsObj!;
        if (mappings.TryGetValue(typeName, out var mapping))
        {
            return (mapping, typeName, discriminator);
        }
        return (null, null, discriminator);
    }

    internal static JsonElement NodeToElement(JsonNode? node)
    {
        if (node is null)
        {
            return JsonSerializer.SerializeToElement<object?>(null);
        }
        return JsonSerializer.SerializeToElement(node, SerializerOptions);
    }

    private static Type NonNullable(Type type)
        => Nullable.GetUnderlyingType(type) ?? type;
}
