using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Refit.Core;
using Refit.Attributes.Params;

namespace Refit.Demo
{
    /// <summary>
    /// Refit for Unity 데모 실행기.
    /// Refit README 의 주요 기능을 모두 시연한다.
    /// </summary>
    public class RefitDemoRunner : MonoBehaviour
    {
        private void Awake()
        {
            if (GitHubService.Instance == null)
                new GameObject("GitHubService").AddComponent<GitHubService>();
        }

        private async void Start()
        {
            var api = GitHubService.Instance;
            var cts = new CancellationTokenSource();

            Debug.Log("=== Refit for Unity 데모 시작 ===\n");

            // ════════════════════════════════════════════════════════
            // httpbin.org — 항상 200 응답이 보장되는 성공 예시들
            // ════════════════════════════════════════════════════════

            // ── [0-A] GET + Query → 200 OK ───────────────────────────
            Debug.Log("[0-A] GET https://httpbin.org/get — Query 파라미터 200 성공");
            try
            {
                var res = await api.GetHttpBinAsync("hello", "refit", cts.Token);
                Debug.Log($"  [200 OK] url: {res.url}");
                //Debug.Log($"  args: query1={res.args?["query1"]}, query2={res.args?["query2"]}");
                Debug.Log($"  User-Agent: {res.headers?["User-Agent"]}");
            }
            catch (ApiException e)       { Debug.LogError($"  ApiException [{e.StatusCode}]: {e.Message}"); }
            catch (ApiRequestException e){ Debug.LogError($"  ApiRequestException: {e.Message}"); }

            // ── [0-B] POST + JSON Body → 200 OK ─────────────────────
            Debug.Log("[0-B] POST https://httpbin.org/post — JSON Body 200 성공");
            try
            {
                var payload = new { name = "Refit", version = 1, active = true };
                var res = await api.PostHttpBinJsonAsync(payload, cts.Token);
                Debug.Log($"  [200 OK] url: {res.url}");
                Debug.Log($"  data(echo): {res.data}");
            }
            catch (ApiException e)       { Debug.LogError($"  ApiException [{e.StatusCode}]: {e.Message}"); }
            catch (ApiRequestException e){ Debug.LogError($"  ApiRequestException: {e.Message}"); }

            // ── [0-C] POST + UrlEncoded POCO + AliasAs → 200 OK ─────
            Debug.Log("[0-C] POST https://httpbin.org/post — UrlEncoded + AliasAs 200 성공");
            try
            {
                var ev = new AnalyticsEvent
                {
                    TrackingId  = "UA-1234-5",
                    HitType     = "event",
                    EventAction = "button_click",
                };
                var res = await api.PostHttpBinFormAsync(ev, cts.Token);
                Debug.Log($"  [200 OK] url: {res.url}");
                // AliasAs 적용 확인: tid, t, ea 키로 전송됐는지 확인
                Debug.Log($"  form: v={F(res.form,"v")}, tid={F(res.form,"tid")}, t={F(res.form,"t")}, ea={F(res.form,"ea")}");
            }
            catch (ApiException e)       { Debug.LogError($"  ApiException [{e.StatusCode}]: {e.Message}"); }
            catch (ApiRequestException e){ Debug.LogError($"  ApiRequestException: {e.Message}"); }

            // ── [0-D] POST + Multipart (ByteArrayPart) → 200 OK ─────
            Debug.Log("[0-D] POST https://httpbin.org/post — Multipart ByteArrayPart 200 성공");
            try
            {
                var fileBytes = Encoding.UTF8.GetBytes("Hello from Refit for Unity!");
                var part = new ByteArrayPart(fileBytes, "hello.txt", "text/plain");
                var res = await api.PostHttpBinMultipartAsync(part, "refit-multipart-test", cts.Token);
                Debug.Log($"  [200 OK] url: {res.url}");
            }
            catch (ApiException e)       { Debug.LogError($"  ApiException [{e.StatusCode}]: {e.Message}"); }
            catch (ApiRequestException e){ Debug.LogError($"  ApiRequestException: {e.Message}"); }

            // ════════════════════════════════════════════════════════
            // GitHub API 시나리오
            // ════════════════════════════════════════════════════════

            // ── 1. GET + Path + AliasAs ──────────────────────────────
            Debug.Log("[1] GET /users/{user} — Path + AliasAs");
            try
            {
                var user = await api.GetUserAsync("octocat", cts.Token);
                Debug.Log($"  login: {user.login}, repos: {user.public_repos}");
            }
            catch (ApiException e)      { Debug.LogError($"  ApiException [{e.StatusCode}]: {e.Message}"); }
            catch (ApiRequestException e){ Debug.LogError($"  ApiRequestException: {e.Message}"); }

            // ── 2. GET + 어트리뷰트 없는 Query 파라미터 ──────────────
            Debug.Log("[2] GET /search/repositories — Query (어트리뷰트 없음)");
            try
            {
                var result = await api.SearchReposAsync("unity", "stars", cts.Token);
                Debug.Log($"  total: {result.total_count}, first: {result.items?[0]?.full_name}");
            }
            catch (ApiException e) { Debug.LogError($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 3. GET + QueryUriFormat ───────────────────────────────
            Debug.Log("[3] GET /search/repositories — AliasAs(per_page)");
            try
            {
                var result = await api.SearchReposWithMapAsync("refit", 5, cts.Token);
                Debug.Log($"  total: {result.total_count}");
            }
            catch (ApiException e) { Debug.LogError($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 4. GET + CollectionFormat.Csv 배열 쿼리 ──────────────
            Debug.Log("[4] GET /repos/{owner}/{repo}/issues — CollectionFormat.Csv");
            try
            {
                var issues = await api.GetIssuesAsync("octocat", "Hello-World",
                    new[] { "bug", "help-wanted" }, cts.Token);
                Debug.Log($"  issues count: {issues?.Count}");
            }
            catch (ApiException e) { Debug.LogError($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 5. ApiResponse<T> — HasRequestError / HasResponseError ─
            Debug.Log("[5] GET /users/nonexistent-user-xyz — ApiResponse<T>");
            var safe = await api.GetUserSafeAsync("nonexistent-user-xyz-12345", cts.Token);
            Debug.Log($"  IsSuccessful: {safe.IsSuccessful}, StatusCode: {safe.StatusCode}");
            if (safe.HasResponseError(out var respErr))
                Debug.Log($"  HasResponseError: [{respErr.StatusCode}] {respErr.Message}");
            else if (safe.HasRequestError(out var reqErr))
                Debug.Log($"  HasRequestError: {reqErr.Message}");

            // ── 6. IApiResponse<T> ────────────────────────────────────
            Debug.Log("[6] GET /users/octocat — IApiResponse<T>");
            var meta = await api.GetUserWithMetaAsync("octocat", cts.Token);
            Debug.Log($"  IsSuccessful: {meta.IsSuccessful}, IsReceived: {meta.IsReceived}");
            if (meta.IsSuccessful) Debug.Log($"  login: {meta.Content?.login}");

            // ── 7. POST + JSON Body ───────────────────────────────────
            Debug.Log("[7] POST /repos — JSON Body (GitHub API 제약으로 실제 생성 불가)");
            try
            {
                var issue = new Issue { title = "Refit Unity Test", body = "Test issue", labels = new List<string> { "bug" } };
                var created = await api.CreateIssueAsync("octocat", "Hello-World", issue, cts.Token);
                Debug.Log($"  created title: {created?.title}");
            }
            catch (ApiException e) { Debug.Log($"  예상된 에러 [{e.StatusCode}]: {e.Message}"); }

            // ── 8. POST + UrlEncoded POCO + AliasAs ──────────────────
            Debug.Log("[8] POST /analytics/collect — UrlEncoded + AliasAs");
            try
            {
                var ev = new AnalyticsEvent { TrackingId = "UA-1234-5", HitType = "event", EventAction = "click" };
                await api.TrackEventAsync(ev, cts.Token);
            }
            catch (ApiException e) { Debug.Log($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 9. [Authorize] — Bearer 단축 어트리뷰트 ──────────────
            Debug.Log("[9] GET /user — [Authorize] Bearer 단축");
            try
            {
                var me = await api.GetAuthenticatedUserAsync("MY_GITHUB_TOKEN", cts.Token);
                Debug.Log($"  login: {me?.login}");
            }
            catch (ApiException e) { Debug.Log($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 10. [HeaderCollection] — 다중 동적 헤더 ───────────────
            Debug.Log("[10] GET /users/{user} — HeaderCollection");
            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "X-Custom-A", "valueA" },
                    { "X-Custom-B", "valueB" },
                };
                var user = await api.GetUserWithHeadersAsync("octocat", headers, cts.Token);
                Debug.Log($"  login: {user?.login}");
            }
            catch (ApiException e) { Debug.LogError($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 11. [Property] — RequestModifier 상태 전달 ────────────
            Debug.Log("[11] GET /repos/{owner}/{repo} — Property(TraceId)");
            try
            {
                var repo = await api.GetRepoAsync("octocat", "Hello-World", "trace-abc-123", cts.Token);
                Debug.Log($"  repo: {repo?.full_name}, stars: {repo?.stargazers_count}");
            }
            catch (ApiException e) { Debug.LogError($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 12. [QueryUriFormat(Unescaped)] ──────────────────────
            Debug.Log("[12] GET /search/code — QueryUriFormat Unescaped");
            try
            {
                var result = await api.SearchCodeUnescapedAsync("Select+Id+From+Users", cts.Token);
                Debug.Log($"  total: {result?.total_count}");
            }
            catch (ApiException e) { Debug.Log($"  [{e.StatusCode}]: {e.Message}"); }

            // ── 13. CancellationToken 취소 시연 ──────────────────────
            Debug.Log("[13] GET /users/octocat — 즉시 취소");
            var cancelCts = new CancellationTokenSource();
            cancelCts.Cancel();
            try
            {
                var user = await api.GetUserAsync("octocat", cancelCts.Token);
            }
            catch (System.OperationCanceledException) { Debug.Log("  요청 취소됨."); }

            Debug.Log("\n=== Refit for Unity 데모 완료 ===");
        }

        /// <summary>딕셔너리에서 키 조회. 없으면 "(none)" 반환. KeyNotFoundException 방지.</summary>
        private static string F(Dictionary<string, string> d, string key)
            => d != null && d.TryGetValue(key, out var v) ? v : "(none)";
    }
}
