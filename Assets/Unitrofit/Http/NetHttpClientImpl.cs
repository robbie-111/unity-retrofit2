using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unitrofit.Core;
using NetHttpMethod       = System.Net.Http.HttpMethod;
using UnitrofitHttpMethod = Unitrofit.Attributes.Http.HttpMethod;

namespace Unitrofit.Http
{
    /// <summary>
    /// System.Net.HttpClient 기반 HTTP 구현체.
    /// 백그라운드 스레드에서 실행 가능하며 Unity 메인 스레드 구속이 없다.
    /// <para>
    /// 인터셉터 체인·타임아웃은 <see cref="UnitrofitClient"/>가 처리하며,
    /// 이 클래스는 순수 HTTP I/O만 담당한다.
    /// </para>
    /// <para>주의: WebGL 플랫폼에서는 System.Net이 지원되지 않으므로
    /// WebGL 빌드에서는 <see cref="UnityWebRequestImpl"/>를 사용해야 한다.</para>
    /// <code>
    /// var client = new UnitrofitClient.Builder()
    ///     .HttpClient(new NetHttpClientImpl())
    ///     .AddInterceptor(new LoggingInterceptor())
    ///     .Timeout(30)
    ///     .Build();
    /// </code>
    /// </summary>
    public class NetHttpClientImpl : IHttpClient
    {
        private readonly HttpClient _http;

        /// <summary>내부에서 HttpClient를 직접 생성한다.</summary>
        public NetHttpClientImpl() : this(new HttpClient()) { }

        /// <summary>외부에서 생성한 HttpClient를 주입한다 (테스트·커스텀 핸들러 용도).</summary>
        public NetHttpClientImpl(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // ── 공개 API ────────────────────────────────────────────────────

        public async UniTask<RawResponse> SendAsync(string url, RequestInfo info, int timeoutSeconds, CancellationToken ct)
        {
            _http.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            using var req = BuildHttpRequest(url, info);

            // ── 전송 (Task 기반) — async UniTask 안에서 자연스럽게 await ──
            HttpResponseMessage resp;
            try
            {
                // Task<HttpResponseMessage> → async UniTask 안에서 별도 변환 없이 await
                resp = await _http.SendAsync(req, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                // 네트워크 오류 — UnityWebRequestImpl과 동일하게 IsNetworkError=true 반환
                return new RawResponse
                {
                    Url            = url,
                    IsNetworkError = true,
                    ErrorMessage   = ex.Message,
                };
            }

            return await ParseResponseAsync(resp, url);
        }

        // ── 요청 구성 ────────────────────────────────────────────────────

        private static HttpRequestMessage BuildHttpRequest(string url, RequestInfo info)
        {
            var req = new HttpRequestMessage(ToNetMethod(info.Method), url);

            // 헤더 적용 (정적 + 동적)
            foreach (var h in info.StaticHeaders)
                if (h.Value != null) req.Headers.TryAddWithoutValidation(h.Name, h.Value);
            foreach (var kv in info.DynamicHeaders)
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

            // ── multipart/form-data ─────────────────────────────────────
            if (info.IsMultipart && info.FilePart != null)
            {
                var form        = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(info.FilePart.GetData());
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(info.FilePart.MimeType);
                form.Add(fileContent, info.FilePart.Field, info.FilePart.FileName);

                foreach (var kv in info.FieldParams)
                    form.Add(new StringContent(kv.Value), kv.Key);

                req.Content = form;
                return req;
            }

            // ── application/x-www-form-urlencoded ──────────────────────
            if (info.FieldParams.Count > 0 && string.IsNullOrEmpty(info.BodyJson))
            {
                req.Content = new FormUrlEncodedContent(info.FieldParams);
                return req;
            }

            // ── application/json ────────────────────────────────────────
            if (!string.IsNullOrEmpty(info.BodyJson))
            {
                req.Content = new StringContent(info.BodyJson, Encoding.UTF8, "application/json");
                return req;
            }

            // ── 바디 없는 POST/PUT/PATCH: Content = null 그대로 ──────────
            return req;
        }

        // ── 응답 파싱 ────────────────────────────────────────────────────

        private static async UniTask<RawResponse> ParseResponseAsync(HttpResponseMessage resp, string url)
        {
            // Task<string> → async UniTask 안에서 별도 변환 없이 await
            var body = await resp.Content.ReadAsStringAsync();

            var raw = new RawResponse
            {
                StatusCode     = (long)resp.StatusCode,
                Body           = body,
                Url            = url,
                IsNetworkError = false,
                IsHttpError    = !resp.IsSuccessStatusCode,
                ErrorMessage   = resp.IsSuccessStatusCode ? null : resp.ReasonPhrase,
            };

            foreach (var h in resp.Headers)
                raw.ResponseHeaders[h.Key] = string.Join(", ", h.Value);
            foreach (var h in resp.Content.Headers)
                raw.ResponseHeaders[h.Key] = string.Join(", ", h.Value);

            return raw;
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────

        private static NetHttpMethod ToNetMethod(UnitrofitHttpMethod m) => m switch
        {
            UnitrofitHttpMethod.Get    => NetHttpMethod.Get,
            UnitrofitHttpMethod.Post   => NetHttpMethod.Post,
            UnitrofitHttpMethod.Put    => NetHttpMethod.Put,
            UnitrofitHttpMethod.Delete => NetHttpMethod.Delete,
            UnitrofitHttpMethod.Head   => NetHttpMethod.Head,
            UnitrofitHttpMethod.Patch  => new NetHttpMethod("PATCH"),
            _                          => throw new ArgumentOutOfRangeException(nameof(m))
        };
    }
}
