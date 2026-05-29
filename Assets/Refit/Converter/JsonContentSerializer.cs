using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Refit.Attributes.Params;

namespace Refit.Converter
{
    /// <summary>
    /// Newtonsoft.Json 기반 기본 IContentSerializer 구현체.
    /// </summary>
    public class JsonContentSerializer : IContentSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonContentSerializer(JsonSerializerSettings settings = null)
        {
            _settings = settings ?? new JsonSerializerSettings
            {
                NullValueHandling     = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };
        }

        public T Deserialize<T>(string body)
        {
            if (string.IsNullOrEmpty(body))
                throw new InvalidOperationException("Response body is empty.");
            try { return JsonConvert.DeserializeObject<T>(body, _settings); }
            catch (Exception e)
            { throw new InvalidOperationException($"Failed to deserialize into {typeof(T).Name}: {e.Message}", e); }
        }

        public string Serialize(object data)
        {
            if (data == null) return string.Empty;
            return JsonConvert.SerializeObject(data, _settings);
        }

        /// <summary>
        /// POCO 의 public readable 프로퍼티를 form-urlencoded 딕셔너리로 변환한다.
        ///
        /// 이름 결정 우선순위:
        /// 1. [AliasAs] 어트리뷰트
        /// 2. [JsonProperty(PropertyName)] 어트리뷰트
        /// 3. 프로퍼티 실제 이름
        ///
        /// private getter 프로퍼티는 제외된다.
        /// </summary>
        public Dictionary<string, string> SerializeFormUrlEncoded(object data)
        {
            var result = new Dictionary<string, string>();
            if (data == null) return result;

            // Dictionary<string, object> 직접 지원
            if (data is IDictionary dict)
            {
                foreach (DictionaryEntry entry in dict)
                    if (entry.Value != null)
                        result[entry.Key.ToString()] = entry.Value.ToString();
                return result;
            }

            // POCO 리플렉션 처리
            foreach (PropertyInfo prop in data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // private getter 제외
                if (prop.GetMethod == null || !prop.GetMethod.IsPublic) continue;

                object value = prop.GetValue(data);
                if (value == null) continue;

                string key = GetFormKey(prop);
                result[key] = value.ToString();
            }

            return result;
        }

        private static string GetFormKey(PropertyInfo prop)
        {
            // 1. [AliasAs] 최우선
            var aliasAs = prop.GetCustomAttribute<AliasAsAttribute>();
            if (aliasAs != null) return aliasAs.Name;

            // 2. [JsonProperty(PropertyName)]
            var jsonProp = prop.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>();
            if (jsonProp?.PropertyName != null) return jsonProp.PropertyName;

            // 3. 프로퍼티 이름
            return prop.Name;
        }
    }
}
