namespace Turbine;

public class ObjectSchemaBuilder<TDomain> : PropertySchemaBuilder<TDomain, ObjectSchemaBuilder<TDomain>>
{
    internal ObjectSchema<TDomain> Schema { get; }

    internal ObjectSchemaBuilder(ObjectSchema<TDomain> schema)
    {
        Schema = schema;
    }

    internal override void AddProperty(ObjectProperty property)
    {
        Schema.Properties.Add(property);
    }

    internal override void RemoveProperty(string propertyName)
    {
        for (var i = Schema.Properties.Count - 1; i >= 0; i--)
        {
            if (Schema.Properties[i].Name == propertyName)
            {
                Schema.Properties.RemoveAt(i);
            }
        }
    }
}
