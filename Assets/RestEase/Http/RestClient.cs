using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using RestEase.Attributes.Http;
using RestEase.Core;

namespace RestEase.Http
{
    /// <summary>
    /// UnityWebRequest 기반 HTTP 실행기.
    /// RequestInfo 의 런타임 데이터를 읽어 요청을 구성하고
    /// 코루틴으로 실행한 뒤 TaskCompletionSource 를 통해 Task 로 반환한다.
    ///
    /// RequestModifier: 전송 직전 UnityWebRequest 를 수정할 수 있는 async 델리게이트.
    /// </summary>
    public class RestClient
    {
        // ── 설정 ───────────────────────────────────────────────────────
        public bool EnableDebug { get; set; } = false;
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 모든 요청 전송 직전에 호출되는 수정자 델리게이트 (RestEase RequestModifier 대응).
        /// 헤더 추가, 토큰 갱신, 로깅 등에 활용한다.
        /// <example>
        /// RequestModifier = async (req, ct) => {
        ///     req.SetRequestHeader("Authorization", "Bearer " + token);
        /// };
        /// </example>
        /// </summary>
        public Func<UnityWebRequest, CancellationToken, Task> RequestModifier { get; set; }

        private readonly MonoBehaviour _runner;

        public RestClient(MonoBehaviour runner)
        {
            _runner = runner;
        }

        // ── 공개 API ────────────────────────────────────────────────────

        /// <summary>
        /// RequestInfo 의 런타임 데이터를 기반으로 HTTP 요청을 전송하고
        /// 원문 응답을 RawResponse 로 반환한다.
        /// </summary>
        public Task<RawResponse> SendAsync(string url, RequestInfo info, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<RawResponse>();
            _runner.StartCoroutine(SendCoroutine(url, info, tcs, ct));
            return tcs.Task;
        }

        // ── 코루틴 ────────────────────────────────────────────────────

        private IEnumerator SendCoroutine(
            string url,
            RequestInfo info,
            TaskCompletionSource<RawResponse> tcs,
            CancellationToken ct)
        {
            if (ct.IsCancellationRequested) { tcs.TrySetCanceled(); yield break; }

            UnityWebRequest uwr = BuildRequest(url, info);

            // 정적 헤더 적용
            foreach (var h in info.StaticHeaders)
            {
                if (h.Value == null) continue; // null == 제거 신호, UWR 에서는 skip
                uwr.SetRequestHeader(h.Name, h.Value);
            }

            // 동적 헤더 적용
            foreach (var kv in info.DynamicHeaders)
                uwr.SetRequestHeader(kv.Key, kv.Value);

            // RequestModifier 적용 (async → 코루틴 브릿지)
            if (RequestModifier != null)
            {
                var modifierTask = RequestModifier(uwr, ct);
                yield return new WaitUntil(() => modifierTask.IsCompleted);
                if (modifierTask.IsFaulted)
                {
                    uwr.Dispose();
                    tcs.TrySetException(modifierTask.Exception);
                    yield break;
                }
            }

            uwr.timeout = TimeoutSeconds;

            if (EnableDebug)
                Debug.Log($"[RestEase] --> {uwr.method} {uwr.url}");

            yield return uwr.SendWebRequest();

            if (ct.IsCancellationRequested)
            {
                uwr.Abort();
                uwr.Dispose();
                tcs.TrySetCanceled();
                yield break;
            }

            var raw = ParseRawResponse(uwr);

            if (EnableDebug)
                Debug.Log($"[RestEase] <-- {uwr.responseCode} {uwr.url}\n{raw.Body}");

            uwr.Dispose();
            tcs.TrySetResult(raw);
        }

        // ── 요청 구성 ────────────────────────────────────────────────────

        private UnityWebRequest BuildRequest(string url, RequestInfo info)
        {
            UnityWebRequest uwr;

            switch (info.Method)
            {
                case HttpMethod.Get:
                    uwr = UnityWebRequest.Get(url);
                    break;

                case HttpMethod.Delete:
                    uwr = UnityWebRequest.Delete(url);
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    break;

                case HttpMethod.Head:
                    uwr = new UnityWebRequest(url, "HEAD");
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    break;

                case HttpMethod.Post:
                case HttpMethod.Put:
                case HttpMethod.Patch:
                    uwr = BuildBodyRequest(url, info);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(info.Method), info.Method, null);
            }

            return uwr;
        }

        private UnityWebRequest BuildBodyRequest(string url, RequestInfo info)
        {
            string method = info.Method.ToString().ToUpper();
            UnityWebRequest uwr;

            // multipart/form-data
            if (info.IsMultipart && info.FilePart != null)
            {
                var form = new List<IMultipartFormSection>
                {
                    new MultipartFormFileSection(
                        info.FilePart.FieldName,
                        info.FilePart.Data,
                        info.FilePart.FileName,
                        info.FilePart.MimeType)
                };
                foreach (var kv in info.FieldParams)
                    form.Add(new MultipartFormDataSection(kv.Key, kv.Value));

                uwr = UnityWebRequest.Post(url, form);
                if (method != "POST") uwr.method = method;
            }
            // application/x-www-form-urlencoded
            else if (info.FieldParams.Count > 0 && string.IsNullOrEmpty(info.BodyJson))
            {
                uwr = UnityWebRequest.Post(url, info.FieldParams);
                if (method != "POST") uwr.method = method;
            }
            // application/json
            else if (!string.IsNullOrEmpty(info.BodyJson))
            {
                byte[] raw = Encoding.UTF8.GetBytes(info.BodyJson);
                uwr = new UnityWebRequest(url, method)
                {
                    uploadHandler = new UploadHandlerRaw(raw),
                    downloadHandler = new DownloadHandlerBuffer(),
                };
                uwr.SetRequestHeader("Content-Type", "application/json");
            }
            // 바디 없는 요청
            else
            {
                uwr = new UnityWebRequest(url, method)
                {
                    downloadHandler = new DownloadHandlerBuffer(),
                };
            }

            return uwr;
        }

        // ── 응답 파싱 ────────────────────────────────────────────────────

        private RawResponse ParseRawResponse(UnityWebRequest uwr)
        {
            var raw = new RawResponse
            {
                StatusCode = uwr.responseCode,
                Body = uwr.downloadHandler?.text,
            };

#if UNITY_2020_1_OR_NEWER
            raw.IsNetworkError = uwr.result == UnityWebRequest.Result.ConnectionError ||
                                 uwr.result == UnityWebRequest.Result.DataProcessingError;
            raw.IsHttpError = uwr.result == UnityWebRequest.Result.ProtocolError;
#else
            raw.IsNetworkError = uwr.isNetworkError;
            raw.IsHttpError = uwr.isHttpError;
#endif
            raw.ErrorMessage = uwr.error;
            raw.Url = uwr.url;

            // 응답 헤더 수집
            var responseHeaders = uwr.GetResponseHeaders();
            if (responseHeaders != null)
                raw.ResponseHeaders = responseHeaders;

            return raw;
        }
    }

    // ── 내부 응답 컨테이너 ─────────────────────────────────────────────

    /// <summary>HTTP 원문 응답 컨테이너 (RestAdapter 내부 전용).</summary>
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
