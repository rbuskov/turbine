namespace Turbine.Starfleet.Types;

// Shift "now" 400 years forward - this is science fiction after all.
internal sealed class ScienceFictionTimeProvider(TimeProvider inner) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => inner.GetUtcNow().AddYears(400);
}
