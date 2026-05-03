namespace Turbine.Tests.Unit;

public class OneOfSchemaBuilderTests
{
    private abstract class Animal
    {
        public string Name { get; set; } = "";
    }

    private sealed class Dog : Animal
    {
        public string Breed { get; set; } = "";
    }

    private sealed class Cat : Animal
    {
        public bool Indoor { get; set; }
    }

    private static (OneOfSchema<Animal> schema, OneOfSchemaBuilder<Animal> builder) Subject()
    {
        var schema = new OneOfSchema<Animal>();
        return (schema, new OneOfSchemaBuilder<Animal>(schema));
    }

    [Fact]
    public void Discriminator_default_is_Type()
    {
        var (schema, _) = Subject();

        Assert.Equal("Type", schema.Discriminator);
    }

    [Fact]
    public void Discriminator_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.Discriminator("kind");

        Assert.Equal("kind", schema.Discriminator);
    }

    [Fact]
    public void Discriminator_throws_on_null()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentNullException>(() => builder.Discriminator(null));
    }

    [Fact]
    public void Nullable_default_is_unset()
    {
        var (schema, _) = Subject();

        Assert.Null(schema.Nullable);
    }

    [Fact]
    public void Nullable_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.Nullable(true);

        Assert.True(schema.Nullable);
    }

    [Fact]
    public void AddMapping_registers_mapping_under_TMap_name()
    {
        var (schema, builder) = Subject();

        builder.AddMapping<Dog>(_ => { });

        Assert.True(schema.Mappings.ContainsKey("Dog"));
        Assert.IsType<ObjectSchema<Dog>>(schema.Mappings["Dog"]);
    }

    [Fact]
    public void AddMapping_invokes_configurator_with_typed_builder()
    {
        var (_, builder) = Subject();
        ObjectSchemaBuilder<Dog>? captured = null;

        builder.AddMapping<Dog>(b => captured = b);

        Assert.NotNull(captured);
    }

    [Fact]
    public void AddMapping_handles_null_configurator()
    {
        var (schema, builder) = Subject();

        builder.AddMapping<Dog>(null);

        Assert.True(schema.Mappings.ContainsKey("Dog"));
    }

    [Fact]
    public void AddMapping_supports_multiple_mappings()
    {
        var (schema, builder) = Subject();

        builder.AddMapping<Dog>(_ => { }).AddMapping<Cat>(_ => { });

        Assert.Equal(2, schema.Mappings.Count);
        Assert.Contains("Dog", schema.Mappings.Keys);
        Assert.Contains("Cat", schema.Mappings.Keys);
    }

    [Fact]
    public void AddMapping_throws_on_duplicate_key()
    {
        var (_, builder) = Subject();
        builder.AddMapping<Dog>(_ => { });

        Assert.Throws<ArgumentException>(() => builder.AddMapping<Dog>(_ => { }));
    }

    [Fact]
    public void Methods_return_same_builder_for_chaining()
    {
        var (_, builder) = Subject();

        Assert.Same(builder, builder.Nullable(true));
        Assert.Same(builder, builder.Discriminator("kind"));
        Assert.Same(builder, builder.AddMapping<Dog>(_ => { }));
    }
}
