using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Refit.Attributes.Http;
using Refit.Attributes.Params;
using Refit.Core;

namespace Refit.Demo
{
    // ── 응답 모델 ─────────────────────────────────────────────────────

    [System.Serializable]
    public class User
    {
        public string login;
        public string name;
        public string blog;
        public int public_repos;
    }

    [System.Serializable]
    public class Repo
    {
        public string name;
        public string full_name;
        public bool @private;
        public string description;
        public int stargazers_count;
    }

    [System.Serializable]
    public class SearchResult
    {
        public int total_count;
        public List<Repo> items;
    }

    [System.Serializable]
    public class Issue
    {
        public string title;
        public string body;
        public List<string> labels;
    }

    // ── 폼 인코딩 POCO (AliasAs 시연) ────────────────────────────────

    public class AnalyticsEvent
    {
        [AliasAs("v")]
        public int Version { get; set; } = 1;

        [AliasAs("tid")]
        public string TrackingId { get; set; }

        [AliasAs("t")]
        public string HitType { get; set; }

        [AliasAs("ea")]
        public string EventAction { get; set; }

        // private getter → 폼 직렬화 제외됨
        public string Ignored { private get; set; } = "ignored";
    }

    // ── httpbin.org 응답 모델 ────────────────────────────────────────────

    [System.Serializable]
    public class HttpBinResponse
    {
        public string url;
        public Dictionary<string, string> args;
        public Dictionary<string, string> headers;
        public string data;
        public Dictionary<string, string> form;
    }

    // ── REST API 인터페이스 ─────────────────────────────────────────────

    /// <summary>
    /// Refit for Unity 데모 인터페이스.
    /// Refit README 의 예시를 Unity 환경에 맞게 재현한다.
    /// </summary>
    [Headers("User-Agent: Refit-Unity-Demo", "Accept: application/vnd.github.v3+json")]
    [Headers("Authorization: Bearer")]   // AuthorizationHeaderValueGetter 연동 플레이스홀더
    public interface IGitHubApi
    {
        // ── GET + Path ────────────────────────────────────────────────

        /// Path 파라미터. AliasAs 로 파라미터 이름 재정의 시연.
        [Get("/users/{user}")]
        Task<User> GetUserAsync([AliasAs("user")] string userId, CancellationToken ct = default);

        // ── GET + Query ───────────────────────────────────────────────

        /// Query 파라미터 (어트리뷰트 없음 → Refit 기본: 파라미터 이름이 키)
        [Get("/search/repositories")]
        Task<SearchResult> SearchReposAsync(string q, string sort = "stars", CancellationToken ct = default);

        // ── GET + QueryMap ────────────────────────────────────────────

        [Get("/search/repositories")]
        [QueryUriFormat(UriFormat.UriEscaped)]
        Task<SearchResult> SearchReposWithMapAsync(
            [Query] string q,
            [AliasAs("per_page")] int perPage,
            CancellationToken ct = default);

        // ── GET + CollectionFormat 배열 쿼리 ─────────────────────────

        /// 배열 파라미터 Csv 포맷: ?labels=bug%2Chelp-wanted
        [Get("/repos/{owner}/{repo}/issues")]
        Task<List<Issue>> GetIssuesAsync(
            [Path] string owner,
            [Path] string repo,
            [Query(CollectionFormat.Csv)] string[] labels,
            CancellationToken ct = default);

        // ── GET + AllowAnyStatusCode → ApiResponse<T> ─────────────────

        [Get("/users/{user}")]
        [AllowAnyStatusCode]
        Task<ApiResponse<User>> GetUserSafeAsync([Path] string user, CancellationToken ct = default);

        // ── GET + IApiResponse<T> ─────────────────────────────────────

        [Get("/users/{user}")]
        Task<IApiResponse<User>> GetUserWithMetaAsync([Path] string user, CancellationToken ct = default);

        // ── POST + JSON Body ──────────────────────────────────────────

        [Post("/repos/{owner}/{repo}/issues")]
        [Headers("X-GitHub-OTP: 123456")]   // 메서드 레벨 정적 헤더
        Task<Issue> CreateIssueAsync(
            [Path] string owner,
            [Path] string repo,
            [Body] Issue issue,
            CancellationToken ct = default);

        // ── POST + UrlEncoded Body (POCO + AliasAs) ───────────────────

        [Post("/analytics/collect")]
        Task TrackEventAsync(
            [Body(BodySerializationMethod.UrlEncoded)] AnalyticsEvent ev,
            CancellationToken ct = default);

        // ── POST + Multipart (StreamPart) ─────────────────────────────

        [Multipart]
        [Post("/users/{user}/avatar")]
        Task UploadAvatarAsync(
            [Path] string user,
            [AliasAs("avatar")] [Part] StreamPart photo,
            CancellationToken ct = default);

        // ── POST + Multipart (ByteArrayPart) ──────────────────────────

        [Multipart]
        [Post("/upload/doc")]
        Task UploadDocAsync(
            [AliasAs("doc")] [Part] ByteArrayPart document,
            [AliasAs("note")] string note,
            CancellationToken ct = default);

        // ── DELETE + dynamic Header ───────────────────────────────────

        [Delete("/repos/{owner}/{repo}")]
        Task DeleteRepoAsync(
            [Path] string owner,
            [Path] string repo,
            [Header("X-Reason")] string reason,
            CancellationToken ct = default);

        // ── GET + Authorize (Bearer 단축) ─────────────────────────────

        [Get("/user")]
        Task<User> GetAuthenticatedUserAsync(
            [Authorize("Bearer")] string token,
            CancellationToken ct = default);

        // ── GET + HeaderCollection (다중 동적 헤더) ───────────────────

        [Get("/users/{user}")]
        Task<User> GetUserWithHeadersAsync(
            [Path] string user,
            [HeaderCollection] IDictionary<string, string> headers,
            CancellationToken ct = default);

        // ── GET + Property (RequestModifier 상태 전달) ────────────────

        [Get("/repos/{owner}/{repo}")]
        Task<Repo> GetRepoAsync(
            [Path] string owner,
            [Path] string repo,
            [Property("TraceId")] string traceId,
            CancellationToken ct = default);

        // ── QueryUriFormat + Unescaped ────────────────────────────────

        [Get("/search/code")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<SearchResult> SearchCodeUnescapedAsync(string q, CancellationToken ct = default);

        // ── httpbin.org — 항상 200 반환이 보장되는 엔드포인트 ────────────
        // 절대 URL 을 사용하므로 baseUrl(api.github.com) 은 무시된다.

        [Get("https://httpbin.org/get")]
        Task<HttpBinResponse> GetHttpBinAsync(
            [Query("query1")] string q1,
            [Query("query2")] string q2,
            CancellationToken ct = default);

        [Post("https://httpbin.org/post")]
        Task<HttpBinResponse> PostHttpBinJsonAsync(
            [Body] object body,
            CancellationToken ct = default);

        [Post("https://httpbin.org/post")]
        Task<HttpBinResponse> PostHttpBinFormAsync(
            [Body(BodySerializationMethod.UrlEncoded)] AnalyticsEvent ev,
            CancellationToken ct = default);

        [Multipart]
        [Post("https://httpbin.org/post")]
        Task<HttpBinResponse> PostHttpBinMultipartAsync(
            [AliasAs("file")] [Part] ByteArrayPart file,
            [AliasAs("note")] string note,
            CancellationToken ct = default);
    }
}
