using System;

namespace Unitrofit.Core
{
    /// <summary>
    /// Retrofit HTTP 요청 실패 시 발생하는 예외.
    /// [AllowAnyStatusCode] 어트리뷰트로 억제할 수 있다.
    /// </summary>
    public class UnitrofitException : Exception
    {
        /// <summary>HTTP 상태 코드. 네트워크 오류이면 0.</summary>
        public int StatusCode { get; }

        /// <summary>원문 응답 바디 (있을 경우).</summary>
        public string RawBody { get; }

        /// <summary>요청 URL.</summary>
        public string RequestUrl { get; }

        /// <summary>네트워크 레벨 오류 여부 (DNS, 타임아웃 등).</summary>
        public bool IsNetworkError { get; }

        public UnitrofitException(
            string message,
            string requestUrl,
            int    statusCode   = 0,
            string rawBody      = null,
            bool   isNetworkError = false,
            Exception inner     = null)
            : base(message, inner)
        {
            RequestUrl    = requestUrl;
            StatusCode    = statusCode;
            RawBody       = rawBody;
            IsNetworkError = isNetworkError;
        }
    }
}
