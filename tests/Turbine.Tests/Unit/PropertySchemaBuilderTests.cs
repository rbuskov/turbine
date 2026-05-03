namespace Turbine.Tests.Unit;

public class PropertySchemaBuilderTests
{
    private sealed class TestDomain
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private sealed class CapturingBuilder : PropertySchemaBuilder<TestDomain, CapturingBuilder>
    {
        public List<ObjectProperty> Added { get; } = new();
        public List<string> Removed { get; } = new();

        internal CapturingBuilder() { }

        internal override void AddProperty(ObjectProperty property)
        {
            Added.Add(property);
        }

        internal override void RemoveProperty(string propertyName)
        {
            Removed.Add(propertyName);
        }
    }

    [Fact]
    public void Add_dispatches_to_AddProperty_hook()
    {
        var builder = new CapturingBuilder();

        builder.Add(p => p.Name);

        var property = Assert.Single(builder.Added);
        Assert.Equal("Name", property.Name);
        Assert.IsType<StringSchema>(property.Schema);
    }

    [Fact]
    public void Remove_dispatches_to_RemoveProperty_hook()
    {
        var builder = new CapturingBuilder();

        builder.Remove("Anything");

        Assert.Equal(new[] { "Anything" }, builder.Removed.ToArray());
    }

    [Fact]
    public void Remove_by_selector_passes_property_name_to_hook()
    {
        var builder = new CapturingBuilder();

        builder.Remove(p => p.Age);

        Assert.Equal(new[] { "Age" }, builder.Removed.ToArray());
    }

    [Fact]
    public void Remove_by_name_throws_on_null()
    {
        var builder = new CapturingBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.Remove((string) null!));
    }

    [Fact]
    public void AddPropertiesFrom_throws_on_null_source_func()
    {
        var builder = new CapturingBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddPropertiesFrom<TestDomain>(null!));
    }
}
