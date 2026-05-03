namespace Turbine;

public interface IObjectSchema : ISchema
{
    internal IList<ObjectProperty> Properties { get; }
}