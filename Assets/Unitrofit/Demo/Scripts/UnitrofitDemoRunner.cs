using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Unitrofit.Adapter;
using Unitrofit.Core;
using Unitrofit.Http;
using Unitrofit.Interceptor;

namespace Unitrofit.Demo
{
    /// <summary>
    /// Unitrofit 데모 실행기.
    /// 씬의 GameObject에 붙이면 Start()에서 httpbin.org로 각종 요청을 시연한다.
    /// </summary>
    public class UnitrofitDemoRunner : MonoBehaviour
    {
        private IHttpBinApi _api;

        private void Awake()
        {
            // ── HTTP 클라이언트 설정 (인터셉터, 타임아웃) ─────────────────
            // 기본값: UnityWebRequestImpl (WebGL 포함 모든 플랫폼 지원)
            // var client = new UnitrofitClient.Builder()
            //     .HttpClient(new NetHttpClientImpl())
            //     .AddInterceptor(new LoggingInterceptor())
            //     .Build();

            // System.Net.HttpClient 사용 시 (WebGL 미지원):
            var client = new UnitrofitClient.Builder()
                .HttpClient(new NetHttpClientImpl())
                .AddInterceptor(new LoggingInterceptor())
                .Build();

            // ── API 레벨 설정 (BaseUrl, Client 주입) ──────────────────────
            _api = new UnitrofitAdapter.Builder()
                .BaseUrl("https://httpbin.org")
                .Client(client)
                .Build()
                .Create<HttpBinService>();
        }

        private async void Start()
        {
            var api = _api;
            var cts = new CancellationTokenSource();

            Debug.Log("=== Unitrofit 데모 시작 ===\n");

            // ── 1. GET + Query + CancellationToken ──────────────────
            Debug.Log("=== [1] GET /get?query1=hello&query2=world ===");
            try
            {
                var res = await api.GetAsync("hello", "world", cts.Token);
                Debug.Log($"  url: {res.url}");
                Debug.Log($"  args: {Dict(res.args)}");
            }
            catch (UnitrofitException e)
            { Debug.LogError($"  실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 2. GET + Path ────────────────────────────────────────
            Debug.Log("=== [2] GET /delay/1 ===");
            try
            {
                var res = await api.GetDelayAsync(1, cts.Token);
                Debug.Log($"  url: {res.url}");
            }
            catch (UnitrofitException e)
            { Debug.LogError($"  실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 3. GET + QueryMap ────────────────────────────────────
            Debug.Log("=== [3] GET /get (QueryMap) ===");
            try
            {
                var filters = new Dictionary<string, string>
                    { { "page", "1" }, { "limit", "20" }, { "sort", "desc" } };
                var res = await api.GetWithMapAsync(filters, cts.Token);
                Debug.Log($"  url: {res.url}");
                Debug.Log($"  args: {Dict(res.args)}");
            }
            catch (UnitrofitException e)
            { Debug.LogError($"  실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 4. POST + JSON Body + 동적 헤더 ────────────────────
            Debug.Log("=== [4] POST /post (JSON Body + Authorization) ===");
            try
            {
                var body = new PostBody { name = "Unitrofit", value = 42 };
                var res  = await api.PostBodyAsync(body, "Bearer demo-token-12345", cts.Token);
                Debug.Log($"  url: {res.url}");
                Debug.Log($"  data: {res.data}");
                Debug.Log($"  Authorization: {res.headers?.GetValueOrDefault("Authorization")}");
            }
            catch (UnitrofitException e)
            { Debug.LogError($"  실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 5. POST + Form 필드 ──────────────────────────────────
            Debug.Log("=== [5] POST /post (Form) ===");
            try
            {
                var res = await api.PostFormAsync("admin", "password123", cts.Token);
                Debug.Log($"  form: {Dict(res.form)}");
            }
            catch (UnitrofitException e)
            { Debug.LogError($"  실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 6. Multipart 파일 업로드 ─────────────────────────────
            Debug.Log("=== [6] POST /post (Multipart) ===");
            try
            {
                byte[] data = Encoding.UTF8.GetBytes("Unitrofit multipart test");
                var part    = new MultipartBody(data, "test.txt", mimeType: "text/plain");
                var res     = await api.UploadFileAsync(part, "demo upload", cts.Token);
                Debug.Log($"  url: {res.url}");
            }
            catch (UnitrofitException e)
            { Debug.LogError($"  실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 7. PUT ───────────────────────────────────────────────
            Debug.Log("=== [7] PUT /put ===");
            try
            {
                var res = await api.PutAsync(new PostBody { name = "put-test", value = 7 }, cts.Token);
                Debug.Log($"  url: {res.url}");
            }
            catch (UnitrofitException e)
            { Debug.LogError($"  실패 [{e.StatusCode}]: {e.Message}"); }

            // ── 8. DELETE + AllowAnyStatusCode ───────────────────────
            Debug.Log("=== [8] DELETE /delete?id=42 (AllowAnyStatusCode) ===");
            var delRes = await api.DeleteAsync(42, cts.Token);
            Debug.Log($"  url: {delRes.url}");

            // ── 9. Action 콜백 패턴 ──────────────────────────────────
            Debug.Log("=== [9] GET /get (Action 콜백) ===");
            api.GetWithCallback(
                onSuccess: res => Debug.Log($"  [콜백 성공] url: {res.url}"),
                onError:   err => Debug.LogError($"  [콜백 실패] {err.Message}"),
                "callback-q1", "callback-q2");

            // ── 10. Action 콜백 + POST ────────────────────────────────
            Debug.Log("=== [10] POST /post (Action 콜백) ===");
            api.PostWithCallback(
                onSuccess: res => Debug.Log($"  [콜백 성공] data: {res.data}"),
                onError:   err => Debug.LogError($"  [콜백 실패] {err.Message}"),
                new PostBody { name = "callback-body", value = 99 });

            // ── 11. CancellationToken 취소 시연 ──────────────────────
            Debug.Log("=== [11] GET /delay/3 — 즉시 취소 ===");
            var cancelCts = new CancellationTokenSource();
            cancelCts.Cancel();
            try
            {
                await api.GetDelayAsync(3, cancelCts.Token);
            }
            catch (System.OperationCanceledException)
            { Debug.Log("  요청이 취소되었습니다."); }

            Debug.Log("\n=== Unitrofit 데모 완료 ===");
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
