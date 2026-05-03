namespace Turbine.Tests.Unit;

public class StringSchemaBuilderTests
{
    private static (StringSchema schema, StringSchemaBuilder builder) Subject()
    {
        var schema = new StringSchema();
        return (schema, new StringSchemaBuilder(schema));
    }

    [Fact]
    public void MinLength_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.MinLength(5);

        Assert.Equal(5, schema.MinLength);
    }

    [Fact]
    public void MinLength_zero_is_valid()
    {
        var (schema, builder) = Subject();

        builder.MinLength(0);

        Assert.Equal(0, schema.MinLength);
    }

    [Fact]
    public void MinLength_throws_on_negative()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.MinLength(-1));
    }

    [Fact]
    public void MaxLength_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.MaxLength(120);

        Assert.Equal(120, schema.MaxLength);
    }

    [Fact]
    public void MaxLength_throws_on_negative()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.MaxLength(-1));
    }

    [Theory]
    [InlineData(StringFormat.Date, "date")]
    [InlineData(StringFormat.DateTime, "date-time")]
    [InlineData(StringFormat.Email, "email")]
    [InlineData(StringFormat.Hostname, "hostname")]
    [InlineData(StringFormat.IPv4, "ipv4")]
    [InlineData(StringFormat.IPv6, "ipv6")]
    [InlineData(StringFormat.Uri, "uri")]
    [InlineData(StringFormat.UriReference, "uri-reference")]
    [InlineData(StringFormat.UriTemplate, "uri-template")]
    public void Format_writes_openapi_format_string(StringFormat input, string expected)
    {
        var (schema, builder) = Subject();

        builder.Format(input);

        Assert.Equal(expected, schema.Format);
    }

    [Fact]
    public void Format_throws_on_undefined_enum_value()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Format((StringFormat) 999));
    }

    [Fact]
    public void Pattern_compiles_regex_into_schema()
    {
        var (schema, builder) = Subject();

        builder.Pattern("^[A-Z]{3}$");

        Assert.NotNull(schema.Pattern);
        Assert.Equal("^[A-Z]{3}$", schema.Pattern!.ToString());
    }

    [Fact]
    public void Pattern_throws_on_invalid_regex()
    {
        var (_, builder) = Subject();

        Assert.ThrowsAny<ArgumentException>(() => builder.Pattern("["));
    }

    [Fact]
    public void Methods_return_same_builder_for_chaining()
    {
        var (_, builder) = Subject();

        Assert.Same(builder, builder.MinLength(1));
        Assert.Same(builder, builder.MaxLength(2));
        Assert.Same(builder, builder.Format(StringFormat.Email));
        Assert.Same(builder, builder.Pattern(".*"));
        Assert.Same(builder, builder.Nullable(true));
    }

    [Fact]
    public void Later_call_overwrites_earlier_call()
    {
        var (schema, builder) = Subject();

        builder.MinLength(5).MinLength(10);

        Assert.Equal(10, schema.MinLength);
    }

}
