using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServerForToDoList.Services
{
    public class TimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return null;

            if (TimeSpan.TryParse(value, out var result))
                return result;

            return null;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString(@"hh\:mm"));
        }
    }
}