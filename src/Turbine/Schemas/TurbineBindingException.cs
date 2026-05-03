namespace Turbine;

/// <summary>
/// Thrown when JSON input cannot be bound to a schema — e.g. missing required
/// discriminator, unknown discriminator value, or malformed enum literal. Distinct
/// from general failures so handlers can map these to HTTP 400 Bad Request rather
/// than letting them surface as 500.
/// </summary>
public sealed class TurbineBindingException : Exception
{
    public TurbineBindingException(string message) : base(message) { }
}
