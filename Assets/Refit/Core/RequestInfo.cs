using System;
using System.Collections.Generic;
using System.Linq;
using Refit.Attributes.Http;

namespace Refit.Core
{
    /// <summary>
    /// 하나의 REST API 메서드에 대한 리플렉션 파싱 결과를 저장한다.
    /// RestAdapter.Awake() 시점에 캐싱되고, 요청마다 런타임 데이터만 갱신된다.
    /// </summary>
    public class RequestInfo
    {
        // ── 파싱 결과 (불변) ──────────────────────────────────────────

        public HttpMethod Method { get; set; }
        public string MethodPath { get; set; }
        public bool IsMultipart { get; set; }
        public string MultipartBoundary { get; set; } = MultipartAttribute.DefaultBoundary;
        public bool HasBody { get; set; }
        public bool AllowAnyStatusCode { get; set; }
        public bool QueryUriUnescaped { get; set; }

        /// <summary>
        /// [Body] 파라미터의 직렬화 방식. ParseParam() 에서 캐싱되고
        /// FillRuntimeData() 에서 BodyInfo.Method 에 그대로 사용된다.
        /// </summary>
        public Attributes.Params.BodySerializationMethod BodySerializationMethod { get; set; }
            = Attributes.Params.BodySerializationMethod.Default;

        /// <summary>정적 헤더 목록 (인터페이스 + 메서드 레벨 [Headers] 합산, 처리 후)</summary>
        public List<(string Name, string Value)> StaticHeaders { get; set; }
            = new List<(string, string)>();

        public List<ParamRole> ParameterRoles { get; set; } = new List<ParamRole>();
        public List<string> ParameterKeys { get; set; } = new List<string>();
        public List<string> ParameterNames { get; set; } = new List<string>();

        // ── 런타임 데이터 (요청마다 갱신) ────────────────────────────

        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PathParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> DynamicHeaders { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();

        // Body
        public string BodyJson { get; set; } = string.Empty;
        public BodySerializationInfo BodyInfo { get; set; }

        // Multipart 파트들
        public List<MultipartItem> MultipartItems { get; set; } = new List<MultipartItem>();

        // ── 파라미터 역할 ─────────────────────────────────────────────

        public enum ParamRole
        {
            Query,
            Path,
            Body,
            Header,
            HeaderCollection,
            Authorize,
            Part,
            Property,
            CancellationToken,
        }

        public class BodySerializationInfo
        {
            public Attributes.Params.BodySerializationMethod Method { get; set; }
            public object RawValue { get; set; }
        }

        public class MultipartItem
        {
            public string PartName { get; set; }
            public object Value { get; set; }   // StreamPart | ByteArrayPart | FilePart | string
            public string ContentType { get; set; }
            public string FileName { get; set; }
        }

        // ── URL 조합 ─────────────────────────────────────────────────

        public string BuildUrl(string baseUrl, string basePath)
        {
            string path = MethodPath ?? string.Empty;

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return ApplyPathParams(path);

            string url;
            if (path.StartsWith("/"))
                url = baseUrl.TrimEnd('/') + path;
            else
            {
                var segments = new List<string> { baseUrl.TrimEnd('/') };
                if (!string.IsNullOrEmpty(basePath)) segments.Add(basePath.Trim('/'));
                if (!string.IsNullOrEmpty(path))     segments.Add(path.TrimStart('/'));
                url = string.Join("/", segments);
            }

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
            if (QueryParams.Count == 0) return url;
            var encode = !QueryUriUnescaped
                ? (Func<string, string>)Uri.EscapeDataString
                : s => s;
            var qs = string.Join("&", QueryParams.Select(
                kv => encode(kv.Key) + "=" + encode(kv.Value ?? "")));
            return url + "?" + qs;
        }

        public void ResetRuntimeData()
        {
            QueryParams.Clear();
            PathParams.Clear();
            DynamicHeaders.Clear();
            CustomProperties.Clear();
            BodyJson = string.Empty;
            BodyInfo = null;
            MultipartItems.Clear();
        }

        // ── 정적 헤더 파싱 헬퍼 ──────────────────────────────────────

        /// <summary>"Key: Value" 문자열을 파싱하여 StaticHeaders 에 병합한다.</summary>
        public void ApplyHeaderString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            int colon = raw.IndexOf(':');
            if (colon < 0)
            {
                // 값 없음 = 헤더 제거
                StaticHeaders.RemoveAll(h => h.Name == raw.Trim());
                return;
            }
            string key = raw.Substring(0, colon).Trim();
            string val = raw.Substring(colon + 1).Trim(); // 빈 문자열 = 빈 헤더
            StaticHeaders.RemoveAll(h => h.Name == key);
            StaticHeaders.Add((key, val));
        }

        private class BodySerializationHelper { }
    }
}
