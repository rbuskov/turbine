namespace Turbine;

internal sealed record TurbineEndpointSchemaMetadata(
    Type ConfigurationType,
    string SchemaPropertyName,
    EndpointSchemaRole Role,
    int? StatusCode,
    string ContentType)
{
    public const string DefaultContentType = "application/json";
}
