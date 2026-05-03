namespace Turbine;

public abstract class SchemaBuilder<TSelf>
    where TSelf : SchemaBuilder<TSelf>
{
    internal SchemaBuilder() { }

    public virtual TSelf Nullable(bool? nullable)
    {
        return (TSelf) this;
    }
}
