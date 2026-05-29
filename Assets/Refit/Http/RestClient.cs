using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Refit.Attributes.Http;
using Refit.Attributes.Params;
using Refit.Core;
using Refit.Settings;

namespace Refit.Http
{
    /// <summary>
    /// UnityWebRequest 기반 HTTP 실행기.
    /// RequestInfo 의 런타임 데이터를 읽어 요청을 구성하고
    /// 코루틴으로 실행한 뒤 TaskCompletionSource 를 통해 Task 로 반환한다.
    /// </summary>
    public class RestClient
    {
        private readonly MonoBehaviour _runner;
        private readonly RefitSettings _settings;

        public RestClient(MonoBehaviour runner, RefitSettings settings)
        {
            _runner   = runner;
            _settings = settings ?? new RefitSettings();
        }

        // ── 공개 API ────────────────────────────────────────────────────

        public Task<RawResponse> SendAsync(string url, RequestInfo info, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<RawResponse>();
            _runner.StartCoroutine(SendCoroutine(url, info, tcs, ct));
            return tcs.Task;
        }

        // ── 코루틴 ────────────────────────────────────────────────────

        private IEnumerator SendCoroutine(
            string url, RequestInfo info,
            TaskCompletionSource<RawResponse> tcs, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) { tcs.TrySetCanceled(); yield break; }

            UnityWebRequest uwr;
            try { uwr = BuildRequest(url, info); }
            catch (Exception e) { tcs.TrySetException(e); yield break; }

            // 정적 헤더
            foreach (var h in info.StaticHeaders)
                uwr.SetRequestHeader(h.Name, h.Value);

            // 동적 헤더
            foreach (var kv in info.DynamicHeaders)
                uwr.SetRequestHeader(kv.Key, kv.Value);

            // AuthorizationHeaderValueGetter — "Authorization: Bearer" 플레이스홀더 감지
            if (_settings.AuthorizationHeaderValueGetter != null &&
                HasAuthorizationPlaceholder(info))
            {
                var tokenTask = _settings.AuthorizationHeaderValueGetter(uwr, ct);
                yield return new WaitUntil(() => tokenTask.IsCompleted);
                if (tokenTask.IsFaulted)
                { uwr.Dispose(); tcs.TrySetException(tokenTask.Exception); yield break; }
                if (!string.IsNullOrEmpty(tokenTask.Result))
                    uwr.SetRequestHeader("Authorization", "Bearer " + tokenTask.Result);
            }

            // RequestModifier 델리게이트
            if (_settings.RequestModifier != null)
            {
                var modTask = _settings.RequestModifier(uwr, ct);
                yield return new WaitUntil(() => modTask.IsCompleted);
                if (modTask.IsFaulted)
                { uwr.Dispose(); tcs.TrySetException(modTask.Exception); yield break; }
            }

            uwr.timeout = _settings.TimeoutSeconds;

            if (_settings.EnableDebug)
                Debug.Log($"[Refit] --> {uwr.method} {uwr.url}");

            yield return uwr.SendWebRequest();

            if (ct.IsCancellationRequested)
            { uwr.Abort(); uwr.Dispose(); tcs.TrySetCanceled(); yield break; }

            var raw = ParseRaw(uwr);

            if (_settings.EnableDebug)
                Debug.Log($"[Refit] <-- {uwr.responseCode} {uwr.url}\n{raw.Body}");

            uwr.Dispose();
            tcs.TrySetResult(raw);
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

                case HttpMethod.Options:
                    var opt = new UnityWebRequest(url, "OPTIONS");
                    opt.downloadHandler = new DownloadHandlerBuffer();
                    return opt;

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
            string method = info.Method.ToString().ToUpper();

            // ── Multipart ─────────────────────────────────────────────
            if (info.IsMultipart && info.MultipartItems.Count > 0)
            {
                var form = new List<IMultipartFormSection>();
                foreach (var item in info.MultipartItems)
                {
                    if (item.Value is StreamPart sp)
                    {
                        using var ms = new MemoryStream();
                        sp.Value.CopyTo(ms);
                        form.Add(new MultipartFormFileSection(
                            item.PartName, ms.ToArray(), item.FileName ?? sp.FileName, item.ContentType ?? sp.ContentType));
                    }
                    else if (item.Value is ByteArrayPart bp)
                    {
                        form.Add(new MultipartFormFileSection(
                            item.PartName, bp.Value, item.FileName ?? bp.FileName, item.ContentType ?? bp.ContentType));
                    }
                    else if (item.Value is FilePart fp)
                    {
                        form.Add(new MultipartFormFileSection(
                            item.PartName, fp.Data, item.FileName ?? fp.FileName, item.ContentType ?? fp.ContentType));
                    }
                    else
                    {
                        // string / primitive → 텍스트 파트
                        form.Add(new MultipartFormDataSection(item.PartName, item.Value?.ToString() ?? ""));
                    }
                }
                var uwr = UnityWebRequest.Post(url, form, Encoding.UTF8.GetBytes(info.MultipartBoundary));
                if (method != "POST") uwr.method = method;
                return uwr;
            }

            // ── Body 직렬화 ───────────────────────────────────────────
            if (info.BodyInfo != null)
            {
                switch (info.BodyInfo.Method)
                {
                    case BodySerializationMethod.UrlEncoded:
                    {
                        var fields = _settings.ContentSerializer.SerializeFormUrlEncoded(info.BodyInfo.RawValue);
                        var uwr = UnityWebRequest.Post(url, fields);
                        if (method != "POST") uwr.method = method;
                        return uwr;
                    }
                    default:
                    {
                        string json = info.BodyInfo.Method == BodySerializationMethod.Json
                            ? info.BodyInfo.RawValue?.ToString() ?? ""
                            : _settings.ContentSerializer.Serialize(info.BodyInfo.RawValue);

                        byte[] raw = Encoding.UTF8.GetBytes(json);
                        var uwr = new UnityWebRequest(url, method)
                        {
                            uploadHandler   = new UploadHandlerRaw(raw),
                            downloadHandler = new DownloadHandlerBuffer(),
                        };
                        uwr.SetRequestHeader("Content-Type", "application/json");
                        return uwr;
                    }
                }
            }

            // ── 바디 없음 ─────────────────────────────────────────────
            return new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer() };
        }

        // ── 응답 파싱 ────────────────────────────────────────────────────

        private RawResponse ParseRaw(UnityWebRequest uwr)
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

            var hdrs = uwr.GetResponseHeaders();
            if (hdrs != null) raw.ResponseHeaders = hdrs;

            return raw;
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────

        private static bool HasAuthorizationPlaceholder(RequestInfo info)
        {
            foreach (var h in info.StaticHeaders)
                if (h.Name == "Authorization" && h.Value != null &&
                    h.Value.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }

    // ── 내부 원문 응답 ──────────────────────────────────────────────────

    public class RawResponse
    {
        public long StatusCode { get; set; }
        public string Body { get; set; }
        public string Url { get; set; }
        public bool IsNetworkError { get; set; }
        public bool IsHttpError { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }
}
