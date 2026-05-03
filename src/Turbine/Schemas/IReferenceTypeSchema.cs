using System.Text.Json;

namespace Turbine;

public interface IReferenceTypeSchema<T> : IValueTypeSchema<T>
{
    void FromJson(JsonElement json, T value);
}