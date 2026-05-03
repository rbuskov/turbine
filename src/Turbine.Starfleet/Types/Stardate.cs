namespace Turbine.Starfleet.Types;

/// <summary>
/// Represents a Star Trek stardate using the TNG convention:
/// 1000 units per Earth year, with the decimal representing fraction of day.
/// Epoch: January 1, 2323 00:00:00 UTC = stardate 0.0
/// </summary>
public readonly struct Stardate : IEquatable<Stardate>, IComparable<Stardate>, IFormattable
{
    // TNG convention: 1000 stardate units per year (Julian year = 365.25 days)
    public const double UnitsPerYear = 1000.0;
    public const double UnitsPerDay = UnitsPerYear / 365.25;   // ≈ 2.7378508
    public const double UnitsPerHour = UnitsPerDay / 24.0;     // ≈ 0.1140771

    /// <summary>Epoch: January 1, 2323 UTC = stardate 0.0 (canonical TNG-ish anchor).</summary>
    public static readonly DateTime Epoch = new(2323, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public double Value { get; }
    public double RoundedValue => Math.Round(Value, 1);  // Rounded to one decimal place, e.g. 78451.3

    public Stardate(double value) => Value = value;

    // ---- Conversions ----

    public static Stardate FromDateTimeOffset(DateTimeOffset dto)
    {
        var days = (dto.UtcDateTime - Epoch).TotalDays;
        return new Stardate(days * UnitsPerDay);
    }

    public DateTimeOffset ToDateTimeOffset()
    {
        var days = Value / UnitsPerDay;
        return new DateTimeOffset(Epoch, TimeSpan.Zero).AddDays(days);
    }
    
    // ---- Operators ----

    public static implicit operator double(Stardate s) => s.Value;
    public static explicit operator Stardate(double v) => new(v);

    public static Stardate operator +(Stardate a, double units) => new(a.Value + units);
    public static Stardate operator -(Stardate a, double units) => new(a.Value - units);
    public static double   operator -(Stardate a, Stardate b)   => a.Value - b.Value;

    public static bool operator ==(Stardate a, Stardate b) => a.Value == b.Value;
    public static bool operator !=(Stardate a, Stardate b) => a.Value != b.Value;
    public static bool operator  <(Stardate a, Stardate b) => a.Value  < b.Value;
    public static bool operator  >(Stardate a, Stardate b) => a.Value  > b.Value;
    public static bool operator <=(Stardate a, Stardate b) => a.Value <= b.Value;
    public static bool operator >=(Stardate a, Stardate b) => a.Value >= b.Value;

    // ---- Equality / comparison / formatting ----

    public bool Equals(Stardate other) => Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is Stardate s && Equals(s);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(Stardate other) => Value.CompareTo(other.Value);

    /// <summary>Default format: one decimal place, e.g. "78451.3".</summary>
    public override string ToString() => ToString("F1", null);

    public string ToString(string? format, IFormatProvider? provider)
        => Value.ToString(format ?? "F1", provider ?? System.Globalization.CultureInfo.InvariantCulture);

    // ---- Parsing ----

    public static Stardate Parse(string s)
        => new(double.Parse(s, System.Globalization.CultureInfo.InvariantCulture));

    public static bool TryParse(string? s, out Stardate result)
    {
        if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var v))
        {
            result = new Stardate(v);
            return true;
        }
        result = default;
        return false;
    }
}