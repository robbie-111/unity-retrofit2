using Newtonsoft.Json;

namespace Unitrofit.Converter
{
    /// <summary>
    /// Newtonsoft.Json 기반 기본 컨버터.
    /// </summary>
    public class JsonConverter : IConverter
    {
        private readonly JsonSerializerSettings _settings;

        public JsonConverter(JsonSerializerSettings settings = null)
        {
            _settings = settings ?? new JsonSerializerSettings
            {
                NullValueHandling    = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };
        }

        public T FromBody<T>(string body)
        {
            return JsonConvert.DeserializeObject<T>(body, _settings);
        }

        public string ToBody(object data)
        {
            return JsonConvert.SerializeObject(data, _settings);
        }
    }
}
