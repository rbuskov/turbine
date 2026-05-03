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

    private static ObjectSchema<Person> SourcePerson()
    {
        var source = new ObjectSchema<Person>();
        var sourceBuilder = new ObjectSchemaBuilder<Person>(source);
        sourceBuilder
            .Add(p => p.Name)
            .Add(p => p.Age)
            .Add(p => p.IsActive, required: false);
        return source;
    }

    [Fact]
    public void AddPropertiesFrom_copies_each_property()
    {
        var source = SourcePerson();
        var (schema, builder) = Subject();

        builder.AddPropertiesFrom(() => source);

        Assert.Equal(new[] { "Name", "Age", "IsActive" }, schema.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void AddPropertiesFrom_shares_inner_schema_references()
    {
        var source = SourcePerson();
        var (schema, builder) = Subject();

        builder.AddPropertiesFrom(() => source);

        for (var i = 0; i < source.Properties.Count; i++)
        {
            Assert.Same(source.Properties[i].Schema, schema.Properties[i].Schema);
        }
    }

    [Fact]
    public void AddPropertiesFrom_preserves_required_by_default()
    {
        var source = SourcePerson();
        var (schema, builder) = Subject();

        builder.AddPropertiesFrom(() => source);

        Assert.True(schema.Properties[0].Required);   // Name
        Assert.True(schema.Properties[1].Required);   // Age
        Assert.False(schema.Properties[2].Required);  // IsActive
    }

    [Fact]
    public void AddPropertiesFrom_asRequired_true_marks_all_required()
    {
        var source = SourcePerson();
        var (schema, builder) = Subject();

        builder.AddPropertiesFrom(() => source, asRequired: true);

        Assert.All(schema.Properties, p => Assert.True(p.Required));
    }

    [Fact]
    public void AddPropertiesFrom_asRequired_false_marks_all_optional()
    {
        var source = SourcePerson();
        var (schema, builder) = Subject();

        builder.AddPropertiesFrom(() => source, asRequired: false);

        Assert.All(schema.Properties, p => Assert.False(p.Required));
    }

    [Fact]
    public void AddPropertiesFrom_does_not_modify_source()
    {
        var source = SourcePerson();
        var (_, builder) = Subject();

        builder.AddPropertiesFrom(() => source, asRequired: true);

        Assert.False(source.Properties[2].Required);  // IsActive in source still optional
    }

    [Fact]
    public void AddPropertiesFrom_returns_same_builder_for_chaining()
    {
        var source = SourcePerson();
        var (_, builder) = Subject();

        Assert.Same(builder, builder.AddPropertiesFrom(() => source));
    }

    [Fact]
    public void AddPropertiesFrom_resolves_func_at_invocation_time()
    {
        var source = new ObjectSchema<Person>();
        var (schema, builder) = Subject();

        builder.AddPropertiesFrom(() => source);

        // Func evaluated at AddPropertiesFrom call time — empty at that point.
        Assert.Empty(schema.Properties);
    }

    [Fact]
    public void Remove_by_selector_drops_existing_property()
    {
        var (schema, builder) = Subject();
        builder.Add(p => p.Name).Add(p => p.Age);

        builder.Remove(p => p.Age);

        Assert.Single(schema.Properties);
        Assert.Equal("Name", schema.Properties[0].Name);
    }

    [Fact]
    public void Remove_by_selector_handles_value_type_via_nullable_lift()
    {
        var (schema, builder) = Subject();
        builder.Add(p => p.IsActive);

        builder.Remove(p => p.IsActive);

        Assert.Empty(schema.Properties);
    }

    [Fact]
    public void Remove_by_name_drops_existing_property()
    {
        var (schema, builder) = Subject();
        builder.Add(p => p.Name).Add(p => p.Age);

        builder.Remove("Name");

        Assert.Single(schema.Properties);
        Assert.Equal("Age", schema.Properties[0].Name);
    }

    [Fact]
    public void Remove_unknown_property_is_no_op()
    {
        var (schema, builder) = Subject();
        builder.Add(p => p.Name);

        builder.Remove("DoesNotExist");

        Assert.Single(schema.Properties);
    }

    [Fact]
    public void Remove_returns_same_builder_for_chaining()
    {
        var (_, builder) = Subject();
        builder.Add(p => p.Name);

        Assert.Same(builder, builder.Remove(p => p.Name));
        Assert.Same(builder, builder.Remove("Anything"));
    }

    private sealed class Atomics
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public int? OptionalAge { get; set; }
        public bool IsActive { get; set; }
        public bool? IsVerified { get; set; }
        public DateOnly Birthday { get; set; }
        public DateOnly? Anniversary { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Tier Tier { get; set; }
        public Tier? OptionalTier { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public Address NestedObject { get; set; } = new();
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
    }

    private static (ObjectSchema<Atomics> schema, ObjectSchemaBuilder<Atomics> builder) AtomicSubject()
    {
        var schema = new ObjectSchema<Atomics>();
        return (schema, new ObjectSchemaBuilder<Atomics>(schema));
    }

    [Fact]
    public void AddAtomicProperties_registers_string()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties();

        var name = Assert.Single(schema.Properties, p => p.Name == "Name");
        Assert.IsType<StringSchema>(name.Schema);
        Assert.True(name.Required);
    }

    [Fact]
    public void AddAtomicProperties_registers_numeric_required_and_optional()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties();

        var age = Assert.Single(schema.Properties, p => p.Name == "Age");
        Assert.IsType<NumericSchema<int>>(age.Schema);
        Assert.True(age.Required);

        var optAge = Assert.Single(schema.Properties, p => p.Name == "OptionalAge");
        Assert.IsType<NumericSchema<int>>(optAge.Schema);
        Assert.False(optAge.Required);

        var price = Assert.Single(schema.Properties, p => p.Name == "Price");
        Assert.IsType<NumericSchema<decimal>>(price.Schema);
        Assert.True(price.Required);

        var discount = Assert.Single(schema.Properties, p => p.Name == "Discount");
        Assert.IsType<NumericSchema<decimal>>(discount.Schema);
        Assert.False(discount.Required);
    }

    [Fact]
    public void AddAtomicProperties_registers_boolean_required_and_optional()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties();

        var isActive = Assert.Single(schema.Properties, p => p.Name == "IsActive");
        Assert.IsType<BooleanSchema>(isActive.Schema);
        Assert.True(isActive.Required);

        var isVerified = Assert.Single(schema.Properties, p => p.Name == "IsVerified");
        Assert.IsType<BooleanSchema>(isVerified.Schema);
        Assert.False(isVerified.Required);
    }

    [Fact]
    public void AddAtomicProperties_registers_date_only_required_and_optional()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties();

        var birthday = Assert.Single(schema.Properties, p => p.Name == "Birthday");
        Assert.IsType<DateOnlySchema>(birthday.Schema);
        Assert.True(birthday.Required);

        var anniversary = Assert.Single(schema.Properties, p => p.Name == "Anniversary");
        Assert.IsType<DateOnlySchema>(anniversary.Schema);
        Assert.False(anniversary.Required);
    }

    [Fact]
    public void AddAtomicProperties_registers_date_time_offset_required_and_optional()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties();

        var createdAt = Assert.Single(schema.Properties, p => p.Name == "CreatedAt");
        Assert.IsType<DateTimeOffsetSchema>(createdAt.Schema);
        Assert.True(createdAt.Required);

        var updatedAt = Assert.Single(schema.Properties, p => p.Name == "UpdatedAt");
        Assert.IsType<DateTimeOffsetSchema>(updatedAt.Schema);
        Assert.False(updatedAt.Required);
    }

    [Fact]
    public void AddAtomicProperties_registers_enum_required_and_optional()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties();

        var tier = Assert.Single(schema.Properties, p => p.Name == "Tier");
        Assert.IsType<EnumSchema<Tier>>(tier.Schema);
        Assert.True(tier.Required);

        var optTier = Assert.Single(schema.Properties, p => p.Name == "OptionalTier");
        Assert.IsType<EnumSchema<Tier>>(optTier.Schema);
        Assert.False(optTier.Required);
    }

    [Fact]
    public void AddAtomicProperties_skips_reference_and_collection_types()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties();

        Assert.DoesNotContain(schema.Properties, p => p.Name == "NestedObject");
        Assert.DoesNotContain(schema.Properties, p => p.Name == "Tags");
    }

    [Fact]
    public void AddAtomicProperties_asRequired_true_marks_all_required()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties(asRequired: true);

        Assert.All(schema.Properties, p => Assert.True(p.Required));
    }

    [Fact]
    public void AddAtomicProperties_asRequired_false_marks_all_optional()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties(asRequired: false);

        Assert.All(schema.Properties, p => Assert.False(p.Required));
    }

    [Fact]
    public void AddAtomicProperties_returns_same_builder_for_chaining()
    {
        var (_, builder) = AtomicSubject();

        Assert.Same(builder, builder.AddAtomicProperties());
    }

    [Fact]
    public void AddAtomicProperties_can_be_followed_by_Remove()
    {
        var (schema, builder) = AtomicSubject();

        builder.AddAtomicProperties().Remove(p => p.Age);

        Assert.DoesNotContain(schema.Properties, p => p.Name == "Age");
        Assert.Contains(schema.Properties, p => p.Name == "Name");
    }
}
