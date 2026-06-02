using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using Unitrofit.Http;

namespace Unitrofit.Interceptor
{
    /// <summary>
    /// 요청/응답을 Unity Console에 출력하는 기본 로깅 인터셉터.
    /// UnityWebRequestImpl / NetHttpClientImpl 모두 동작한다.
    /// <example>
    /// var client = new UnitrofitClient.Builder()
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .Build();
    /// </example>
    /// </summary>
    public class LoggingInterceptor : IRequestInterceptor
    {
        public void OnRequest(RequestContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--> {ctx.Method} {ctx.Url}");

            sb.AppendLine("Headers:");
            foreach (var header in ctx.Headers)
                sb.AppendLine($"  {header.Key}: {header.Value}");

            if (ctx.HasBody)
            {
                sb.AppendLine("Request Body:");
                sb.AppendLine(PrettyJson(ctx.Body));
            }

            sb.Append($"--> END {ctx.Method}");
            Debug.Log(sb.ToString());
        }

        public void OnResponse(RawResponse response)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<-- {response.StatusCode} {response.Url}");

            sb.AppendLine("Response Body:");
            sb.AppendLine(PrettyJson(response.Body));

            sb.AppendLine("Headers:");
            foreach (var header in response.ResponseHeaders)
                sb.AppendLine($"  {header.Key}: {header.Value}");

            sb.Append("<-- END HTTP");
            Debug.Log(sb.ToString());
        }

        // ── JSON pretty-print. 파싱 실패 시 raw 그대로 반환 ────────────

        private static string PrettyJson(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            try
            {
                var parsed = JsonConvert.DeserializeObject<object>(raw);
                return JsonConvert.SerializeObject(parsed, Formatting.Indented);
            }
            catch (JsonException)
            {
                return raw;
            }
        }
    }
}
