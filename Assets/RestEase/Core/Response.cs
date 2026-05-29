namespace RestEase.Core
{
    /// <summary>
    /// HTTP 응답 원문 래퍼. 상태코드와 바디를 함께 제공한다.
    /// Task&lt;Response&lt;T&gt;&gt; 를 반환하면 [AllowAnyStatusCode] 없이도 비 2xx 응답을 직접 처리할 수 있다.
    /// </summary>
    public class Response<T>
    {
        /// <summary>역직렬화된 응답 데이터. 실패 시 default(T).</summary>
        public T Data { get; internal set; }

        /// <summary>HTTP 상태 코드.</summary>
        public long StatusCode { get; internal set; }

        /// <summary>원문 응답 바디 (UTF-8 텍스트).</summary>
        public string RawBody { get; internal set; }

        /// <summary>응답 헤더 (key → value). 주요 헤더만 포함.</summary>
        public System.Collections.Generic.Dictionary<string, string> Headers { get; internal set; }
            = new System.Collections.Generic.Dictionary<string, string>();

        /// <summary>2xx 범위이면 true.</summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        /// <summary>에러 메시지. IsSuccess == false 이거나 네트워크 오류 시 설정됨.</summary>
        public string ErrorMessage { get; internal set; }

        /// <summary>네트워크 레벨 오류 여부.</summary>
        public bool IsNetworkError { get; internal set; }
    }
}
