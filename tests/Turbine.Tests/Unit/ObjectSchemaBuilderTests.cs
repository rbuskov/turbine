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

    private sealed class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
    }

    private sealed class PersonWithAddress
    {
        public string Name { get; set; } = "";
        public Address Home { get; set; } = new();
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
        public IEnumerable<Address> PreviousAddresses { get; set; } = Array.Empty<Address>();
        public Animal Pet { get; set; } = new Dog();
    }

    private abstract class Animal
    {
        public string Name { get; set; } = "";
    }

    private sealed class Dog : Animal { }

    private sealed class Cat : Animal { }

    private static (ObjectSchema<PersonWithAddress> schema, ObjectSchemaBuilder<PersonWithAddress> builder) AddressSubject()
    {
        var schema = new ObjectSchema<PersonWithAddress>();
        return (schema, new ObjectSchemaBuilder<PersonWithAddress>(schema));
    }

    [Fact]
    public void AddObject_creates_nested_object_property()
    {
        var (schema, builder) = AddressSubject();

        builder.AddObject(p => p.Home);

        var property = Assert.Single(schema.Properties);
        Assert.Equal("Home", property.Name);
        Assert.IsType<ObjectSchema<Address>>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void AddObject_runs_nested_configure_callback()
    {
        var (schema, builder) = AddressSubject();

        builder.AddObject(p => p.Home, schema: home =>
        {
            home.Add(a => a.Street);
            home.Add(a => a.City);
        });

        var nested = Assert.IsType<ObjectSchema<Address>>(schema.Properties[0].Schema);
        Assert.Equal(new[] { "Street", "City" }, nested.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void AddObject_explicit_name_overrides_selector()
    {
        var (schema, builder) = AddressSubject();

        builder.AddObject(p => p.Home, name: "address");

        Assert.Equal("address", schema.Properties[0].Name);
    }

    [Fact]
    public void AddObject_can_override_required()
    {
        var (schema, builder) = AddressSubject();

        builder.AddObject(p => p.Home, required: false);

        Assert.False(schema.Properties[0].Required);
    }

    [Fact]
    public void AddArray_creates_array_property()
    {
        var (schema, builder) = AddressSubject();

        builder.AddArray(p => p.Tags);

        var property = Assert.Single(schema.Properties);
        Assert.Equal("Tags", property.Name);
        Assert.IsType<ArraySchema<string>>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void AddArray_runs_item_schema_callback()
    {
        var (schema, builder) = AddressSubject();

        builder.AddArray(p => p.Tags, itemSchema: items => items.MinItems(1).MaxItems(10));

        var array = Assert.IsType<ArraySchema<string>>(schema.Properties[0].Schema);
        Assert.Equal(1, array.MinItems);
        Assert.Equal(10, array.MaxItems);
    }

    [Fact]
    public void AddArray_with_complex_item_type()
    {
        var (schema, builder) = AddressSubject();

        builder.AddArray(p => p.PreviousAddresses);

        Assert.IsType<ArraySchema<Address>>(schema.Properties[0].Schema);
    }

    [Fact]
    public void AddOneOf_creates_one_of_property()
    {
        var (schema, builder) = AddressSubject();

        builder.AddOneOf(p => p.Pet);

        var property = Assert.Single(schema.Properties);
        Assert.Equal("Pet", property.Name);
        Assert.IsType<OneOfSchema<Animal>>(property.Schema);
        Assert.True(property.Required);
    }

    [Fact]
    public void AddOneOf_runs_configure_callback()
    {
        var (schema, builder) = AddressSubject();

        builder.AddOneOf(p => p.Pet, schema: o =>
        {
            o.Discriminator("kind");
            o.AddMapping<Dog>(_ => { });
            o.AddMapping<Cat>(_ => { });
        });

        var oneOf = Assert.IsType<OneOfSchema<Animal>>(schema.Properties[0].Schema);
        Assert.Equal("kind", oneOf.Discriminator);
        Assert.Equal(2, oneOf.Mappings.Count);
    }

    [Fact]
    public void Reference_type_Adds_return_same_builder_for_chaining()
    {
        var (_, builder) = AddressSubject();

        Assert.Same(builder, builder.AddObject(p => p.Home));
        Assert.Same(builder, builder.AddArray(p => p.Tags));
        Assert.Same(builder, builder.AddOneOf(p => p.Pet));
    }
}
