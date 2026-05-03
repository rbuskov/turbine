using System.Reflection;

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

    /// <summary>
    /// The reflective handle to the source CLR property when the schema property was added
    /// via a property selector (e.g. <c>Add(p =&gt; p.Name)</c>). Null for custom properties
    /// added via <c>AddCustom</c> — those carry their accessor in <see cref="ValueExpression"/>.
    /// </summary>
    internal PropertyInfo? Member { get; set; }
}
