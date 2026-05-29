using System;

namespace Refit.Core
{
    /// <summary>
    /// Refit API 예외의 기반 클래스.
    ///
    /// <list type="bullet">
    ///   <item><see cref="ApiException"/> — 서버로부터 응답을 받았으나 에러 (4xx, 5xx)</item>
    ///   <item><see cref="ApiRequestException"/> — 서버에 요청 자체가 실패 (네트워크, 타임아웃)</item>
    /// </list>
    /// </summary>
    public abstract class ApiExceptionBase : Exception
    {
        /// <summary>요청 URL</summary>
        public string RequestUri { get; }

        protected ApiExceptionBase(string message, string requestUri, Exception innerException = null)
            : base(message, innerException)
        {
            RequestUri = requestUri;
        }
    }
}
