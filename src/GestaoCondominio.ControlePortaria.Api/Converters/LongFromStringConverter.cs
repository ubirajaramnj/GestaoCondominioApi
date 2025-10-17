namespace GestaoCondominio.ControlePortaria.Api.Converters
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class LongFromStringConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var strValue = reader.GetString();
                if (long.TryParse(strValue, out var value))
                    return value;

                throw new JsonException($"Não foi possível converter '{strValue}' para long.");
            }

            return reader.GetInt64();
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
