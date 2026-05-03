namespace Turbine.Tests.Unit;

public class TurbineSchemaComponentNamerTests
{
    public sealed class PersonnelSchemas : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    public sealed class Schemas : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    public sealed class Inventory : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    [Fact]
    public void Strips_trailing_Schemas_suffix()
    {
        var name = TurbineSchemaComponentNamer.GetComponentName(typeof(PersonnelSchemas), "Summary");
        Assert.Equal("Personnel_Summary", name);
    }

    [Fact]
    public void Keeps_full_type_name_when_it_is_exactly_Schemas()
    {
        var name = TurbineSchemaComponentNamer.GetComponentName(typeof(Schemas), "Foo");
        Assert.Equal("Schemas_Foo", name);
    }

    [Fact]
    public void Leaves_type_without_suffix_alone()
    {
        var name = TurbineSchemaComponentNamer.GetComponentName(typeof(Inventory), "Item");
        Assert.Equal("Inventory_Item", name);
    }

    [Fact]
    public void Throws_on_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TurbineSchemaComponentNamer.GetComponentName(null!, "Foo"));
        Assert.Throws<ArgumentNullException>(() =>
            TurbineSchemaComponentNamer.GetComponentName(typeof(PersonnelSchemas), null!));
    }
}
