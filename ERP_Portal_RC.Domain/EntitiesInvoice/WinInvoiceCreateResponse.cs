using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.EntitiesInvoice
{
    public class WinInvoiceCreateResponse
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// WinInvoice trả object khi thành công, trả [] (array rỗng) khi lỗi.
        /// Dùng WinInvoiceDataConverter để xử lý cả 2 trường hợp.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(WinInvoiceDataConverter))]
        public WinInvoiceCreateResponseData? Data { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("invRef")]
        public string? InvRef { get; set; }

        /// <summary>
        /// Giá trị có thể có: 0, 200 (không lỗi) | "ER01", "ER40"... (lỗi)
        /// WinInvoice trả về lúc number lúc string → dùng converter
        /// </summary>
        [JsonPropertyName("ErrorCode")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// WinInvoice trả "data": {} khi thành công và "data": [] khi lỗi.
    /// Converter này xử lý cả 2 dạng: array rỗng → null, object → deserialize bình thường.
    /// </summary>
    public class WinInvoiceDataConverter : JsonConverter<WinInvoiceCreateResponseData?>
    {
        public override WinInvoiceCreateResponseData? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            // WinInvoice trả [] khi lỗi → bỏ qua, trả null
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Skip();
                return null;
            }

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            // Trường hợp bình thường: object
            return JsonSerializer.Deserialize<WinInvoiceCreateResponseData>(ref reader, options);
        }

        public override void Write(
            Utf8JsonWriter writer,
            WinInvoiceCreateResponseData? value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }

    public class FlexibleStringConverter : JsonConverter<string?>
    {
        public override string? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.GetInt32().ToString(),
                JsonTokenType.Null => null,
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                _ => reader.GetString()
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            string? value,
            JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteStringValue(value);
        }
    }
}
