using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Refit.Adapter;
using Refit.Converter;
using Refit.Core;
using Refit.Settings;

namespace Refit.Demo
{
    /// <summary>
    /// IGitHubApi 구현 서비스.
    /// RefitSettings 를 통해 AuthorizationHeaderValueGetter, RequestModifier 를 구성한다.
    /// </summary>
    public class GitHubService : RestAdapter, IGitHubApi
    {
        // ── 싱글톤 ────────────────────────────────────────────────────
        private static GitHubService _instance;
        public static GitHubService Instance => _instance;

        // 데모용 토큰 (실제 프로젝트에서는 안전한 저장소 사용)
        private string _accessToken = string.Empty;
        public void SetAccessToken(string token) => _accessToken = token;

        protected override void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        // ── RestAdapter 필수 오버라이드 ───────────────────────────────

        protected override string SetBaseUrl() => "https://api.github.com";
        protected override Type SetApiInterface() => typeof(IGitHubApi);

        protected override RefitSettings SetRefitSettings() => new RefitSettings
        {
            ContentSerializer = new JsonContentSerializer(),
            TimeoutSeconds    = 30,
            EnableDebug       = true,

            // AuthorizationHeaderValueGetter:
            // 인터페이스에 [Headers("Authorization: Bearer")] 플레이스홀더가 있으면
            // 매 요청 직전에 이 델리게이트가 호출되어 토큰을 자동 주입한다.
            AuthorizationHeaderValueGetter = async (req, ct) =>
            {
                // 실제 프로젝트에서는 TokenService.GetTokenAsync() 등으로 교체
                await Task.CompletedTask;
                return _accessToken;
            },

            // RequestModifier: 모든 요청에 공통 처리 (로깅, 서명 등)
            RequestModifier = async (req, ct) =>
            {
                Debug.Log($"[Refit RequestModifier] {req.method} {req.url}");
                // [Property] 로 전달된 TraceId 는 CustomProperties 에서 읽어야 하지만
                // UnityWebRequest 에는 Properties 딕셔너리가 없으므로
                // 커스텀 헤더로 포워딩하는 패턴을 사용한다.
                await Task.CompletedTask;
            },
        };

        // ── IGitHubApi 구현 ─────────────────────────────────────────

        public Task<User> GetUserAsync(string userId, CancellationToken ct)
            => SendRequest<User>(userId, ct);

        public Task<SearchResult> SearchReposAsync(string q, string sort, CancellationToken ct)
            => SendRequest<SearchResult>(q, sort, ct);

        public Task<SearchResult> SearchReposWithMapAsync(string q, int perPage, CancellationToken ct)
            => SendRequest<SearchResult>(q, perPage, ct);

        public Task<List<Issue>> GetIssuesAsync(string owner, string repo, string[] labels, CancellationToken ct)
            => SendRequest<List<Issue>>(owner, repo, labels, ct);

        public Task<ApiResponse<User>> GetUserSafeAsync(string user, CancellationToken ct)
            => SendRequest<ApiResponse<User>>(user, ct);

        public Task<IApiResponse<User>> GetUserWithMetaAsync(string user, CancellationToken ct)
            => SendRequest<IApiResponse<User>>(user, ct);

        public Task<Issue> CreateIssueAsync(string owner, string repo, Issue issue, CancellationToken ct)
            => SendRequest<Issue>(owner, repo, issue, ct);

        public Task TrackEventAsync(AnalyticsEvent ev, CancellationToken ct)
            => SendRequest(ev, ct);

        public Task UploadAvatarAsync(string user, StreamPart photo, CancellationToken ct)
            => SendRequest(user, photo, ct);

        public Task UploadDocAsync(ByteArrayPart document, string note, CancellationToken ct)
            => SendRequest(document, note, ct);

        public Task DeleteRepoAsync(string owner, string repo, string reason, CancellationToken ct)
            => SendRequest(owner, repo, reason, ct);

        public Task<User> GetAuthenticatedUserAsync(string token, CancellationToken ct)
            => SendRequest<User>(token, ct);

        public Task<User> GetUserWithHeadersAsync(string user, IDictionary<string, string> headers, CancellationToken ct)
            => SendRequest<User>(user, headers, ct);

        public Task<Repo> GetRepoAsync(string owner, string repo, string traceId, CancellationToken ct)
            => SendRequest<Repo>(owner, repo, traceId, ct);

        public Task<SearchResult> SearchCodeUnescapedAsync(string q, CancellationToken ct)
            => SendRequest<SearchResult>(q, ct);

        // ── httpbin.org 구현 ─────────────────────────────────────────

        public Task<HttpBinResponse> GetHttpBinAsync(string q1, string q2, CancellationToken ct)
            => SendRequest<HttpBinResponse>(q1, q2, ct);

        public Task<HttpBinResponse> PostHttpBinJsonAsync(object body, CancellationToken ct)
            => SendRequest<HttpBinResponse>(body, ct);

        public Task<HttpBinResponse> PostHttpBinFormAsync(AnalyticsEvent ev, CancellationToken ct)
            => SendRequest<HttpBinResponse>(ev, ct);

        public Task<HttpBinResponse> PostHttpBinMultipartAsync(ByteArrayPart file, string note, CancellationToken ct)
            => SendRequest<HttpBinResponse>(file, note, ct);
    }
}
