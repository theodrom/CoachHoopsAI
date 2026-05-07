using System.Text.Json;

namespace CoachHoopsAI.Admin.Helpers
{
    public class JsonHelpers
    {
        private static readonly JsonSerializerOptions CompactJsonOpts = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// Serializes an object to JSON with the same settings as the API, so it can be sent back in query parameters.
        /// </summary>
        public static string SerializeForApi(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }

        public static T? TryDeserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            try
            {
                return JsonSerializer.Deserialize<T>(json, CompactJsonOpts);
            }
            catch
            {
                return default;
            }
        }
    }
}
