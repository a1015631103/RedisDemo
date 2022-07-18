using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace RedisDemo.Extends
{
    public static class JsonExtensions
    {
        private static JsonSerializerOptions defaultSerializerSettings = new JsonSerializerOptions()
        { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };


        public static string SerializeCustom<TValue>(TValue value)
        {
            return JsonSerializer.Serialize<TValue>(value, defaultSerializerSettings);
        }

        public static TValue DeserializeCustom<TValue>(string value)
        {
            return JsonSerializer.Deserialize<TValue>(value, defaultSerializerSettings);
        }
    }
}
