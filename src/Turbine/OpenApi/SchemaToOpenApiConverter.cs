using System.Globalization;
using System.Numerics;
using System.Reflection;
using Microsoft.OpenApi;

namespace Turbine;

internal sealed class SchemaToOpenApiConverter
{
    private readonly Func<ISchema, IOpenApiSchema?> resolveOwnedReference;

    public SchemaToOpenApiConverter(Func<ISchema, IOpenApiSchema?>? resolveOwnedReference = null)
    {
        this.resolveOwnedReference = resolveOwnedReference ?? (_ => null);
    }

    public OpenApiSchema Convert(ISchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return schema switch
        {
            StringSchema s => ConvertString(s),
            BooleanSchema s => ConvertBoolean(s),
            DateOnlySchema s => ConvertDateOnly(s),
            DateTimeOffsetSchema s => ConvertDateTimeOffset(s),
            IObjectSchema s => ConvertObject(s),
            _ => ConvertGeneric(schema),
        };
    }

    private OpenApiSchema ConvertGeneric(ISchema schema)
    {
        var type = schema.GetType();
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(NumericSchema<>))
            {
                return ConvertNumeric(schema, type.GenericTypeArguments[0]);
            }
            if (def == typeof(EnumSchema<>))
            {
                return ConvertEnum(schema, type.GenericTypeArguments[0]);
            }
            if (def == typeof(ArraySchema<>))
            {
                return ConvertArray(schema);
            }
            if (def == typeof(OneOfSchema<>))
            {
                return ConvertOneOf(schema, type);
            }
        }
        throw new InvalidOperationException(
            $"Unsupported schema type '{type.FullName}' encountered while building OpenAPI schema.");
    }

    private static OpenApiSchema ConvertString(StringSchema schema)
    {
        var result = new OpenApiSchema
        {
            Type = WithNullable(JsonSchemaType.String, schema.Nullable),
            MinLength = schema.MinLength,
            MaxLength = schema.MaxLength,
            Format = schema.Format,
        };
        if (schema.Pattern is not null)
        {
            result.Pattern = schema.Pattern.ToString();
        }
        return result;
    }

    private static OpenApiSchema ConvertBoolean(BooleanSchema schema)
    {
        return new OpenApiSchema
        {
            Type = WithNullable(JsonSchemaType.Boolean, schema.Nullable),
        };
    }

    private static OpenApiSchema ConvertDateOnly(DateOnlySchema schema)
    {
        return new OpenApiSchema
        {
            Type = WithNullable(JsonSchemaType.String, schema.Nullable),
            Format = "date",
        };
    }

    private static OpenApiSchema ConvertDateTimeOffset(DateTimeOffsetSchema schema)
    {
        return new OpenApiSchema
        {
            Type = WithNullable(JsonSchemaType.String, schema.Nullable),
            Format = "date-time",
        };
    }

    private static OpenApiSchema ConvertNumeric(ISchema schema, Type numericType)
    {
        var (jsonType, format) = ClassifyNumeric(numericType);
        var nullable = (bool?) schema.GetType().GetProperty(nameof(NumericSchema<int>.Nullable))!.GetValue(schema);
        var result = new OpenApiSchema
        {
            Type = WithNullable(jsonType, nullable),
            Format = format,
        };
        var minimum = schema.GetType().GetProperty(nameof(NumericSchema<int>.Minimum))!.GetValue(schema);
        var maximum = schema.GetType().GetProperty(nameof(NumericSchema<int>.Maximum))!.GetValue(schema);
        var exclusiveMinimum = schema.GetType().GetProperty(nameof(NumericSchema<int>.ExclusiveMinimum))!.GetValue(schema);
        var exclusiveMaximum = schema.GetType().GetProperty(nameof(NumericSchema<int>.ExclusiveMaximum))!.GetValue(schema);
        var multipleOf = schema.GetType().GetProperty(nameof(NumericSchema<int>.MultipleOf))!.GetValue(schema);

        // Microsoft.OpenApi 2.0 stores Minimum and ExclusiveMinimum (and likewise Maximum/ExclusiveMaximum)
        // in shared slots — setting the exclusive form clears the inclusive form. Prefer the exclusive
        // form when both are present so the stricter constraint wins.
        if (exclusiveMinimum is not null) result.ExclusiveMinimum = FormatNumber(exclusiveMinimum);
        else if (minimum is not null) result.Minimum = FormatNumber(minimum);
        if (exclusiveMaximum is not null) result.ExclusiveMaximum = FormatNumber(exclusiveMaximum);
        else if (maximum is not null) result.Maximum = FormatNumber(maximum);
        if (multipleOf is not null) result.MultipleOf = ToDecimal(multipleOf);

        return result;
    }

    private static (JsonSchemaType Type, string? Format) ClassifyNumeric(Type t)
    {
        if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int))
            return (JsonSchemaType.Integer, "int32");
        if (t == typeof(uint) || t == typeof(long))
            return (JsonSchemaType.Integer, "int64");
        if (t == typeof(ulong))
            return (JsonSchemaType.Integer, "int64");
        if (t == typeof(float))
            return (JsonSchemaType.Number, "float");
        if (t == typeof(double))
            return (JsonSchemaType.Number, "double");
        if (t == typeof(decimal))
            return (JsonSchemaType.Number, "double");
        return (JsonSchemaType.Number, null);
    }

    private static string FormatNumber(object value)
    {
        return value switch
        {
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static decimal ToDecimal(object value)
    {
        return value switch
        {
            decimal d => d,
            IConvertible c => c.ToDecimal(CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType()} to decimal."),
        };
    }

    private static OpenApiSchema ConvertEnum(ISchema schema, Type enumType)
    {
        var nullableProp = schema.GetType().GetProperty(nameof(EnumSchema<DayOfWeek>.Nullable))!;
        var nullable = (bool?) nullableProp.GetValue(schema);
        var result = new OpenApiSchema
        {
            Type = WithNullable(JsonSchemaType.String, nullable),
            Enum = new List<System.Text.Json.Nodes.JsonNode>(),
        };
        foreach (var name in Enum.GetNames(enumType))
        {
            result.Enum.Add(System.Text.Json.Nodes.JsonValue.Create(name)!);
        }
        return result;
    }

    private OpenApiSchema ConvertArray(ISchema schema)
    {
        var nullable = (bool?) schema.GetType().GetProperty(nameof(ArraySchema<int>.Nullable))!.GetValue(schema);
        var minItems = (int?) schema.GetType().GetProperty(nameof(ArraySchema<int>.MinItems))!.GetValue(schema);
        var maxItems = (int?) schema.GetType().GetProperty(nameof(ArraySchema<int>.MaxItems))!.GetValue(schema);
        var items = (ISchema?) schema.GetType().GetProperty(nameof(ArraySchema<int>.Items))!.GetValue(schema);
        var result = new OpenApiSchema
        {
            Type = WithNullable(JsonSchemaType.Array, nullable),
            MinItems = minItems,
            MaxItems = maxItems,
        };
        if (items is not null)
        {
            result.Items = ResolveNested(items);
        }
        return result;
    }

    private OpenApiSchema ConvertObject(IObjectSchema schema)
    {
        var result = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal),
            Required = new HashSet<string>(StringComparer.Ordinal),
        };
        foreach (var property in schema.Properties)
        {
            result.Properties[property.Name] = ResolveNested(property.Schema);
            if (property.Required)
            {
                result.Required.Add(property.Name);
            }
        }
        return result;
    }

    private OpenApiSchema ConvertOneOf(ISchema schemaObj, Type type)
    {
        var nullable = (bool?) type.GetProperty(nameof(OneOfSchema<object>.Nullable))!.GetValue(schemaObj);
        var discriminator = (string) type.GetProperty(nameof(OneOfSchema<object>.Discriminator))!.GetValue(schemaObj)!;
        var mappingsObj = type.GetProperty("Mappings", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(schemaObj);
        var mappings = (System.Collections.IEnumerable) mappingsObj!;

        var result = new OpenApiSchema
        {
            OneOf = new List<IOpenApiSchema>(),
        };
        if (nullable == true)
        {
            // Represent null variant via OneOf containing a null-typed schema; OpenAPI 3.1 style.
            result.OneOf.Add(new OpenApiSchema { Type = JsonSchemaType.Null });
        }
        var discriminatorMap = new Dictionary<string, OpenApiSchemaReference>(StringComparer.Ordinal);
        foreach (var kvObj in mappings)
        {
            var kvType = kvObj!.GetType();
            var key = (string) kvType.GetProperty("Key")!.GetValue(kvObj)!;
            var value = (IObjectSchema) kvType.GetProperty("Value")!.GetValue(kvObj)!;
            var nested = ResolveNested(value);
            result.OneOf.Add(nested);
            if (nested is OpenApiSchemaReference reference)
            {
                discriminatorMap[key] = reference;
            }
        }

        result.Discriminator = new OpenApiDiscriminator
        {
            PropertyName = discriminator,
            Mapping = discriminatorMap,
        };

        return result;
    }

    private IOpenApiSchema ResolveNested(ISchema schema)
    {
        var owned = resolveOwnedReference(schema);
        return owned ?? Convert(schema);
    }

    private static JsonSchemaType WithNullable(JsonSchemaType type, bool? nullable)
        => nullable == true ? type | JsonSchemaType.Null : type;
}
