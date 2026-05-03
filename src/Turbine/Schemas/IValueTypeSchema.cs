using System.Text.Json;

namespace Turbine;

public interface IValueTypeSchema<T> : ISchema
{
    T FromJson(JsonElement json);
    JsonElement ToJson(T value);
}