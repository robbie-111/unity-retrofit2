using System;
using System.Collections.Generic;
using System.Linq;
using RestEase.Attributes.Http;

namespace RestEase.Core
{
    /// <summary>
    /// 하나의 REST API 메서드에 대한 리플렉션 파싱 결과를 저장한다.
    /// RestAdapter.Awake() 시점에 캐싱되고, 이후 요청마다 런타임 데이터만 갱신된다.
    /// </summary>
    public class RequestInfo
    {
        // ── 파싱 결과 (불변) ──────────────────────────────────────────

        /// <summary>HTTP 메서드 종류.</summary>
        public HttpMethod Method { get; set; }

        /// <summary>메서드 레벨 상대 경로 (예: "users/{id}").</summary>
        public string MethodPath { get; set; }

        /// <summary>multipart/form-data 여부.</summary>
        public bool IsMultipart { get; set; }

        /// <summary>[Body] 파라미터 존재 여부.</summary>
        public bool HasBody { get; set; }

        /// <summary>[AllowAnyStatusCode] 적용 여부 (메서드 또는 인터페이스 레벨).</summary>
        public bool AllowAnyStatusCode { get; set; }

        /// <summary>정적 헤더 목록 (인터페이스 + 메서드 레벨 [Header] 합산).</summary>
        public List<(string Name, string Value)> StaticHeaders { get; set; }
            = new List<(string, string)>();

        /// <summary>각 파라미터의 역할 (순서 인덱스와 매칭).</summary>
        public List<ParamRole> ParameterRoles { get; set; } = new List<ParamRole>();

        /// <summary>각 파라미터와 연결된 이름 (Query/Path/Field/Header 키). null 이면 파라미터 이름을 사용.</summary>
        public List<string> ParameterKeys { get; set; } = new List<string>();

        /// <summary>각 파라미터의 실제 이름 (리플렉션에서 추출).</summary>
        public List<string> ParameterNames { get; set; } = new List<string>();

        // ── 런타임 데이터 (요청마다 갱신) ────────────────────────────

        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryMapParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PathParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> FieldParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> DynamicHeaders { get; set; } = new Dictionary<string, string>();
        public string BodyJson { get; set; } = string.Empty;
        public FilePart FilePart { get; set; }

        // ── 파라미터 역할 열거형 ──────────────────────────────────────

        public enum ParamRole
        {
            Query,
            QueryMap,
            Path,
            Body,
            Field,
            HeaderParam,
            Part,
            CancellationToken,  // 자동 감지, 어트리뷰트 불필요
        }

        // ── URL 조합 ─────────────────────────────────────────────────

        /// <summary>
        /// baseUrl + basePath + methodPath 를 조합하고,
        /// PathParams 치환 및 QueryParams 추가하여 완성된 URL 을 반환한다.
        /// </summary>
        public string BuildUrl(string baseUrl, string basePath)
        {
            string path = MethodPath ?? string.Empty;

            // 절대 URL이면 baseUrl, basePath 모두 무시
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return ApplyPathParams(path);

            // '/'로 시작하면 basePath 무시, baseUrl 만 사용
            string url;
            if (path.StartsWith("/"))
            {
                url = baseUrl.TrimEnd('/') + path;
            }
            else
            {
                // baseUrl + basePath + methodPath
                var segments = new List<string>();
                segments.Add(baseUrl.TrimEnd('/'));
                if (!string.IsNullOrEmpty(basePath))
                    segments.Add(basePath.Trim('/'));
                if (!string.IsNullOrEmpty(path))
                    segments.Add(path.TrimStart('/'));
                url = string.Join("/", segments);
            }

            url = ApplyPathParams(url);
            url = ApplyQueryParams(url);
            return url;
        }

        private string ApplyPathParams(string url)
        {
            foreach (var kv in PathParams)
                url = url.Replace("{" + kv.Key + "}", Uri.EscapeDataString(kv.Value));
            return url;
        }

        private string ApplyQueryParams(string url)
        {
            var all = new Dictionary<string, string>(QueryParams);
            foreach (var kv in QueryMapParams) all[kv.Key] = kv.Value;
            if (all.Count == 0) return url;

            var qs = string.Join("&", all.Select(
                kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value ?? "")));
            return url + "?" + qs;
        }

        /// <summary>요청마다 런타임 데이터를 초기화한다.</summary>
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
    }
}
