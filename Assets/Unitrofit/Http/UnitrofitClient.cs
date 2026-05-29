using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unitrofit.Attributes.Http;
using Unitrofit.Core;
using Unitrofit.Interceptor;

namespace Unitrofit.Http
{
    /// <summary>
    /// UnityWebRequest + UniTask 기반 HTTP 실행기.
    /// 코루틴 없이 순수 async/await로 동작한다.
    ///
    /// UnitrofitClient.Builder로 생성한다:
    /// <code>
    /// var client = new UnitrofitClient.Builder()
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .AddInterceptor(new AuthInterceptor())
    ///     .Timeout(30)
    ///     .Build();
    /// </code>
    /// </summary>
    public class UnitrofitClient
    {
        private readonly List<IRequestInterceptor> _interceptors;

        public int TimeoutSeconds { get; set; } = 30;

        private UnitrofitClient(List<IRequestInterceptor> interceptors, int timeout)
        {
            _interceptors  = interceptors ?? new List<IRequestInterceptor>();
            TimeoutSeconds = timeout;
        }

        // ── 공개 API ────────────────────────────────────────────────────

        public async UniTask<RawResponse> SendAsync(
            string      url,
            RequestInfo info,
            CancellationToken ct = default)
        {
            // UnityWebRequest는 Unity 메인 스레드에서만 생성 가능
            await UniTask.SwitchToMainThread();

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            using var uwr = BuildRequest(url, info);

            // 정적 헤더 적용 (인터페이스/메서드 레벨)
            foreach (var h in info.StaticHeaders)
            {
                if (h.Value != null)
                    uwr.SetRequestHeader(h.Name, h.Value);
            }

            // 동적 헤더 적용 ([Header] 파라미터)
            foreach (var kv in info.DynamicHeaders)
                uwr.SetRequestHeader(kv.Key, kv.Value);

            uwr.timeout = TimeoutSeconds;

            // RequestContext 조립 — 헤더(정적+동적)와 바디를 인터셉터에 전달
            var requestHeaders = new Dictionary<string, string>();
            foreach (var h in info.StaticHeaders)
                if (h.Value != null) requestHeaders[h.Name] = h.Value;
            foreach (var kv in info.DynamicHeaders)
                requestHeaders[kv.Key] = kv.Value;

            string requestBody = null;
            if (uwr.uploadHandler?.data != null && uwr.uploadHandler.data.Length > 0)
                requestBody = Encoding.UTF8.GetString(uwr.uploadHandler.data);

            var requestContext = new RequestContext(uwr.method, uwr.url, requestHeaders, requestBody);

            // 요청 인터셉터 체인
            foreach (var interceptor in _interceptors)
                interceptor.OnRequest(requestContext);

            try
            {
                await uwr.SendWebRequest().ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnityWebRequestException)
            {
                // 4xx/5xx 도 여기서 잡힘 — ParseResponse 에서 처리
            }

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            var raw = ParseResponse(uwr);

            // 응답 인터셉터 체인
            foreach (var interceptor in _interceptors)
                interceptor.OnResponse(uwr, raw);

            return raw;
        }

        // ── 요청 구성 ────────────────────────────────────────────────────

        private UnityWebRequest BuildRequest(string url, RequestInfo info)
        {
            switch (info.Method)
            {
                case HttpMethod.Get:
                    return UnityWebRequest.Get(url);

                case HttpMethod.Head:
                    var head = new UnityWebRequest(url, "HEAD");
                    head.downloadHandler = new DownloadHandlerBuffer();
                    return head;

                case HttpMethod.Delete:
                    var del = UnityWebRequest.Delete(url);
                    del.downloadHandler = new DownloadHandlerBuffer();
                    return del;

                case HttpMethod.Post:
                case HttpMethod.Put:
                case HttpMethod.Patch:
                    return BuildBodyRequest(url, info);

                default:
                    throw new ArgumentOutOfRangeException(nameof(info.Method));
            }
        }

