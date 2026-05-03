namespace Turbine;

public abstract class SchemaBuilder<TSelf>
    where TSelf : SchemaBuilder<TSelf>
{
    public TSelf Nullable(bool? nullable)
    {
        return (TSelf) this;
    }
}