using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Sparky
{
    // TODO: This is absolutely not safe, will fix eventually.
    public static class Configuration
    {
        private const string _path = "cfg/config.json";

        private static JObject _cache;

        static Configuration()
        {
            _cache = JObject.Parse(File.ReadAllText(_path));
        }

        public static void Write<TValue>(string key, TValue value)
        {
            _cache[key] = JToken.Parse(JsonConvert.SerializeObject(value));
            File.WriteAllText(_path, _cache.ToString());
        }

        public static TValue Get<TValue>(string key)
            => _cache.SelectToken(key).ToObject<TValue>();
    }
}