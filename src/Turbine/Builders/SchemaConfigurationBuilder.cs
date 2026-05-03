using System.Linq.Expressions;
using System.Numerics;

namespace Turbine;

public class SchemaConfigurationBuilder
{
    internal SchemaConfigurationBuilder() { }

    public BooleanSchemaBuilder Schema(Expression<Func<BooleanSchema>> propertySelector, string? name = null)
    {
        return new BooleanSchemaBuilder();
    }
    
    public StringSchemaBuilder Schema(Expression<Func<StringSchema>> propertySelector, string? name = null)
    {
        return new StringSchemaBuilder(new StringSchema());
    }

    public NumericSchemaBuilder<TNumber> Schema<TNumber>(Expression<Func<NumericSchema<TNumber>>> propertySelector, string? name = null)
        where TNumber : struct, INumber<TNumber>
    {
        return new NumericSchemaBuilder<TNumber>(new NumericSchema<TNumber>());
    }

    public ObjectSchemaBuilder<TDomain> Schema<TDomain>(Expression<Func<ObjectSchema<TDomain>>> propertySelector, string? name = null) 
    {
        return new ObjectSchemaBuilder<TDomain>();
    }
    
    public ArraySchemaBuilder<TItem> Schema<TItem>(Expression<Func<ArraySchema<TItem>>> propertySelector, string? name = null)
    {
        return new ArraySchemaBuilder<TItem>();
    }
    
    public OneOfSchemaBuilder<TBase> Schema<TBase>(Expression<Func<OneOfSchema<TBase>>> propertySelector, string? name = null)
    {
        return new OneOfSchemaBuilder<TBase>();
    }
}