using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using RestEase.Core;

namespace RestEase.Demo
{
    /// <summary>
    /// RestEase for Unity 데모 실행기.
    /// 씬의 GameObject 에 붙이면 Start() 에서 httpbin.org 로 각종 요청을 시연한다.
    /// HttpBinService 가 없으면 자동 생성한다.
    /// </summary>
    public class RestEaseDemoRunner : MonoBehaviour
    {
        private void Awake()
        {
            if (HttpBinService.Instance == null)
            {
                var go = new GameObject("HttpBinService");
                go.AddComponent<HttpBinService>();
            }
        }

        private async void Start()
        {
            var api = HttpBinService.Instance;
            var cts = new CancellationTokenSource();

            // ── 1. GET + Query + CancellationToken ──────────────────
            Debug.Log("=== [1] GET /get?query1=hello&query2=world ===");
            try
            {
                var res = await api.GetAsync("hello", "world", cts.Token);
                Debug.Log($"url: {res.url}, args: {Dict(res.args)}");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 2. GET + Path ────────────────────────────────────────
            Debug.Log("=== [2] GET /delay/1 ===");
            try
            {
                var res = await api.GetDelayAsync(1, cts.Token);
                Debug.Log($"url: {res.url}");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 3. GET + QueryMap ────────────────────────────────────
            Debug.Log("=== [3] GET /get (QueryMap) ===");
            try
            {
                var filters = new Dictionary<string, string> { {"page","1"}, {"limit","20"} };
                var res = await api.GetWithMapAsync(filters, cts.Token);
                Debug.Log($"url: {res.url}");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 4. POST + JSON Body + HeaderParam ───────────────────
            Debug.Log("=== [4] POST /post (JSON Body + Authorization) ===");
            try
            {
                var body = new PostBody { name = "RestEase", value = 100 };
                var res = await api.PostBodyAsync(body, "Bearer demo-token", cts.Token);
                Debug.Log($"url: {res.url}, data: {res.data}");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 5. POST + Form ───────────────────────────────────────
            Debug.Log("=== [5] POST /post (Form) ===");
            try
            {
                var res = await api.PostFormAsync("admin", "secret", cts.Token);
                Debug.Log($"form: {Dict(res.form)}");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 6. Multipart 업로드 ──────────────────────────────────
            Debug.Log("=== [6] POST /post (Multipart) ===");
            try
            {
                var data = Encoding.UTF8.GetBytes("hello restease");
                var part = new FilePart("file", "hello.txt", data, "text/plain");
                var res = await api.UploadFileAsync(part, "test upload", cts.Token);
                Debug.Log($"url: {res.url}");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 7. PUT ───────────────────────────────────────────────
            Debug.Log("=== [7] PUT /put ===");
            try
            {
                var res = await api.PutAsync(new PostBody { name = "put-test", value = 7 }, cts.Token);
                Debug.Log($"url: {res.url}");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 8. DELETE + AllowAnyStatusCode → Response<T> ────────
            Debug.Log("=== [8] DELETE /delete?id=42 (AllowAnyStatusCode) ===");
            var delRes = await api.DeleteAsync(42, cts.Token);  // 예외 없음
            Debug.Log($"status: {delRes.StatusCode}, isSuccess: {delRes.IsSuccess}, url: {delRes.Data?.url}");

            // ── 9. Response<T> 반환 — 상태코드 직접 확인 ─────────────
            Debug.Log("=== [9] GET /get → Response<T> ===");
            var wrappedRes = await api.GetWithResponseAsync("wrapped", cts.Token);
            Debug.Log($"StatusCode: {wrappedRes.StatusCode}, IsSuccess: {wrappedRes.IsSuccess}");
            if (wrappedRes.IsSuccess)
                Debug.Log($"data url: {wrappedRes.Data?.url}");

            // ── 10. CancellationToken 취소 시연 ──────────────────────
            Debug.Log("=== [10] GET /delay/3 — 1초 후 취소 ===");
            var cancelCts = new CancellationTokenSource();
            cancelCts.CancelAfter(1000); // 1초 후 취소
            try
            {
                var res = await api.GetDelayAsync(3, cancelCts.Token);
                Debug.Log($"url: {res.url}");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("요청이 취소되었습니다.");
            }
            catch (ApiException e) { Debug.LogError($"실패 [{e.StatusCode}]: {e.Message}"); }

            Debug.Log("=== 모든 데모 완료 ===");
        }

        private static string Dict(Dictionary<string, string> d)
        {
            if (d == null) return "null";
            var sb = new StringBuilder("{");
            foreach (var kv in d) sb.Append($" {kv.Key}={kv.Value},");
            return sb.Append(" }").ToString();
        }
    }
}
