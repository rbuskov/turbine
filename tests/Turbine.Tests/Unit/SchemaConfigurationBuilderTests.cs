namespace Turbine.Tests.Unit;

public class SchemaConfigurationBuilderTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private abstract class Animal
    {
        public string Name { get; set; } = "";
    }

    private sealed class Dog : Animal { }

    private sealed class Holder
    {
        public StringSchema NameSchema { get; set; } = null!;
        public BooleanSchema FlagSchema { get; set; } = null!;
        public NumericSchema<int> AgeSchema { get; set; } = null!;
        public ObjectSchema<Person> PersonSchema { get; set; } = null!;
        public ArraySchema<string> TagsSchema { get; set; } = null!;
        public OneOfSchema<Animal> AnimalSchema { get; set; } = null!;

        public StringSchema FieldSchema = null!;
    }

    private static SchemaConfigurationBuilder NewBuilder() => new();

    [Fact]
    public void Schema_assigns_StringSchema_to_property()
    {
        var holder = new Holder();
        var builder = NewBuilder();

        var stringBuilder = builder.Schema(() => holder.NameSchema);
        stringBuilder.MinLength(3);

        Assert.NotNull(holder.NameSchema);
        Assert.Equal(3, holder.NameSchema.MinLength);
    }

    [Fact]
    public void Schema_assigns_BooleanSchema_to_property()
    {
        var holder = new Holder();
        var builder = NewBuilder();

        builder.Schema(() => holder.FlagSchema).Nullable(true);

        Assert.NotNull(holder.FlagSchema);
        Assert.True(holder.FlagSchema.Nullable);
    }

    [Fact]
    public void Schema_assigns_NumericSchema_to_property()
    {
        var holder = new Holder();
        var builder = NewBuilder();

        builder.Schema(() => holder.AgeSchema).Minimum(0).Maximum(150);

        Assert.NotNull(holder.AgeSchema);
        Assert.Equal(0, holder.AgeSchema.Minimum);
        Assert.Equal(150, holder.AgeSchema.Maximum);
    }

    [Fact]
    public void Schema_assigns_ObjectSchema_to_property()
    {
        var holder = new Holder();
        var builder = NewBuilder();

        builder.Schema(() => holder.PersonSchema)
            .Add(p => p.Name)
            .Add(p => p.Age);

        Assert.NotNull(holder.PersonSchema);
        Assert.Equal(new[] { "Name", "Age" }, holder.PersonSchema.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Schema_assigns_ArraySchema_to_property()
    {
        var holder = new Holder();
        var builder = NewBuilder();

        builder.Schema(() => holder.TagsSchema).MinItems(1);

        Assert.NotNull(holder.TagsSchema);
        Assert.Equal(1, holder.TagsSchema.MinItems);
    }

    [Fact]
    public void Schema_assigns_OneOfSchema_to_property()
    {
        var holder = new Holder();
        var builder = NewBuilder();

        builder.Schema(() => holder.AnimalSchema).AddMapping<Dog>(_ => { });

        Assert.NotNull(holder.AnimalSchema);
        Assert.True(holder.AnimalSchema.Mappings.ContainsKey("Dog"));
    }

    [Fact]
    public void Schema_assigns_to_field_not_just_property()
    {
        var holder = new Holder();
        var builder = NewBuilder();

        builder.Schema(() => holder.FieldSchema).MinLength(2);

        Assert.NotNull(holder.FieldSchema);
        Assert.Equal(2, holder.FieldSchema.MinLength);
    }

    [Fact]
    public void Schema_creates_independent_instances_per_call()
    {
        var first = new Holder();
        var second = new Holder();
        var builder = NewBuilder();

        builder.Schema(() => first.NameSchema);
        builder.Schema(() => second.NameSchema);

        Assert.NotNull(first.NameSchema);
        Assert.NotNull(second.NameSchema);
        Assert.NotSame(first.NameSchema, second.NameSchema);
    }

    [Fact]
    public void Schema_throws_on_non_member_expression()
    {
        var builder = NewBuilder();

        Assert.Throws<ArgumentException>(() => builder.Schema(() => CreateString()));
    }

    private static StringSchema CreateString() => new();

    [Fact]
    public void Schema_works_inside_SchemaConfiguration_subclass()
    {
        var configuration = new ResourceSchemas();

        configuration.Configure(new SchemaConfigurationBuilder());

        Assert.NotNull(configuration.Summary);
        Assert.NotNull(configuration.CreateResult);
        Assert.Equal(new[] { "Id", "Name" }, configuration.Summary.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(new[] { "Id" }, configuration.CreateResult.Properties.Select(p => p.Name).ToArray());
    }

    private sealed class ResourceSchemas : SchemaConfiguration
    {
        public ObjectSchema<Person> Summary { get; set; } = null!;
        public ObjectSchema<Person> CreateResult { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Summary)
                .Add(p => p.Name, name: "Id")
                .Add(p => p.Name);
            builder.Schema(() => CreateResult)
                .Add(p => p.Name, name: "Id");
        }
    }
}
