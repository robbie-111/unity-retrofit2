using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unitrofit.Attributes.Http;

namespace Unitrofit.Core
{
    /// <summary>
    /// 인터페이스 메서드를 어트리뷰트로 파싱한 결과를 캐싱하는 단위.
    /// Awake 시점에 한 번 파싱하고, 이후 런타임 데이터를 채워 재사용한다.
    /// </summary>
    public class RequestInfo
    {
        // ── 파싱 시점에 결정되는 불변 데이터 ────────────────────────────

        public HttpMethod        Method           { get; set; }
        public string            MethodPath       { get; set; }
        public bool              IsMultipart      { get; set; }
        public bool              HasBody          { get; set; }
        public bool              AllowAnyStatusCode { get; set; }
        public ReturnKind        ReturnType       { get; set; }

        /// <summary>인터페이스+메서드 레벨 정적 헤더 (Key, Value). Value=null 이면 제거.</summary>
        public List<(string Name, string Value)> StaticHeaders { get; } = new List<(string, string)>();

        public List<ParamRole>   ParameterRoles  { get; } = new List<ParamRole>();
        public List<string>      ParameterKeys   { get; } = new List<string>();
        public List<string>      ParameterNames  { get; } = new List<string>();

        // ── 런타임마다 채워지는 가변 데이터 ─────────────────────────────

        public Dictionary<string, string> QueryParams     { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryMapParams  { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> PathParams      { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> FieldParams     { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> DynamicHeaders  { get; } = new Dictionary<string, string>();

        public string        BodyJson  { get; set; } = string.Empty;
        public MultipartBody FilePart  { get; set; }

        // ── 열거형 ────────────────────────────────────────────────────────

        public enum ParamRole
        {
            Path, Query, QueryMap, Body, Field, Header, Part, CancellationToken,
        }

        /// <summary>메서드 반환 타입 종류.</summary>
        public enum ReturnKind
        {
            /// <summary>UniTask&lt;T&gt; 또는 Task&lt;T&gt; (제네릭 결과)</summary>
            UniTaskOfT,
            /// <summary>UniTask (void 결과)</summary>
            UniTask,
            /// <summary>Callback&lt;T&gt; 패턴 (첫 파라미터가 Callback)</summary>
            Callback,
        }

        // ── URL 조합 ─────────────────────────────────────────────────────

        public string BuildUrl(string baseUrl)
        {
            string path = MethodPath ?? string.Empty;

            // 절대 URL 이면 그대로 사용
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return ApplyPathParams(ApplyQueryParams(path));

            string url = baseUrl.TrimEnd('/') +
                         (path.StartsWith("/") ? path : "/" + path);

            url = ApplyPathParams(url);
            url = ApplyQueryParams(url);
            return url;
        }

        private string ApplyPathParams(string url)
        {
            foreach (var kv in PathParams)
                url = url.Replace("{" + kv.Key + "}", Uri.EscapeDataString(kv.Value ?? ""));
            return url;
        }

        private string ApplyQueryParams(string url)
        {
            var all = new Dictionary<string, string>(QueryParams);
            foreach (var kv in QueryMapParams)
                all[kv.Key] = kv.Value;

            if (all.Count == 0) return url;

            var sb = new StringBuilder(url).Append('?');
            bool first = true;
            foreach (var kv in all)
            {
                if (!first) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kv.Key))
                  .Append('=')
                  .Append(Uri.EscapeDataString(kv.Value ?? ""));
                first = false;
            }
            return sb.ToString();
        }

        // ── 런타임 리셋 ──────────────────────────────────────────────────

        public void ResetRuntimeData()
        {
            QueryParams.Clear();
            QueryMapParams.Clear();
            PathParams.Clear();
            FieldParams.Clear();
            DynamicHeaders.Clear();
            BodyJson = string.Empty;
            FilePart = null;
        }

        // ── 정적 헤더 파싱 헬퍼 ─────────────────────────────────────────

        /// <summary>"Key: Value" 문자열을 파싱하여 StaticHeaders에 적용한다.</summary>
        public void ApplyHeaderString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            int colon = raw.IndexOf(':');
            if (colon < 0)
            {
                // 콜론 없음 = 해당 키 제거 신호
                string key = raw.Trim();
                StaticHeaders.RemoveAll(h => h.Name == key);
                return;
            }
            string k = raw.Substring(0, colon).Trim();
            string v = raw.Substring(colon + 1).Trim();
            StaticHeaders.RemoveAll(h => h.Name == k);
            StaticHeaders.Add((k, v));
        }
    }
}
