using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Unitrofit.Attributes.Http;
using Unitrofit.Core;

namespace Unitrofit.Http
{
    /// <summary>
    /// UnityWebRequest 기반 HTTP 구현체.
    /// Unity 메인 스레드에서 실행되며 WebGL을 포함한 모든 Unity 플랫폼을 지원한다.
    /// <para>
    /// 인터셉터 체인·타임아웃은 <see cref="UnitrofitClient"/>가 처리하며,
    /// 이 클래스는 순수 HTTP I/O만 담당한다.
    /// </para>
    /// UnitrofitClient.Builder에서 HttpClient()를 지정하지 않으면 기본으로 사용된다.
    /// </summary>
    public class UnityWebRequestImpl : IHttpClient
    {
        private int _timeoutSeconds = 30;

        // ── IHttpClient ─────────────────────────────────────────────────

        /// <summary>
        /// 타임아웃을 설정한다.
        /// <see cref="UnitrofitClient.Builder.Build"/>에서 자동으로 호출된다.
        /// </summary>
        public void SetTimeout(int timeoutSeconds)
        {
            _timeoutSeconds = timeoutSeconds;
        }

        public async UniTask<RawResponse> SendAsync(string url, RequestInfo info, CancellationToken ct)
        {
            // UnityWebRequest는 Unity 메인 스레드에서만 생성 가능
            await UniTask.SwitchToMainThread();

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            using var uwr = BuildRequest(url, info);

            // 정적 헤더 적용 (인터페이스/메서드 레벨)
            foreach (var h in info.StaticHeaders)
                if (h.Value != null) uwr.SetRequestHeader(h.Name, h.Value);

            // 동적 헤더 적용 ([Header] 파라미터)
            foreach (var kv in info.DynamicHeaders)
                uwr.SetRequestHeader(kv.Key, kv.Value);

            uwr.timeout = _timeoutSeconds;

            // ── 전송 ─────────────────────────────────────────────────────
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
                // 4xx/5xx 도 여기서 잡힘 — ParseResponse에서 처리
            }

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            return ParseResponse(uwr);
        }

        // ── 요청 구성 ────────────────────────────────────────────────────

        private static UnityWebRequest BuildRequest(string url, RequestInfo info)
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

        private static UnityWebRequest BuildBodyRequest(string url, RequestInfo info)
        {
            string verb = info.Method.ToString().ToUpper();
            UnityWebRequest uwr;

            // ── multipart/form-data ─────────────────────────────────────
            if (info.IsMultipart && info.FilePart != null)
            {
                var sections = new List<IMultipartFormSection>
                {
                    new MultipartFormFileSection(
                        info.FilePart.Field,
                        info.FilePart.GetData(),
                        info.FilePart.FileName,
                        info.FilePart.MimeType)
                };
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
                byte[] body = Encoding.UTF8.GetBytes(info.BodyJson);
                uwr = new UnityWebRequest(url, verb)
                {
                    uploadHandler   = new UploadHandlerRaw(body),
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

        private static RawResponse ParseResponse(UnityWebRequest uwr)
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
    }
}
