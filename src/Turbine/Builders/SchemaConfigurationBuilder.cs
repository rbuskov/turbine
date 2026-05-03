using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace Turbine;

public class SchemaConfigurationBuilder
{
    internal SchemaConfigurationBuilder() { }

    public BooleanSchemaBuilder Schema(Expression<Func<BooleanSchema>> propertySelector, string? name = null)
    {
        var schema = new BooleanSchema();
        AssignSchema(propertySelector, schema);
        return new BooleanSchemaBuilder(schema);
    }

    public StringSchemaBuilder Schema(Expression<Func<StringSchema>> propertySelector, string? name = null)
    {
        var schema = new StringSchema();
        AssignSchema(propertySelector, schema);
        return new StringSchemaBuilder(schema);
    }

    public NumericSchemaBuilder<TNumber> Schema<TNumber>(Expression<Func<NumericSchema<TNumber>>> propertySelector, string? name = null)
        where TNumber : struct, INumber<TNumber>
    {
        var schema = new NumericSchema<TNumber>();
        AssignSchema(propertySelector, schema);
        return new NumericSchemaBuilder<TNumber>(schema);
    }

    public ObjectSchemaBuilder<TDomain> Schema<TDomain>(Expression<Func<ObjectSchema<TDomain>>> propertySelector, string? name = null)
    {
        var schema = new ObjectSchema<TDomain>();
        AssignSchema(propertySelector, schema);
        return new ObjectSchemaBuilder<TDomain>(schema);
    }

    public ArraySchemaBuilder<TItem> Schema<TItem>(Expression<Func<ArraySchema<TItem>>> propertySelector, string? name = null)
    {
        var schema = new ArraySchema<TItem>();
        AssignSchema(propertySelector, schema);
        return new ArraySchemaBuilder<TItem>(schema);
    }

    public OneOfSchemaBuilder<TBase> Schema<TBase>(Expression<Func<OneOfSchema<TBase>>> propertySelector, string? name = null)
    {
        var schema = new OneOfSchema<TBase>();
        AssignSchema(propertySelector, schema);
        return new OneOfSchemaBuilder<TBase>(schema);
    }

    private static void AssignSchema<TSchema>(Expression<Func<TSchema>> selector, TSchema schema)
    {
        ArgumentNullException.ThrowIfNull(selector);
        if (selector.Body is not MemberExpression member)
        {
            throw new ArgumentException(
                "Selector must reference a property or field (e.g. () => MySchema).",
                nameof(selector));
        }

        var instance = member.Expression is null
            ? null
            : Expression.Lambda(member.Expression).Compile().DynamicInvoke();

        switch (member.Member)
        {
            case PropertyInfo property:
                property.SetValue(instance, schema);
                break;
            case FieldInfo field:
                field.SetValue(instance, schema);
                break;
            default:
                throw new ArgumentException(
                    "Selector must reference a property or field.",
                    nameof(selector));
        }
    }
}
