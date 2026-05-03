namespace Turbine.Tests.Unit;

public class ObjectSchemaBuilderTests
{
    private enum Tier
    {
        Basic,
        Premium,
    }

    private sealed class Person
    {
        public int Age { get; set; }
        public int? OptionalAge { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
        public bool? IsVerified { get; set; }
        public DateOnly Birthday { get; set; }
        public DateOnly? Anniversary { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Tier Tier { get; set; }
    }

    private static (ObjectSchema<Person> schema, ObjectSchemaBuilder<Person> builder) Subject()
    {
        var schema = new ObjectSchema<Person>();
        return (schema, new ObjectSchemaBuilder<Person>(schema));
    }

    private static ObjectProperty Single(ObjectSchema<Person> schema)
    {
        Assert.Single(schema.Properties);
        return schema.Properties[0];
    }

    [Fact]
    public void Add_string_creates_property_with_string_schema()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Name);

        var property = Single(schema);
        Assert.Equal("Name", property.Name);
        Assert.IsType<StringSchema>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void Add_string_with_explicit_name_overrides_selector()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Name, name: "fullName");

        Assert.Equal("fullName", Single(schema).Name);
    }

    [Fact]
    public void Add_string_runs_configure_callback()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Name, schema: s => s.MinLength(2).MaxLength(50));

        var stringSchema = Assert.IsType<StringSchema>(Single(schema).Schema);
        Assert.Equal(2, stringSchema.MinLength);
        Assert.Equal(50, stringSchema.MaxLength);
    }

    [Fact]
    public void Add_string_can_override_required()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Name, required: false);

        Assert.False(Single(schema).Required);
    }

    [Fact]
    public void Add_int_creates_numeric_schema_required_by_default()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Age);

        var property = Single(schema);
        Assert.Equal("Age", property.Name);
        Assert.IsType<NumericSchema<int>>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void Add_nullable_int_creates_numeric_schema_optional_by_default()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.OptionalAge);

        var property = Single(schema);
        Assert.Equal("OptionalAge", property.Name);
        Assert.IsType<NumericSchema<int>>(property.Schema);
        Assert.False(property.Required);
    }

    [Fact]
    public void Add_int_runs_configure_callback()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Age, schema: s => s.Minimum(0).Maximum(150));

        var numericSchema = Assert.IsType<NumericSchema<int>>(Single(schema).Schema);
        Assert.Equal(0, numericSchema.Minimum);
        Assert.Equal(150, numericSchema.Maximum);
    }

    [Fact]
    public void Add_bool_creates_boolean_schema_required_by_default()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.IsActive);

        var property = Single(schema);
        Assert.Equal("IsActive", property.Name);
        Assert.IsType<BooleanSchema>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void Add_nullable_bool_creates_boolean_schema_optional_by_default()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.IsVerified);

        var property = Single(schema);
        Assert.IsType<BooleanSchema>(property.Schema);
        Assert.False(property.Required);
    }

    [Fact]
    public void Add_DateOnly_creates_date_only_schema()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Birthday);

        var property = Single(schema);
        Assert.Equal("Birthday", property.Name);
        Assert.IsType<DateOnlySchema>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void Add_nullable_DateOnly_is_optional_by_default()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Anniversary);

        Assert.False(Single(schema).Required);
    }

    [Fact]
    public void Add_DateTimeOffset_creates_date_time_offset_schema()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.CreatedAt);

        var property = Single(schema);
        Assert.IsType<DateTimeOffsetSchema>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void Add_nullable_DateTimeOffset_is_optional_by_default()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.UpdatedAt);

        Assert.False(Single(schema).Required);
    }

    [Fact]
    public void Add_enum_creates_enum_schema()
    {
        var (schema, builder) = Subject();

        builder.Add(p => p.Tier);

        var property = Single(schema);
        Assert.Equal("Tier", property.Name);
        Assert.IsType<EnumSchema<Tier>>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void Add_preserves_call_order()
    {
        var (schema, builder) = Subject();

        builder
            .Add(p => p.Name)
            .Add(p => p.Age)
            .Add(p => p.IsActive);

        Assert.Equal(new[] { "Name", "Age", "IsActive" }, schema.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Add_returns_same_builder_for_chaining()
    {
        var (_, builder) = Subject();

        Assert.Same(builder, builder.Add(p => p.Name));
        Assert.Same(builder, builder.Add(p => p.Age));
        Assert.Same(builder, builder.Add(p => p.IsActive));
        Assert.Same(builder, builder.Add(p => p.Tier));
    }

    [Fact]
    public void Add_throws_on_non_member_expression()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentException>(() => builder.Add(p => p.Name + "!"));
    }
}
