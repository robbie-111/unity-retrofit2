using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using RestEase.Adapter;
using RestEase.Core;

namespace RestEase.Demo
{
    /// <summary>
    /// IHttpBinApi 구현 서비스.
    /// RestAdapter 를 상속받아 3개 메서드만 오버라이드하면 완성된다.
    /// 각 API 메서드 본문은 SendRequest&lt;T&gt;() 한 줄이다.
    /// </summary>
    public class HttpBinService : RestAdapter, IHttpBinApi
    {
        // ── 싱글톤 ────────────────────────────────────────────────────
        private static HttpBinService _instance;
        public static HttpBinService Instance => _instance;

        protected override void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        // ── RestAdapter 필수 오버라이드 ───────────────────────────────

        protected override string SetBaseUrl() => "https://httpbin.org";
        protected override Type SetApiInterface() => typeof(IHttpBinApi);
        protected override bool EnableDebug => true;

        /// RequestModifier: 모든 요청 직전에 실행되는 델리게이트.
        /// 여기서는 로깅만 하지만, 토큰 갱신 등 어떤 async 작업이든 가능하다.
        protected override Func<UnityWebRequest, CancellationToken, Task> SetRequestModifier()
            => async (req, ct) =>
            {
                Debug.Log($"[RequestModifier] {req.method} {req.url}");
                await Task.CompletedTask;
            };

        // ── IHttpBinApi 구현 ─────────────────────────────────────────

        public Task<HttpBinGetResponse> GetAsync(string q1, string q2, CancellationToken ct)
            => SendRequest<HttpBinGetResponse>(q1, q2, ct);

        public Task<HttpBinGetResponse> GetDelayAsync(int seconds, CancellationToken ct)
            => SendRequest<HttpBinGetResponse>(seconds, ct);

        public Task<HttpBinGetResponse> GetWithMapAsync(Dictionary<string, string> filters, CancellationToken ct)
            => SendRequest<HttpBinGetResponse>(filters, ct);

        public Task<HttpBinPostResponse> PostBodyAsync(PostBody body, string token, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(body, token, ct);

        public Task<HttpBinPostResponse> PostFormAsync(string username, string password, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(username, password, ct);

        public Task<HttpBinPostResponse> UploadFileAsync(FilePart file, string description, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(file, description, ct);

        public Task<HttpBinPostResponse> PutAsync(PostBody body, CancellationToken ct)
            => SendRequest<HttpBinPostResponse>(body, ct);

        public Task<Response<HttpBinGetResponse>> DeleteAsync(int id, CancellationToken ct)
            => SendRequest<Response<HttpBinGetResponse>>(id, ct);

        public Task<Response<HttpBinGetResponse>> GetWithResponseAsync(string key, CancellationToken ct)
            => SendRequest<Response<HttpBinGetResponse>>(key, ct);
    }
}
