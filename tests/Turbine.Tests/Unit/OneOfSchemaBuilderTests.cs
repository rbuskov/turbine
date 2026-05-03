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

    private static OneOfSchema<Animal> SourceWithDogAndCat()
    {
        var source = new OneOfSchema<Animal>();
        var sourceBuilder = new OneOfSchemaBuilder<Animal>(source);
        sourceBuilder.AddMapping<Dog>(b => b.Add(d => d.Breed));
        sourceBuilder.AddMapping<Cat>(b => b.Add(c => c.Indoor));
        return source;
    }

    [Fact]
    public void AddMappingsFrom_copies_all_mappings()
    {
        var source = SourceWithDogAndCat();
        var (target, builder) = Subject();

        builder.AddMappingsFrom(() => source);

        Assert.Equal(2, target.Mappings.Count);
        Assert.Contains("Dog", target.Mappings.Keys);
        Assert.Contains("Cat", target.Mappings.Keys);
    }

    [Fact]
    public void AddMappingsFrom_shares_object_schema_when_asRequired_is_null()
    {
        var source = SourceWithDogAndCat();
        var (target, builder) = Subject();

        builder.AddMappingsFrom(() => source);

        Assert.Same(source.Mappings["Dog"], target.Mappings["Dog"]);
        Assert.Same(source.Mappings["Cat"], target.Mappings["Cat"]);
    }

    [Fact]
    public void AddMappingsFrom_asRequired_false_marks_inner_properties_optional()
    {
        var source = SourceWithDogAndCat();
        var (target, builder) = Subject();

        builder.AddMappingsFrom(() => source, asRequired: false);

        var dog = (ObjectSchema<Dog>) target.Mappings["Dog"];
        Assert.All(dog.Properties, p => Assert.False(p.Required));
    }

    [Fact]
    public void AddMappingsFrom_asRequired_true_marks_inner_properties_required()
    {
        var source = SourceWithDogAndCat();
        // Pre-mark source's Cat property as optional so the override is observable.
        ((ObjectSchema<Cat>) source.Mappings["Cat"]).Properties[0].Required = false;

        var (target, builder) = Subject();
        builder.AddMappingsFrom(() => source, asRequired: true);

        var cat = (ObjectSchema<Cat>) target.Mappings["Cat"];
        Assert.All(cat.Properties, p => Assert.True(p.Required));
    }

    [Fact]
    public void AddMappingsFrom_asRequired_does_not_mutate_source()
    {
        var source = SourceWithDogAndCat();
        var (_, builder) = Subject();

        builder.AddMappingsFrom(() => source, asRequired: false);

        var sourceDog = (ObjectSchema<Dog>) source.Mappings["Dog"];
        Assert.True(sourceDog.Properties[0].Required); // Breed remains required on source
    }

    [Fact]
    public void AddMappingsFrom_asRequired_does_not_share_object_schema()
    {
        var source = SourceWithDogAndCat();
        var (target, builder) = Subject();

        builder.AddMappingsFrom(() => source, asRequired: false);

        Assert.NotSame(source.Mappings["Dog"], target.Mappings["Dog"]);
    }

    [Fact]
    public void AddMappingsFrom_throws_on_null_source_func()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentNullException>(() => builder.AddMappingsFrom<Animal>(null!));
    }

    [Fact]
    public void AddMappingsFrom_throws_on_duplicate_mapping_key()
    {
        var source = SourceWithDogAndCat();
        var (_, builder) = Subject();
        builder.AddMapping<Dog>(_ => { });

        Assert.Throws<ArgumentException>(() => builder.AddMappingsFrom(() => source));
    }

    [Fact]
    public void AddMappingsFrom_returns_same_builder_for_chaining()
    {
        var source = SourceWithDogAndCat();
        var (_, builder) = Subject();

        Assert.Same(builder, builder.AddMappingsFrom(() => source));
    }
}
