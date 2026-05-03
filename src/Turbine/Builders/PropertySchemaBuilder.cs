using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Turbine;

public abstract class PropertySchemaBuilder<TDomain, TSelf> : SchemaBuilder<TSelf>
    where TSelf : PropertySchemaBuilder<TDomain, TSelf>
{
    internal PropertySchemaBuilder() { }

    internal abstract void AddProperty(ObjectProperty property);

    internal abstract void RemoveProperty(string propertyName);

    public TSelf AddPropertiesFrom<TSource>(Func<ObjectSchema<TSource>> schema, bool? asRequired = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var source = schema();
        foreach (var property in source.Properties)
        {
            AddProperty(new ObjectProperty
            {
                Name = property.Name,
                Schema = property.Schema,
                Required = asRequired ?? property.Required,
            });
        }
        return (TSelf) this;
    }

    public TSelf AddAtomicProperties(bool? asRequired = null)
    {
        var properties = typeof(TDomain).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var atomic = TryCreateAtomicSchema(property.PropertyType);
            if (atomic is null)
            {
                continue;
            }
            AddProperty(new ObjectProperty
            {
                Name = property.Name,
                Schema = atomic.Value.Schema,
                Required = asRequired ?? atomic.Value.DefaultRequired,
            });
        }
        return (TSelf) this;
    }

    private static (ISchema Schema, bool DefaultRequired)? TryCreateAtomicSchema(Type propertyType)
    {
        var underlying = System.Nullable.GetUnderlyingType(propertyType);
        var actualType = underlying ?? propertyType;
        var defaultRequired = underlying is null;

        if (actualType == typeof(string))
        {
            return (new StringSchema(), true);
        }
        if (actualType == typeof(bool))
        {
            return (new BooleanSchema(), defaultRequired);
        }
        if (actualType == typeof(DateOnly))
        {
            return (new DateOnlySchema(), defaultRequired);
        }
        if (actualType == typeof(DateTimeOffset))
        {
            return (new DateTimeOffsetSchema(), defaultRequired);
        }
        if (actualType.IsEnum)
        {
            var schema = (ISchema) Activator.CreateInstance(
                typeof(EnumSchema<>).MakeGenericType(actualType),
                nonPublic: true)!;
            return (schema, defaultRequired);
        }
        if (IsNumeric(actualType))
        {
            var schema = (ISchema) Activator.CreateInstance(
                typeof(NumericSchema<>).MakeGenericType(actualType),
                nonPublic: true)!;
            return (schema, defaultRequired);
        }
        return null;
    }

    private static bool IsNumeric(Type type)
    {
        if (!type.IsValueType)
        {
            return false;
        }
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType
                && iface.GetGenericTypeDefinition() == typeof(INumber<>)
                && iface.GenericTypeArguments[0] == type)
            {
                return true;
            }
        }
        return false;
    }

    public TSelf Remove<TProperty>(Expression<Func<TDomain, TProperty?>> selector)
    {
        RemoveProperty(GetPropertyName(selector));
        return (TSelf) this;
    }

    public TSelf Remove(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        RemoveProperty(propertyName);
        return (TSelf) this;
    }

    // Enum
    public TSelf Add<TEnum>(
        Expression<Func<TDomain, TEnum>> selector,
        string? name = null,
        bool? required = null,
        Action<EnumSchemaBuilder<TEnum>>? schema = null)
        where TEnum : struct, Enum
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new EnumSchema<TEnum>(),
            s => new EnumSchemaBuilder<TEnum>(s),
            schema);
    }

    // Boolean
    public TSelf Add(
        Expression<Func<TDomain, bool>> selector,
        string? name = null,
        bool? required = null,
        Action<BooleanSchemaBuilder>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new BooleanSchema(),
            s => new BooleanSchemaBuilder(s),
            schema);
    }

    public TSelf Add(
        Expression<Func<TDomain, bool?>> selector,
        string? name = null,
        bool? required = null,
        Action<BooleanSchemaBuilder>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? false,
            () => new BooleanSchema(),
            s => new BooleanSchemaBuilder(s),
            schema);
    }

    // DateTimeOffset
    public TSelf Add(
        Expression<Func<TDomain, DateTimeOffset>> selector,
        string? name = null,
        bool? required = null,
        Action<DateTimeOffsetSchemaBuilder>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new DateTimeOffsetSchema(),
            s => new DateTimeOffsetSchemaBuilder(s),
            schema);
    }

    public TSelf Add(
        Expression<Func<TDomain, DateTimeOffset?>> selector,
        string? name = null,
        bool? required = null,
        Action<DateTimeOffsetSchemaBuilder>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? false,
            () => new DateTimeOffsetSchema(),
            s => new DateTimeOffsetSchemaBuilder(s),
            schema);
    }

    // DateOnly
    public TSelf Add(
        Expression<Func<TDomain, DateOnly>> selector,
        string? name = null,
        bool? required = null,
        Action<DateOnlySchemaBuilder>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new DateOnlySchema(),
            s => new DateOnlySchemaBuilder(s),
            schema);
    }

    public TSelf Add(
        Expression<Func<TDomain, DateOnly?>> selector,
        string? name = null,
        bool? required = null,
        Action<DateOnlySchemaBuilder>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? false,
            () => new DateOnlySchema(),
            s => new DateOnlySchemaBuilder(s),
            schema);
    }

    // String (no string overload, since string and string? are equivalent at runtime)
    public TSelf Add(
        Expression<Func<TDomain, string?>> selector,
        string? name = null,
        bool? required = null,
        Action<StringSchemaBuilder>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new StringSchema(),
            s => new StringSchemaBuilder(s),
            schema);
    }

    // Numeric
    public TSelf Add<TNumber>(
        Expression<Func<TDomain, TNumber>> selector,
        string? name = null,
        bool? required = null,
        Action<NumericSchemaBuilder<TNumber>>? schema = null)
        where TNumber : struct, INumber<TNumber>
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new NumericSchema<TNumber>(),
            s => new NumericSchemaBuilder<TNumber>(s),
            schema);
    }

    public TSelf Add<TNumber>(
        Expression<Func<TDomain, TNumber?>> selector,
        string? name = null,
        bool? required = null,
        Action<NumericSchemaBuilder<TNumber>>? schema = null)
        where TNumber : struct, INumber<TNumber>
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? false,
            () => new NumericSchema<TNumber>(),
            s => new NumericSchemaBuilder<TNumber>(s),
            schema);
    }

    // Object
    public TSelf AddObject<TProperty>(
        Expression<Func<TDomain, TProperty>> selector,
        string? name = null,
        bool? required = null,
        Action<ObjectSchemaBuilder<TProperty>>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new ObjectSchema<TProperty>(),
            s => new ObjectSchemaBuilder<TProperty>(s),
            schema);
    }

    // Array
    public TSelf AddArray<TItem>(
        Expression<Func<TDomain, IEnumerable<TItem>>> selector,
        string? name = null,
        bool? required = null,
        Action<ArraySchemaBuilder<TItem>>? itemSchema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new ArraySchema<TItem>(),
            s => new ArraySchemaBuilder<TItem>(s),
            itemSchema);
    }

    // OneOf
    public TSelf AddOneOf<TBase>(
        Expression<Func<TDomain, TBase>> selector,
        string? name = null,
        bool? required = null,
        Action<OneOfSchemaBuilder<TBase>>? schema = null)
    {
        return AddSchemaProperty(
            selector,
            name,
            required ?? true,
            () => new OneOfSchema<TBase>(),
            s => new OneOfSchemaBuilder<TBase>(s),
            schema);
    }

    // Custom boolean
    public TSelf AddCustom(
        string name,
        Func<TDomain, bool> expr,
        bool? required = null,
        Func<TDomain, JsonValue>? toJson = null,
        Action<JsonValue, TDomain>? fromJson = null,
        Action<BooleanSchemaBuilder>? schema = null)
    {
        return (TSelf) this;
    }

    public TSelf AddCustom(
        string name,
        Func<TDomain, bool?> expr,
        bool? required = null,
        Func<TDomain, JsonValue>? toJson = null,
        Action<JsonValue, TDomain>? fromJson = null,
        Action<BooleanSchemaBuilder>? schema = null)
    {
        return (TSelf) this;
    }

    // Custom string
    public TSelf AddCustom(
        string name,
        Func<TDomain, string?> expr,
        bool? required = null,
        Func<TDomain, JsonValue>? toJson = null,
        Action<JsonValue, TDomain>? fromJson = null,
        Action<StringSchemaBuilder>? schema = null)
    {
        return (TSelf) this;
    }

    // Custom numeric
    public TSelf AddCustom<TNumber>(
        string name,
        Func<TDomain, TNumber> expr,
        bool? required = null,
        NumericSchemaBuilder<TNumber>? schema = null,
        Func<TDomain, JsonValue>? toJson = null,
        Action<JsonValue, TDomain>? fromJson = null)
        where TNumber : struct, INumber<TNumber>
    {
        return (TSelf) this;
    }

    public TSelf AddCustom<TNumber>(
        string name,
        Func<TDomain, TNumber?> expr,
        bool? required = null,
        Func<TDomain, JsonValue>? toJson = null,
        Action<JsonValue, TDomain>? fromJson = null,
        Action<NumericSchemaBuilder<TNumber>>? schema = null)
        where TNumber : struct, INumber<TNumber>
    {
        return (TSelf) this;
    }

    private TSelf AddSchemaProperty<TProperty, TSchema, TBuilder>(
        Expression<Func<TDomain, TProperty>> selector,
        string? name,
        bool required,
        Func<TSchema> createSchema,
        Func<TSchema, TBuilder> createBuilder,
        Action<TBuilder>? configure)
        where TSchema : ISchema
    {
        var propertyName = name ?? GetPropertyName(selector);
        var schema = createSchema();
        if (configure is not null)
        {
            configure(createBuilder(schema));
        }
        AddProperty(new ObjectProperty
        {
            Name = propertyName,
            Schema = schema,
            Required = required,
        });
        return (TSelf) this;
    }

    private static string GetPropertyName<TProperty>(Expression<Func<TDomain, TProperty>> selector)
    {
        var body = selector.Body;
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }
        if (body is MemberExpression member)
        {
            return member.Member.Name;
        }
        throw new ArgumentException(
            "Selector must be a property access expression (e.g. p => p.Property).",
            nameof(selector));
    }
}
