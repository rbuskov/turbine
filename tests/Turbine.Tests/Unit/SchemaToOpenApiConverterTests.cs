using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Turbine.Tests.Unit;

public class SchemaToOpenApiConverterTests
{
    public sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private static SchemaToOpenApiConverter Converter() => new();

    [Fact]
    public void Converts_StringSchema_with_constraints()
    {
        var schema = new StringSchema
        {
            MinLength = 2,
            MaxLength = 30,
            Pattern = new Regex("^x"),
            Format = "email",
        };
        var result = Converter().Convert(schema);
        Assert.Equal(JsonSchemaType.String, result.Type);
        Assert.Equal(2, result.MinLength);
        Assert.Equal(30, result.MaxLength);
        Assert.Equal("^x", result.Pattern);
        Assert.Equal("email", result.Format);
    }

    [Fact]
    public void Marks_nullable_string_with_null_flag()
    {
        var schema = new StringSchema { Nullable = true };
        var result = Converter().Convert(schema);
        Assert.Equal(JsonSchemaType.String | JsonSchemaType.Null, result.Type);
    }

    [Fact]
    public void Converts_BooleanSchema()
    {
        var result = Converter().Convert(new BooleanSchema());
        Assert.Equal(JsonSchemaType.Boolean, result.Type);
    }

    [Fact]
    public void Converts_DateOnly_with_date_format()
    {
        var date = Converter().Convert(new DateOnlySchema());
        Assert.Equal(JsonSchemaType.String, date.Type);
        Assert.Equal("date", date.Format);
    }

    [Fact]
    public void Converts_DateTimeOffset_with_date_time_format()
    {
        var dto = Converter().Convert(new DateTimeOffsetSchema());
        Assert.Equal(JsonSchemaType.String, dto.Type);
        Assert.Equal("date-time", dto.Format);
    }

    [Fact]
    public void Converts_NumericSchema_int_with_inclusive_bounds()
    {
        var schema = new NumericSchema<int>
        {
            Minimum = 1,
            Maximum = 100,
            MultipleOf = 5,
        };
        var result = Converter().Convert(schema);
        Assert.Equal(JsonSchemaType.Integer, result.Type);
        Assert.Equal("int32", result.Format);
        Assert.Equal("1", result.Minimum);
        Assert.Equal("100", result.Maximum);
        Assert.Equal(5m, result.MultipleOf);
    }

    [Fact]
    public void Exclusive_bounds_take_precedence_over_inclusive_bounds_in_OpenApi_output()
    {
        var schema = new NumericSchema<int>
        {
            Minimum = 1,
            Maximum = 100,
            ExclusiveMinimum = 0,
            ExclusiveMaximum = 200,
        };
        var result = Converter().Convert(schema);
        Assert.Equal("0", result.ExclusiveMinimum);
        Assert.Equal("200", result.ExclusiveMaximum);
    }

    [Fact]
    public void Converts_NumericSchema_long_to_int64_format()
    {
        var result = Converter().Convert(new NumericSchema<long>());
        Assert.Equal(JsonSchemaType.Integer, result.Type);
        Assert.Equal("int64", result.Format);
    }

    [Fact]
    public void Converts_NumericSchema_double_to_number_with_format()
    {
        var result = Converter().Convert(new NumericSchema<double>());
        Assert.Equal(JsonSchemaType.Number, result.Type);
        Assert.Equal("double", result.Format);
    }

    [Fact]
    public void Converts_EnumSchema_to_string_with_names()
    {
        var result = Converter().Convert(new EnumSchema<DayOfWeek>());
        Assert.Equal(JsonSchemaType.String, result.Type);
        Assert.NotNull(result.Enum);
        Assert.Contains(result.Enum, n => n!.GetValue<string>() == "Monday");
        Assert.Contains(result.Enum, n => n!.GetValue<string>() == "Sunday");
    }

    [Fact]
    public void Converts_ObjectSchema_with_required_properties()
    {
        var obj = new ObjectSchema<Person>();
        obj.Properties.Add(new ObjectProperty { Name = "Name", Schema = new StringSchema(), Required = true });
        obj.Properties.Add(new ObjectProperty { Name = "Age", Schema = new NumericSchema<int>(), Required = false });
        var result = Converter().Convert(obj);
        Assert.Equal(JsonSchemaType.Object, result.Type);
        Assert.True(result.Properties!.ContainsKey("Name"));
        Assert.True(result.Properties.ContainsKey("Age"));
        Assert.Contains("Name", result.Required!);
        Assert.DoesNotContain("Age", result.Required!);
    }

    [Fact]
    public void Converts_ArraySchema_with_inline_item_schema()
    {
        var inner = new ObjectSchema<Person>();
        inner.Properties.Add(new ObjectProperty { Name = "Name", Schema = new StringSchema(), Required = true });
        var array = new ArraySchema<Person>
        {
            MinItems = 1,
            MaxItems = 10,
            Items = inner,
        };
        var result = Converter().Convert(array);
        Assert.Equal(JsonSchemaType.Array, result.Type);
        Assert.Equal(1, result.MinItems);
        Assert.Equal(10, result.MaxItems);
        Assert.NotNull(result.Items);
        Assert.Equal(JsonSchemaType.Object, result.Items!.Type);
    }

    [Fact]
    public void Refs_owned_nested_schemas_when_resolver_returns_a_reference()
    {
        var owned = new StringSchema { MinLength = 5 };
        var converter = new SchemaToOpenApiConverter(s =>
            ReferenceEquals(s, owned)
                ? new OpenApiSchemaReference("Owned_Foo", new OpenApiDocument(), null)
                : null);

        var obj = new ObjectSchema<Person>();
        obj.Properties.Add(new ObjectProperty { Name = "Foo", Schema = owned, Required = true });
        var converted = converter.Convert(obj);
        var prop = converted.Properties!["Foo"];
        Assert.IsType<OpenApiSchemaReference>(prop);
    }

    [Fact]
    public void Inlines_nested_schemas_when_resolver_returns_null()
    {
        var anonymous = new StringSchema { MinLength = 1 };
        var obj = new ObjectSchema<Person>();
        obj.Properties.Add(new ObjectProperty { Name = "Foo", Schema = anonymous, Required = true });
        var converted = Converter().Convert(obj);
        var prop = converted.Properties!["Foo"];
        Assert.IsNotType<OpenApiSchemaReference>(prop);
        Assert.Equal(JsonSchemaType.String, prop.Type);
    }

    [Fact]
    public void Converts_OneOfSchema_with_discriminator_and_oneOf_entries()
    {
        var oneOf = new OneOfSchema<Person> { Discriminator = "Kind" };
        var alpha = new ObjectSchema<Person>();
        var beta = new ObjectSchema<Person>();
        oneOf.AddSchema("alpha", alpha);
        oneOf.AddSchema("beta", beta);
        var result = Converter().Convert(oneOf);
        Assert.Equal(2, result.OneOf!.Count);
        Assert.Equal("Kind", result.Discriminator!.PropertyName);
    }
}
