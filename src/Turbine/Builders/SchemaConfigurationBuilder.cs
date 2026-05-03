using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace Turbine;

public class SchemaConfigurationBuilder
{
    internal SchemaConfigurationBuilder() { }

    public BooleanSchemaBuilder Schema(Expression<Func<BooleanSchema>> propertySelector, string? name = null)
    {
        var schema = AcquireSchema(propertySelector, () => new BooleanSchema());
        return new BooleanSchemaBuilder(schema);
    }

    public StringSchemaBuilder Schema(Expression<Func<StringSchema>> propertySelector, string? name = null)
    {
        var schema = AcquireSchema(propertySelector, () => new StringSchema());
        return new StringSchemaBuilder(schema);
    }

    public NumericSchemaBuilder<TNumber> Schema<TNumber>(Expression<Func<NumericSchema<TNumber>>> propertySelector, string? name = null)
        where TNumber : struct, INumber<TNumber>
    {
        var schema = AcquireSchema(propertySelector, () => new NumericSchema<TNumber>());
        return new NumericSchemaBuilder<TNumber>(schema);
    }

    public ObjectSchemaBuilder<TDomain> Schema<TDomain>(Expression<Func<ObjectSchema<TDomain>>> propertySelector, string? name = null)
    {
        var schema = AcquireSchema(propertySelector, () => new ObjectSchema<TDomain>());
        return new ObjectSchemaBuilder<TDomain>(schema);
    }

    public ArraySchemaBuilder<TItem> Schema<TItem>(Expression<Func<ArraySchema<TItem>>> propertySelector, string? name = null)
    {
        var schema = AcquireSchema(propertySelector, () => new ArraySchema<TItem>());
        return new ArraySchemaBuilder<TItem>(schema);
    }

    public OneOfSchemaBuilder<TBase> Schema<TBase>(Expression<Func<OneOfSchema<TBase>>> propertySelector, string? name = null)
    {
        var schema = AcquireSchema(propertySelector, () => new OneOfSchema<TBase>());
        return new OneOfSchemaBuilder<TBase>(schema);
    }

    private static TSchema AcquireSchema<TSchema>(Expression<Func<TSchema>> selector, Func<TSchema> factory)
        where TSchema : class, ISchema
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

        TSchema schema;
        switch (member.Member)
        {
            case PropertyInfo property:
            {
                var existing = property.GetValue(instance) as TSchema;
                if (existing is not null)
                {
                    schema = existing;
                    break;
                }
                schema = factory();
                property.SetValue(instance, schema);
                break;
            }
            case FieldInfo field:
            {
                var existing = field.GetValue(instance) as TSchema;
                if (existing is not null)
                {
                    schema = existing;
                    break;
                }
                schema = factory();
                field.SetValue(instance, schema);
                break;
            }
            default:
                throw new ArgumentException(
                    "Selector must reference a property or field.",
                    nameof(selector));
        }

        var ctx = TurbineBuildContext.Current;
        if (ctx is not null && ctx.CurrentConfig is not null)
        {
            ctx.RegisterOwner(schema, ctx.CurrentConfig, member.Member.Name);
            ctx.CurrentOuterSchemaProperty = member.Member.Name;
        }

        return schema;
    }
}
