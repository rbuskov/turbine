using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json.Nodes;

namespace Turbine;

public abstract class PropertySchemaBuilder<TDomain, TSelf> : SchemaBuilder<TSelf>
    where TSelf : PropertySchemaBuilder<TDomain, TSelf>
{
    public TSelf AddPropertiesFrom<TSource>(Func<ObjectSchema<TSource>> schema, bool? asRequired = null)
    {
        return (TSelf) this;
    }
    
    public TSelf AddAtomicProperties(bool? asRequired = null)
    {
        return (TSelf) this;
    }

    public TSelf Remove<TProperty>(Expression<Func<TDomain, TProperty?>> selector)
    {
        return (TSelf) this;
    }

    public TSelf Remove(string propertyName)
    {
        return (TSelf) this;
    }

    // Enum
    public TSelf Add(
        Expression<Func<TDomain, Enum>> selector, 
        string? name = null,
        bool? required = null,
        Action<EnumSchemaBuilder>? schema = null) // Todo: EnumSchemaBuilder
    {
        return (TSelf) this;
    }

    // Boolean
    public TSelf Add(
        Expression<Func<TDomain, bool>> selector, 
        string? name = null,
        bool? required = null,
        Action<BooleanSchemaBuilder>? schema = null)
    {
        return (TSelf) this;
    }
    
    public TSelf Add(
        Expression<Func<TDomain, bool?>> selector, 
        string? name = null,
        bool? required = null,
        Action<BooleanSchemaBuilder>? schema = null)
    {
        return(TSelf) this;
    }
    
    // DateTimeOffset
    public TSelf Add(
        Expression<Func<TDomain, DateTimeOffset>> selector, 
        string? name = null,
        bool? required = null,
        Action<DateTimeOffsetSchemaBuilder>? schema = null)
    {
        return (TSelf) this;
    }
    
    public TSelf Add(
        Expression<Func<TDomain, DateTimeOffset?>> selector, 
        string? name = null,
        bool? required = null,
        Action<DateTimeOffsetSchemaBuilder>? schema = null)
    {
        return(TSelf) this;
    }
    
    // DateOnly
    public TSelf Add(
        Expression<Func<TDomain, DateOnly>> selector, 
        string? name = null,
        bool? required = null,
        Action<DateOnlySchemaBuilder>? schema = null)
    {
        return (TSelf) this;
    }
    
    public TSelf Add(
        Expression<Func<TDomain, DateOnly?>> selector, 
        string? name = null,
        bool? required = null,
        Action<DateOnlySchemaBuilder>? schema = null)
    {
        return(TSelf) this;
    }
    
    // String (no string overload, since string and string? are equivalent at runtime)
    public TSelf Add(
        Expression<Func<TDomain, string?>> selector,
        string? name = null,
        bool? required = null,
        Action<StringSchemaBuilder>? schema = null)
    {
        return(TSelf) this;
    }

    // Numeric
    public TSelf Add<TNumber>(
        Expression<Func<TDomain, TNumber>> selector,
        string? name = null,
        bool? required = null,
        Action<NumericSchemaBuilder<TNumber>>? schema = null)
        where TNumber : struct, INumber<TNumber>
    {
        return (TSelf) this;
    }
    
    public TSelf Add<TNumber>(
        Expression<Func<TDomain, TNumber?>> selector,
        string? name = null,
        bool? required = null,
        Action<NumericSchemaBuilder<TNumber>>? schema = null)
        where TNumber : struct, INumber<TNumber>
    {
        return (TSelf) this;
    }
    
    // Object
    public TSelf AddObject<TProperty>(
        Expression<Func<TDomain, TProperty>> selector,
        string? name = null,
        bool? required = null,
        Action<ObjectSchemaBuilder<TProperty>>? schema = null)
    {
        return(TSelf) this;
    }
    
    // Array
    public TSelf AddArray<TItem>(
        Expression<Func<TDomain, IEnumerable<TItem>>> selector,
        string? name = null,
        bool? required = null,
        Action<ArraySchemaBuilder<TItem>>? itemSchema = null)
    {
        return (TSelf) this;
    }
    
    // OneOf
    public TSelf AddOneOf<TBase>(
        Expression<Func<TDomain, TBase>> selector,
        string? name = null,
        bool? required = null,
        Action<OneOfSchemaBuilder<TBase>>? schema = null)
    {
        return (TSelf) this;
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

}