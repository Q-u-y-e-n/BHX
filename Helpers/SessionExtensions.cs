using System.Text.Json; // <--- Dùng cái này thay cho Newtonsoft
using Microsoft.AspNetCore.Http;

namespace BHX_Web.Helpers
{
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            // Sửa JsonConvert.SerializeObject -> JsonSerializer.Serialize
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);

            // Sửa JsonConvert.DeserializeObject -> JsonSerializer.Deserialize
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}