        private UnityWebRequest BuildBodyRequest(string url, RequestInfo info)
        {
            string verb = info.Method.ToString().ToUpper();
            UnityWebRequest uwr;

            // ── multipart/form-data ─────────────────────────────────────
            if (info.IsMultipart && info.FilePart != null)
            {
                var sections = new List<IMultipartFormSection>();
                sections.Add(new MultipartFormFileSection(
                    info.FilePart.Field,
                    info.FilePart.GetData(),
                    info.FilePart.FileName,
                    info.FilePart.MimeType));

                foreach (var kv in info.FieldParams)
                    sections.Add(new MultipartFormDataSection(kv.Key, kv.Value));

                uwr = UnityWebRequest.Post(url, sections);
                if (verb != "POST") uwr.method = verb;
                return uwr;
            }

            // ── application/x-www-form-urlencoded ──────────────────────
            if (info.FieldParams.Count > 0 && string.IsNullOrEmpty(info.BodyJson))
            {
                uwr = UnityWebRequest.Post(url, info.FieldParams);
                if (verb != "POST") uwr.method = verb;
                return uwr;
            }

            // ── application/json ────────────────────────────────────────
            if (!string.IsNullOrEmpty(info.BodyJson))
            {
                byte[] raw = Encoding.UTF8.GetBytes(info.BodyJson);
                uwr = new UnityWebRequest(url, verb)
                {
                    uploadHandler   = new UploadHandlerRaw(raw),
                    downloadHandler = new DownloadHandlerBuffer(),
                };
                uwr.SetRequestHeader("Content-Type", "application/json");
                return uwr;
            }

            // ── 바디 없는 POST/PUT/PATCH ────────────────────────────────
            return new UnityWebRequest(url, verb)
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };
        }

        // ── 응답 파싱 ────────────────────────────────────────────────────

        private RawResponse ParseResponse(UnityWebRequest uwr)
        {
            var raw = new RawResponse
            {
                StatusCode = uwr.responseCode,
                Body       = uwr.downloadHandler?.text,
                Url        = uwr.url,
            };

#if UNITY_2020_1_OR_NEWER
            raw.IsNetworkError = uwr.result == UnityWebRequest.Result.ConnectionError ||
                                 uwr.result == UnityWebRequest.Result.DataProcessingError;
            raw.IsHttpError    = uwr.result == UnityWebRequest.Result.ProtocolError;
#else
            raw.IsNetworkError = uwr.isNetworkError;
            raw.IsHttpError    = uwr.isHttpError;
#endif
            raw.ErrorMessage = uwr.error;

            var headers = uwr.GetResponseHeaders();
            if (headers != null) raw.ResponseHeaders = headers;

            return raw;
        }

        // ════════════════════════════════════════════════════════════════
        // Builder — OkHttpClient.Builder 대응
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// UnitrofitClient.Builder — HTTP 레벨 설정 (인터셉터, 타임아웃).
        /// <code>
        /// var client = new UnitrofitClient.Builder()
        ///     .AddInterceptor(new LoggingInterceptor())
        ///     .AddInterceptor(new AuthInterceptor())
        ///     .Timeout(30)
        ///     .Build();
        /// </code>
        /// </summary>
        public class Builder
        {
            private readonly List<IRequestInterceptor> _interceptors = new List<IRequestInterceptor>();
            private int _timeout = 30;

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

            public UnitrofitClient Build() => new UnitrofitClient(_interceptors, _timeout);
        }
    }

    // ── 내부 응답 컨테이너 ─────────────────────────────────────────────

    public class RawResponse
    {
        public long   StatusCode  { get; set; }
        public string Body        { get; set; }
        public string Url         { get; set; }
        public bool   IsNetworkError { get; set; }
        public bool   IsHttpError    { get; set; }
        public string ErrorMessage   { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }
}
