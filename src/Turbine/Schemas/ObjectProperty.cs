namespace Turbine;

public class ObjectProperty
{
    public string Name { get; set; } = null!;
    public ISchema Schema { get; set; } = null!;
    public bool Required { get; set; }
}