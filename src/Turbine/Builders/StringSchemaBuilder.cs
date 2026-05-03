using System.Text.RegularExpressions;

namespace Turbine;

public class StringSchemaBuilder : SchemaBuilder<StringSchemaBuilder>
{
    internal StringSchemaBuilder() { }

    public StringSchemaBuilder MinLength(int minLength)
    {
        return this;
    }
    
    public StringSchemaBuilder MaxLength(int maxLength)
    {
        return this;
    }

    public StringSchemaBuilder Format(StringFormat format)
    {
        return this;
    }

    public StringSchemaBuilder Pattern(string pattern)
    {
        return this;
    }
}