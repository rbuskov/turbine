using System.Text.RegularExpressions;

namespace Turbine;

public class StringSchemaBuilder : SchemaBuilder<StringSchemaBuilder>
{
    internal StringSchema Schema { get; }

    internal StringSchemaBuilder(StringSchema schema)
    {
        Schema = schema;
    }

    public StringSchemaBuilder MinLength(int minLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        Schema.MinLength = minLength;
        return this;
    }

    public StringSchemaBuilder MaxLength(int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        Schema.MaxLength = maxLength;
        return this;
    }

    public StringSchemaBuilder Format(StringFormat format)
    {
        Schema.Format = format switch
        {
            StringFormat.Date => "date",
            StringFormat.DateTime => "date-time",
            StringFormat.Email => "email",
            StringFormat.Hostname => "hostname",
            StringFormat.IPv4 => "ipv4",
            StringFormat.IPv6 => "ipv6",
            StringFormat.Uri => "uri",
            StringFormat.UriReference => "uri-reference",
            StringFormat.UriTemplate => "uri-template",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };
        return this;
    }

    public StringSchemaBuilder Pattern(string pattern)
    {
        Schema.Pattern = new Regex(pattern);
        return this;
    }
}
