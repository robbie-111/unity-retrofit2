using System;
using Newtonsoft.Json;

namespace RestEase.Core
{
    /// <summary>
    /// HTTP 요청이 실패하거나 2xx 이외의 상태 코드를 받았을 때 throw 되는 예외.
    /// [AllowAnyStatusCode] 가 붙어있으면 throw 되지 않는다.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>요청 URL</summary>
        public string RequestUri { get; }

        /// <summary>HTTP 상태 코드 (네트워크 오류면 0)</summary>
        public int StatusCode { get; }

        /// <summary>원문 응답 바디</summary>
        public string RawBody { get; }

        /// <summary>네트워크 레벨 오류 여부 (DNS 실패, 타임아웃 등)</summary>
        public bool IsNetworkError { get; }

        public ApiException(string message, string requestUri, int statusCode, string rawBody, bool isNetworkError = false)
            : base(message)
        {
            RequestUri = requestUri;
            StatusCode = statusCode;
            RawBody = rawBody;
            IsNetworkError = isNetworkError;
        }

        /// <summary>
        /// 응답 바디를 T 타입으로 역직렬화한다.
        /// 파싱에 실패하면 null 을 반환한다.
        /// </summary>
        public T DeserializeContent<T>()
        {
            if (string.IsNullOrEmpty(RawBody)) return default;
            try { return JsonConvert.DeserializeObject<T>(RawBody); }
            catch { return default; }
        }
    }
}
