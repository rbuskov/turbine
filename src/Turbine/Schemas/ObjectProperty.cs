namespace Turbine;

public class ObjectProperty
{
    internal ObjectProperty() { }

    public string Name { get; set; } = null!;
    public ISchema Schema { get; set; } = null!;
    public bool Required { get; set; }

    public Delegate? ValueExpression { get; set; }
    public Delegate? ToJson { get; set; }
    public Delegate? FromJson { get; set; }
}