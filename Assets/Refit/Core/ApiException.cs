using System;
using Newtonsoft.Json;

namespace Refit.Core
{
    /// <summary>
    /// 서버로부터 응답을 수신했으나 에러 상태코드(비 2xx)일 때 throw 되는 예외.
    /// [AllowAnyStatusCode] 가 붙어있으면 throw 되지 않는다.
    /// </summary>
    public class ApiException : ApiExceptionBase
    {
        /// <summary>HTTP 상태 코드</summary>
        public int StatusCode { get; }

        /// <summary>원문 응답 바디</summary>
        public string Content { get; }

        public ApiException(string message, string requestUri, int statusCode, string content)
            : base(message, requestUri)
        {
            StatusCode = statusCode;
            Content = content;
        }

        /// <summary>
        /// 응답 바디(Content)를 T 타입으로 역직렬화한다.
        /// 파싱에 실패하면 default(T) 를 반환한다.
        /// </summary>
        public T DeserializeContent<T>()
        {
            if (string.IsNullOrEmpty(Content)) return default;
            try { return JsonConvert.DeserializeObject<T>(Content); }
            catch { return default; }
        }
    }
}
