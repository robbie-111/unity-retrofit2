using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unitrofit.Core;
using Unitrofit.Interceptor;

namespace Unitrofit.Http
{
    /// <summary>
    /// Unitrofit HTTP 실행기.
    /// 인터셉터 체인·타임아웃을 관리하며, 실제 HTTP I/O는 <see cref="IHttpClient"/>에 위임한다.
    ///
    /// UnitrofitClient.Builder로 생성한다:
    /// <code>
    /// // UnityWebRequest (기본값)
    /// var client = new UnitrofitClient.Builder()
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .Timeout(30)
    ///     .Build();
    ///
    /// // System.Net.HttpClient 선택
    /// var client = new UnitrofitClient.Builder()
    ///     .HttpClient(new NetHttpClientImpl())
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .Timeout(30)
    ///     .Build();
    /// </code>
    /// </summary>
    public class UnitrofitClient
    {
        private readonly IHttpClient               _httpClient;
        private readonly List<IRequestInterceptor> _interceptors;
        private readonly int                       _timeoutSeconds;

        private UnitrofitClient(IHttpClient httpClient, List<IRequestInterceptor> interceptors, int timeoutSeconds)
        {
            _httpClient     = httpClient;
            _interceptors   = interceptors ?? new List<IRequestInterceptor>();
            _timeoutSeconds = timeoutSeconds;
        }

        // ── 공개 API ────────────────────────────────────────────────────

        public async UniTask<RawResponse> SendAsync(string url, RequestInfo info, CancellationToken ct = default)
        {
            // ── RequestContext 조립 → OnRequest 체인 ────────────────────
            var requestHeaders = BuildRequestHeaders(info);
            string requestBody = BuildRequestBodyPreview(info);
            var ctx = new RequestContext(info.Method.ToString().ToUpper(), url, requestHeaders, requestBody);
            foreach (var interceptor in _interceptors)
                interceptor.OnRequest(ctx);

            // ── 실제 HTTP I/O — IHttpClient에 위임 ──────────────────────
            var raw = await _httpClient.SendAsync(url, info, _timeoutSeconds, ct);

            // ── OnResponse 체인 ──────────────────────────────────────────
            foreach (var interceptor in _interceptors)
                interceptor.OnResponse(raw);

            return raw;
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────

        private static Dictionary<string, string> BuildRequestHeaders(RequestInfo info)
        {
            var d = new Dictionary<string, string>();
            foreach (var h in info.StaticHeaders)
                if (h.Value != null) d[h.Name] = h.Value;
            foreach (var kv in info.DynamicHeaders)
                d[kv.Key] = kv.Value;
            return d;
        }

        /// <summary>
        /// 인터셉터(LoggingInterceptor 등)에 전달할 요청 바디 미리보기.
        /// JSON 바디가 있으면 반환하고, Form 파라미터가 있으면 URL-encoded 문자열로 변환.
        /// 바디가 없으면 null.
        /// </summary>
        private static string BuildRequestBodyPreview(RequestInfo info)
        {
            if (!string.IsNullOrEmpty(info.BodyJson))
                return info.BodyJson;

            if (info.FieldParams.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var kv in info.FieldParams)
                {
                    if (sb.Length > 0) sb.Append('&');
                    sb.Append(Uri.EscapeDataString(kv.Key))
                      .Append('=')
                      .Append(Uri.EscapeDataString(kv.Value ?? ""));
                }
                return sb.ToString();
            }

            return null;
        }

        // ════════════════════════════════════════════════════════════════
        // Builder
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// UnitrofitClient.Builder — HTTP 레벨 설정 (구현체, 인터셉터, 타임아웃).
        /// </summary>
        public class Builder
        {
            private readonly List<IRequestInterceptor> _interceptors = new List<IRequestInterceptor>();
            private IHttpClient _httpClient;
            private int         _timeout = 30;

            /// <summary>
            /// HTTP 구현체를 선택한다.
            /// 지정하지 않으면 <see cref="UnityWebRequestImpl"/>이 기본으로 사용된다.
            /// </summary>
            public Builder HttpClient(IHttpClient httpClient)
            {
                _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
                return this;
            }

            /// <summary>인터셉터를 추가한다. 등록 순서대로 OnRequest/OnResponse가 호출된다.</summary>
            public Builder AddInterceptor(IRequestInterceptor interceptor)
            {
                if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));
                _interceptors.Add(interceptor);
                return this;
            }

            /// <summary>요청 타임아웃(초). 기본값 30.</summary>
            public Builder Timeout(int seconds)
            {
                _timeout = seconds;
                return this;
            }

            public UnitrofitClient Build()
            {
                var http = _httpClient ?? new UnityWebRequestImpl();
                return new UnitrofitClient(http, _interceptors, _timeout);
            }
        }
    }

    // ── 응답 컨테이너 ──────────────────────────────────────────────────

    public class RawResponse
    {
        public long   StatusCode     { get; set; }
        public string Body           { get; set; }
        public string Url            { get; set; }
        public bool   IsNetworkError { get; set; }
        public bool   IsHttpError    { get; set; }
        public string ErrorMessage   { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }
}
