using System;
using Newtonsoft.Json;

namespace RestEase.Converter
{
    /// <summary>
    /// Newtonsoft.Json 기반 기본 IResponseDeserializer 구현체.
    /// </summary>
    public class JsonResponseDeserializer : IResponseDeserializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public T Deserialize<T>(string body)
        {
            if (string.IsNullOrEmpty(body))
                throw new InvalidOperationException("Response body is empty.");
            try
            {
                return JsonConvert.DeserializeObject<T>(body, Settings);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize response into {typeof(T).Name}: {e.Message}", e);
            }
        }

        public string Serialize(object data)
        {
            if (data == null) return string.Empty;
            return JsonConvert.SerializeObject(data, Settings);
        }
    }
}
