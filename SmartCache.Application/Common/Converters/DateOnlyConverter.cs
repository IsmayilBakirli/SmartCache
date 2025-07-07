using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartCache.Application.Common.Converters
{
    public class DateOnlyConverter<T> : JsonConverter<T> where T : struct
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss";

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();

            if (string.IsNullOrEmpty(value))
            {
                if (Nullable.GetUnderlyingType(typeof(T)) != null)
                    return default;
                throw new JsonException("Cannot convert null or empty string to DateTime.");
            }

            return (T)(object)DateTime.Parse(value);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var dateTime = (DateTime)(object)value;
            writer.WriteStringValue(dateTime.ToString(Format));
        }
    }
}
